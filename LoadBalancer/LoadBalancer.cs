using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SyslogLogging;
using WatsonWebserver;
using RestWrapper;
using Uscale.Classes;

namespace Uscale
{
    public partial class LoadBalancer
    {
        public static Settings _Settings;
        public static LoggingModule _Logging;
        public static HostManager _Hosts;
        public static Server _Server;
        public static ConnectionManager _Connections;
        public static ConsoleManager _Console;

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

            if (!Common.FileExists("System.json"))
            {
                Setup s = new Setup();
            }

            _Settings = Settings.FromFile("System.json");

            Welcome();

            #endregion

            #region Start-Modules

            _Logging = new LoggingModule(
                _Settings.Logging.SyslogServerIp,
                _Settings.Logging.SyslogServerPort,
                Common.IsTrue(_Settings.Logging.ConsoleLogging),
                (LoggingModule.Severity)(_Settings.Logging.MinimumSeverityLevel),
                false,
                true,
                true,
                false,
                true,
                false);

            _Hosts = new HostManager(_Settings, _Logging, _Settings.Hosts);
             
            _Server = new Server(
                _Settings.Server.DnsHostname,
                _Settings.Server.Port,
                Common.IsTrue(_Settings.Server.Ssl),
                RequestHandler);

            _Connections = new ConnectionManager(_Logging);

            if (Common.IsTrue(_Settings.EnableConsole)) _Console = new ConsoleManager(_Settings, _Connections, _Hosts, ExitApplication);

            #endregion

            #region Wait-for-Server-Thread

            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, Guid.NewGuid().ToString());
            bool waitHandleSignal = false;
            do
            {
                waitHandleSignal = waitHandle.WaitOne(1000);
            } while (!waitHandleSignal);

            _Logging.Log(LoggingModule.Severity.Debug, "LoadBalancer exiting");

