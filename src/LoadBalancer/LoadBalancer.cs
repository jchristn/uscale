namespace Uscale
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using RestWrapper;
    using SerializationHelper;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using Uscale.Classes;

    public partial class LoadBalancer
    {
        private static string _Header = "[uscale] ";
        private static Settings _Settings = null;
        private static LoggingModule _Logging = null;
        private static HostManager _Hosts = null;
        private static WebserverSettings _WebserverSettings = null;
        private static Webserver _Webserver = null;
        private static ContextManager _Connections = null;
        private static ConsoleManager _Console = null;

        private static List<string> _AdminPrivilegesRequired = new List<string>
        {
            "*",
            "0.0.0.0",
            "+"
        };

        static void Main(string[] args)
        {
            #region Process-Arguments

            if (args != null && args.Length > 0)
            {
                foreach (string curr in args)
                {
                    if (curr.Equals("setup"))
                    {
                        new Setup();
                    }
                }
            }

            #endregion

            #region Load-Config-and-Initialize

            if (!File.Exists(Constants.SystemSettingsFile))
            {
                Setup s = new Setup();
            }

            _Settings = Serializer.DeserializeJson<Settings>(File.ReadAllBytes(Constants.SystemSettingsFile));

            Console.WriteLine(Constants.Logo);

            #endregion

            #region Start-Modules

            _Logging = new LoggingModule(
                _Settings.Logging.SyslogServerIp,
                _Settings.Logging.SyslogServerPort,
                _Settings.Logging.ConsoleLogging);

            _Logging.Settings.MinimumSeverity = (Severity)_Settings.Logging.MinimumSeverityLevel;
            _WebserverSettings = _Settings.ToWebserverSettings();

            Console.WriteLine("");
            Console.WriteLine("Starting listener on " + _WebserverSettings.Prefix);
            if (_AdminPrivilegesRequired.Contains(_WebserverSettings.Hostname)) Console.WriteLine("*** Administrative privileges required");
            Console.WriteLine("");

            _Webserver = new Webserver(_WebserverSettings, RequestHandler);
            _Webserver.Start();
            _Connections = new ContextManager(_Logging);

            using (_Hosts = new HostManager(_Settings, _Logging, _Settings.Hosts))
            {
                if (_Settings.EnableConsole) _Console = new ConsoleManager(_Settings, _Connections, _Hosts, ExitApplication);

                #endregion

                #region Wait-for-Server-Thread

                EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
                bool waitHandleSignal = false;
                do
                {
                    waitHandleSignal = waitHandle.WaitOne(1000);
                } while (!waitHandleSignal);

                _Logging.Debug("LoadBalancer exiting");
            }

            #endregion 
        }

        static async Task RequestHandler(HttpContextBase ctx)
        {
            DateTime startTime = DateTime.UtcNow;
            Host currHost = null;
            Node currNode = null;
            string hostKey = null;
            bool connAdded = false;

            if (_Settings.Logging.LogRequests)
            {
                _Logging.Debug(
                    _Header
                    + ctx.Request.Source.IpAddress + ":" + ctx.Request.Source.Port + " "
                    + ctx.Request.Method.ToString() + " " + ctx.Request.Url.Full);
            }

            try
            {
                #region Unauthenticated-APIs

                switch (ctx.Request.Method)
                {
                    case HttpMethod.GET:
                        if (ctx.Request.Url.RawWithoutQuery.Equals("/_loadbalancer/loopback"))
                        {
                            ctx.Response.StatusCode = 200;
                            ctx.Response.ContentType = Constants.ContentTypeJson;
                            await ctx.Response.Send(Constants.LoopbackMessage);
                            return;
                        }
                        break;

                    case HttpMethod.PUT:
                    case HttpMethod.POST:
                    case HttpMethod.DELETE:
                    default:
                        break;
                }

                #endregion

                #region Add-to-Connection-List

                _Connections.Add(Thread.CurrentThread.ManagedThreadId, ctx);
                connAdded = true;

                #endregion

                #region Authenticate-and-Admin-APIs

                if (!String.IsNullOrEmpty(ctx.Request.RetrieveHeaderValue(_Settings.Auth.AdminApiKeyHeader)))
                { 
                    if (ctx.Request.RetrieveHeaderValue(_Settings.Auth.AdminApiKeyHeader).Equals(_Settings.Auth.AdminApiKey))
                    {
                        #region Admin-APIs

                        _Logging.Info(_Header + "use of admin API key detected for: " + ctx.Request.Url.RawWithoutQuery);

                        switch (ctx.Request.Method)
                        {
                            case WatsonWebserver.Core.HttpMethod.GET:
                                if (ctx.Request.Url.RawWithoutQuery.Equals("/_loadbalancer/connections"))
                                {
                                    ctx.Response.ContentType = Constants.ContentTypeJson;
                                    await ctx.Response.Send(Serializer.SerializeJson(_Connections.GetActiveConnections()));
                                    return;
                                }

                                if (ctx.Request.Url.RawWithoutQuery.Equals("/_loadbalancer/config"))
                                {
                                    ctx.Response.ContentType = Constants.ContentTypeJson;
                                    await ctx.Response.Send(Serializer.SerializeJson(_Settings));
                                    return;
                                }

                                if (ctx.Request.Url.RawWithoutQuery.Equals("/_loadbalancer/hosts"))
                                {
                                    ctx.Response.ContentType = Constants.ContentTypeJson;
                                    await ctx.Response.Send(Serializer.SerializeJson(_Hosts.Get()));
                                    return;
                                }
                                break;
                                
                            case WatsonWebserver.Core.HttpMethod.PUT:
                            case WatsonWebserver.Core.HttpMethod.POST:
                            case WatsonWebserver.Core.HttpMethod.DELETE:
                            default:
                                break;
                        }

                        _Logging.Warn(_Header + "unknown admin API endpoint: " + ctx.Request.Url.RawWithoutQuery);

                        ctx.Response.ContentType = Constants.ContentTypeText;
                        ctx.Response.StatusCode = 400;
                        await ctx.Response.Send("Unknown API endpoint or verb");
                        return;

                        #endregion
                    }
                    else
                    {
                        #region Failed-Auth

                        _Logging.Warn(_Header + "invalid admin API key supplied: " + ctx.Request.RetrieveHeaderValue(_Settings.Auth.AdminApiKeyHeader));

                        ctx.Response.ContentType = Constants.ContentTypeText;
                        ctx.Response.StatusCode = 401;
                        await ctx.Response.Send("Authentication failed");
                        return;

                        #endregion
                    }
                }

                #endregion

                #region Find-Host-and-Node

                if (ctx.Request.Headers.AllKeys.Contains(Constants.HeaderHost) 
                    && !String.IsNullOrEmpty(ctx.Request.Headers.Get(Constants.HeaderHost)))
                {
                    hostKey = ctx.Request.RetrieveHeaderValue("Host");
                }

                if (String.IsNullOrEmpty(hostKey))
                {
                    _Logging.Warn(_Header + "no host header supplied from " + ctx.Request.Source.IpAddress + ":" + ctx.Request.Source.Port + " " + ctx.Request.Method + " " + ctx.Request.Url.RawWithoutQuery);
                    ctx.Response.ContentType = Constants.ContentTypeText;
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send("No host header supplied");
                    return;
                }

                if (!_Hosts.SelectNodeForHost(hostKey, out currHost, out currNode))
                {
                    _Logging.Warn(_Header + "host or node not found for " + ctx.Request.Source.IpAddress + ":" + ctx.Request.Source.Port + " " + ctx.Request.Method + " " + ctx.Request.Url.RawWithoutQuery);
                    ctx.Response.ContentType = Constants.ContentTypeText;
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send("Host or node not found");
                    return;
                }
                else
                {
                    if (currHost == null || currHost == default(Host))
                    {
                        _Logging.Warn(_Header + "host not found for " + ctx.Request.Source.IpAddress + ":" + ctx.Request.Source.Port + " " + ctx.Request.Method + " " + ctx.Request.Url.RawWithoutQuery);
                        ctx.Response.ContentType = Constants.ContentTypeText;
                        ctx.Response.StatusCode = 400;
                        await ctx.Response.Send("Host not found");
                        return;
                    }

                    if (currNode == null || currNode == default(Node))
                    {
                        _Logging.Warn("RequestHandler node not found for " + ctx.Request.Source.IpAddress + ":" + ctx.Request.Source.Port + " " + ctx.Request.Method + " " + ctx.Request.Url.RawWithoutQuery);
                        ctx.Response.ContentType = Constants.ContentTypeText;
                        ctx.Response.StatusCode = 400;
                        await ctx.Response.Send("No node found for host");
                        return;
                    }

                    _Connections.Update(Thread.CurrentThread.ManagedThreadId, hostKey, currHost.Name, currNode.Hostname);
                }

                #endregion

                #region Process-Connection

                if (currHost.HandlingMode == HandlingModeEnum.Proxy)
                {
                    #region Proxy
                     
                    string redirectUrl = BuildProxyUrl(currNode, ctx);

                    NameValueCollection requestHeaders = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
                    if (ctx.Request.Headers != null && ctx.Request.Headers.Count > 0)
                    {
                        List<string> matchHeaders = new List<string> { "host", "connection", "user-agent", "expect" };

                        for (int i = 0; i < ctx.Request.Headers.AllKeys.Count(); i++)
                        {
                            if (matchHeaders.Contains(ctx.Request.Headers.Keys[i]))
                            {
                                continue;
                            }
                            else
                            {
                                requestHeaders.Add(ctx.Request.Headers.Keys[i], ctx.Request.Headers.Get(ctx.Request.Headers.Keys[i]));
                            }
                        }
                    }

                    requestHeaders.Add(Constants.HeaderForwarded, ctx.Request.Source.IpAddress);

                    System.Net.Http.HttpMethod method = Common.WatsonHttpMethodToSystemNetHttpMethod(ctx.Request.Method);

                    // process REST request
                    using (RestRequest restReq = new RestRequest(redirectUrl, method, requestHeaders, ctx.Request.ContentType))
                    {
                        restReq.IgnoreCertificateErrors = _Settings.Rest.AcceptInvalidCerts;

                        RestResponse restResp = null;

                        if (method == System.Net.Http.HttpMethod.Get
                            || method == System.Net.Http.HttpMethod.Head)
                        {
                            restResp = restReq.Send();
                        }
                        else if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
                        {
                            restResp = restReq.Send(ctx.Request.ContentLength, ctx.Request.Data);
                        }
                        else if (ctx.Request.DataAsBytes != null)
                        {
                            restResp = restReq.Send(ctx.Request.DataAsBytes);
                        }
                        else
                        {
                            restResp = restReq.Send();
                        }

                        if (restResp == null)
                        {
                            _Logging.Warn(_Header + "null proxy response from " + redirectUrl);
                            ctx.Response.ContentType = Constants.ContentTypeText;
                            ctx.Response.StatusCode = 500;
                            await ctx.Response.Send("Unable to contact node");
                            return;
                        }
                        else
                        {
                            ctx.Response.StatusCode = restResp.StatusCode;
                            ctx.Response.ContentType = restResp.ContentType;
                            ctx.Response.ContentLength = restResp.ContentLength;
                            ctx.Response.Headers = restResp.Headers;
                            await ctx.Response.Send(restResp.ContentLength, restResp.Data);
                            restResp.Dispose();
                            return;
                        }
                    }

                    #endregion
                }
                else if (currHost.HandlingMode == HandlingModeEnum.Redirect)
                {
                    #region Redirect

                    string redirectUrl = BuildProxyUrl(currNode, ctx);

                    NameValueCollection redirectHeaders = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
                    redirectHeaders.Add("location", redirectUrl);

                    ctx.Response.ContentType = Constants.ContentTypeText;
                    ctx.Response.StatusCode = _Settings.RedirectStatusCode;
                    ctx.Response.Headers = redirectHeaders;
                    await ctx.Response.Send(_Settings.RedirectStatusString);

                    _Logging.Debug(_Header + "redirecting " + ctx.Request.Url.Full + " to " + redirectUrl);
                    return;

                    #endregion
                }
                else
                {
                    #region Unknown-Handling-Mode

                    _Logging.Warn(_Header + "invalid handling mode " + currHost.HandlingMode + " for host " + currHost.Name);
                    ctx.Response.ContentType = Constants.ContentTypeText;
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send("Invalid handling mode '" + currHost.HandlingMode + "'");
                    return;

                    #endregion
                }

                #endregion
            }
            catch (Exception e)
            {
                _Logging.Exception(e);
                ctx.Response.ContentType = Constants.ContentTypeText;
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send("Internal server error");
                return;
            }
            finally
            {
                string message = _Header 
                    + ctx.Request.Source.IpAddress + ":" + ctx.Request.Source.Port + " " 
                    + ctx.Request.Method + " " + ctx.Request.Url.RawWithoutQuery;

                if (currNode != null) message += " " 
                    + hostKey 
                    + " to " + currNode.Hostname + ":" + currNode.Port 
                    + " " + currHost.HandlingMode;

                message += " " 
                    + ctx.Response.StatusCode 
                    + " " + Common.TotalMsFrom(startTime) + "ms";

                _Logging.Debug(message);

                if (connAdded)
                    _Connections.Close(Thread.CurrentThread.ManagedThreadId);
            }
        }

        public static string BuildProxyUrl(Node redirectNode, HttpContextBase ctx)
        { 
            UriBuilder modified = new UriBuilder(ctx.Request.Url.Full);
            string ret = "";
            
            modified.Host = redirectNode.Hostname;
            modified.Port = redirectNode.Port;

            if (redirectNode.Ssl) modified.Scheme = Uri.UriSchemeHttps;
            else modified.Scheme = Uri.UriSchemeHttp;

            ret = modified.Uri.ToString();
            return ret; 
        }

        static bool ExitApplication()
        {
            _Logging.Info("LoadBalancer exiting due to console request");
            Environment.Exit(0);
            return true;
        }
    }
}