            #endregion 
        }

        static void Welcome()
        {
            // http://patorjk.com/software/taag/#p=display&f=Slant&t=uscale

            string msg =
                Environment.NewLine +
                @"                           __     " + Environment.NewLine +
                @"    __  ________________ _/ /__   " + Environment.NewLine +
                @"   / / / / ___/ ___/ __ `/ / _ \  " + Environment.NewLine +
                @"  / /_/ (__  ) /__/ /_/ / /  __/  " + Environment.NewLine +
                @"  \__,_/____/\___/\__,_/_/\___/   " + Environment.NewLine +
                Environment.NewLine;

            Console.WriteLine(msg);
        }

        static HttpResponse RequestHandler(HttpRequest req)
        {
            DateTime startTime = DateTime.Now;
            HttpResponse resp = null;
            Host currHost = null;
            Node currNode = null;
            string hostKey = null;
            bool connAdded = false;

            try
            {
                #region Unauthenticated-APIs

                switch (req.Method)
                {
                    case WatsonWebserver.HttpMethod.GET:
                        if (WatsonCommon.UrlEqual(req.RawUrlWithoutQuery, "/_loadbalancer/loopback", false))
                        {
                            resp = new HttpResponse(req, 200, null, "application/json", Encoding.UTF8.GetBytes("Hello from uscale!"));
                            return resp;
                        }
                        break;

                    case WatsonWebserver.HttpMethod.PUT:
                    case WatsonWebserver.HttpMethod.POST:
                    case WatsonWebserver.HttpMethod.DELETE:
                    default:
                        break;
                }

                #endregion

                #region Add-to-Connection-List

                _Connections.Add(Thread.CurrentThread.ManagedThreadId, req);
                connAdded = true;

                #endregion

                #region Authenticate-and-Admin-APIs

                if (!String.IsNullOrEmpty(req.RetrieveHeaderValue(_Settings.Auth.AdminApiKeyHeader)))
                { 
                    if (req.RetrieveHeaderValue(_Settings.Auth.AdminApiKeyHeader).Equals(_Settings.Auth.AdminApiKey))
                    {
                        #region Admin-APIs

                        _Logging.Log(LoggingModule.Severity.Info, "RequestHandler use of admin API key detected for: " + req.RawUrlWithoutQuery);

                        switch (req.Method)
                        {
                            case WatsonWebserver.HttpMethod.GET:
                                if (WatsonCommon.UrlEqual(req.RawUrlWithoutQuery, "/_loadbalancer/connections", false))
                                {
                                    resp = new HttpResponse(req, 200, null, "application/json", Encoding.UTF8.GetBytes(Common.SerializeJson(_Connections.GetActiveConnections())));
                                    return resp;
                                }

                                if (WatsonCommon.UrlEqual(req.RawUrlWithoutQuery, "/_loadbalancer/config", false))
                                {
                                    resp = new HttpResponse(req, 200, null, "application/json", Encoding.UTF8.GetBytes(Common.SerializeJson(_Settings)));
                                    return resp;
                                }

                                if (WatsonCommon.UrlEqual(req.RawUrlWithoutQuery, "/_loadbalancer/hosts", false))
                                {
                                    resp = new HttpResponse(req, 200, null, "application/json", Encoding.UTF8.GetBytes(Common.SerializeJson(_Hosts.Get())));
                                    return resp;
                                }
                                break;
                                
                            case WatsonWebserver.HttpMethod.PUT:
                            case WatsonWebserver.HttpMethod.POST:
                            case WatsonWebserver.HttpMethod.DELETE:
                            default:
                                break;
                        }

                        _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler unknown admin API endpoint: " + req.RawUrlWithoutQuery);
                        resp = new HttpResponse(req, 400, null, "application/json", Encoding.UTF8.GetBytes("Unknown API endpoint or verb"));
                        return resp;

                        #endregion
                    }
                    else
                    {
                        #region Failed-Auth

                        _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler invalid admin API key supplied: " + req.RetrieveHeaderValue(_Settings.Auth.AdminApiKeyHeader));
                        resp = new HttpResponse(req, 401, null, "application/json", Encoding.UTF8.GetBytes("Authentication failed"));
                        return resp;

                        #endregion
                    }
                }

                #endregion

                #region Find-Host-and-Node

                if (req.Headers.ContainsKey("Host")) hostKey = req.RetrieveHeaderValue("Host");

                if (String.IsNullOrEmpty(hostKey))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler no host header supplied for " + req.SourceIp + ":" + req.SourceIp + " " + req.Method + " " + req.RawUrlWithoutQuery);
                    resp = new HttpResponse(req, 400, null, "application/json", Encoding.UTF8.GetBytes("No host header supplied"));
                    return resp;
                }

                if (!_Hosts.SelectNodeForHost(hostKey, out currHost, out currNode))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler host or node not found for " + req.SourceIp + ":" + req.SourceIp + " " + req.Method + " " + req.RawUrlWithoutQuery);
                    resp = new HttpResponse(req, 400, null, "application/json", Encoding.UTF8.GetBytes("Host or node not found"));
                    return resp;
                }
                else
                {
                    if (currHost == null || currHost == default(Host))
                    {
                        _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler host not found for " + req.SourceIp + ":" + req.SourceIp + " " + req.Method + " " + req.RawUrlWithoutQuery);
                        resp = new HttpResponse(req, 400, null, "application/json", Encoding.UTF8.GetBytes("Host not found"));
                        return resp;
                    }

                    if (currNode == null || currNode == default(Node))
                    {
                        _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler node not found for " + req.SourceIp + ":" + req.SourceIp + " " + req.Method + " " + req.RawUrlWithoutQuery);
                        resp = new HttpResponse(req, 400, null, "application/json", Encoding.UTF8.GetBytes("No node found for host"));
                        return resp;
                    }

                    _Connections.Update(Thread.CurrentThread.ManagedThreadId, hostKey, currHost.Name, currNode.Hostname);
                }

                #endregion

                #region Process-Connection

                if (currHost.HandlingMode == HandlingMode.Redirect)
                {
                    #region Redirect
                     
                    string redirectUrl = BuildProxyUrl(currNode, req);

                    // add host header
                    Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
                    
                    // add other headers
                    if (req.Headers != null && req.Headers.Count > 0)
                    {
                        List<string> matchHeaders = new List<string> { "host", "connection", "user-agent", "expect" };

                        foreach (KeyValuePair<string, string> currHeader in req.Headers)
                        {
                            if (matchHeaders.Contains(currHeader.Key.ToLower().Trim()))
                            {
                                continue;
                            }
                            else
                            {
                                requestHeaders.Add(currHeader.Key, currHeader.Value);
                            }
                        }
                    }

                    RestWrapper.HttpMethod method = (RestWrapper.HttpMethod)(Enum.Parse(typeof(RestWrapper.HttpMethod), req.Method.ToString()));

                    // process REST request
                    RestRequest restReq = new RestRequest(
                        redirectUrl,
                        method,
                        requestHeaders,
                        req.ContentType,
                        true);

                    restReq.IgnoreCertificateErrors = _Settings.Rest.AcceptInvalidCerts;

                    RestResponse restResp = restReq.Send();

                    if (restResp == null)
                    {
                        _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler null proxy response from " + redirectUrl);
                        resp = new HttpResponse(req, 500, null, "application/json", Encoding.UTF8.GetBytes("Unable to contact node"));
                        return resp;
                    }
                    else
                    {
                        resp = new HttpResponse(req, restResp.StatusCode, restResp.Headers, restResp.ContentType, restResp.Data);
                        return resp;
                    }

                    #endregion
                }
                else if (currHost.HandlingMode == HandlingMode.Proxy)
                {
                    #region Proxy

                    string redirectUrl = BuildProxyUrl(currNode, req);
                    
                    Dictionary<string, string> redirectHeaders = new Dictionary<string, string>();
                    redirectHeaders.Add("location", redirectUrl);

                    resp = new HttpResponse(req, _Settings.RedirectStatusCode, redirectHeaders, "text/plain", Encoding.UTF8.GetBytes(_Settings.RedirectStatusString));
                    return resp;

                    #endregion
                }
                else
                {
                    #region Unknown-Handling-Mode

                    _Logging.Log(LoggingModule.Severity.Warn, "RequestHandler invalid handling mode " + currHost.HandlingMode + " for host " + currHost.Name);
                    resp = new HttpResponse(req, 500, null, "application/json", Encoding.UTF8.GetBytes("Invalid handling mode '" + currHost.HandlingMode + "'"));
                    return resp;

                    #endregion
                }

                #endregion
            }
            catch (Exception e)
            {
                _Logging.LogException("LoadBalancer", "RequestHandler", e);
                resp = new HttpResponse(req, 500, null, "application/json", Encoding.UTF8.GetBytes("Internal server error"));
                return resp;
            }
            finally
            {
                if (resp != null)
                {
                    string message = "RequestHandler " + req.SourceIp + ":" + req.SourcePort + " " + req.Method + " " + req.RawUrlWithoutQuery;
                    if (currNode != null) message += " " + hostKey + " to " + currNode.Hostname + ":" + currNode.Port + " " + currHost.HandlingMode;
                    message += " " + resp.StatusCode + " " + Common.TotalMsFrom(startTime) + "ms";
                    _Logging.Log(LoggingModule.Severity.Debug, message);
                }

                if (connAdded)
                {
                    _Connections.Close(Thread.CurrentThread.ManagedThreadId);
                }
            }
        }

        public static string BuildProxyUrl(Node redirectNode, HttpRequest req)
        { 
            UriBuilder modified = new UriBuilder(req.FullUrl);
            string ret = "";
            
            modified.Host = String.Copy(redirectNode.Hostname);
            modified.Port = redirectNode.Port;

            if (Common.IsTrue(redirectNode.Ssl)) modified.Scheme = Uri.UriSchemeHttps;
            else modified.Scheme = Uri.UriSchemeHttp;

            ret = modified.Uri.ToString();
            return ret; 
        }

        static bool ExitApplication()
        {
            _Logging.Log(LoggingModule.Severity.Info, "LoadBalancer exiting due to console request");
            Environment.Exit(0);
            return true;
        }
    }
}
