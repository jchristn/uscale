using System;
using System.Collections.Generic;

namespace Uscale.Classes
{
    /// <summary>
    /// Setup class for the Loadbalancer.
    /// </summary>
    public class Setup
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the setup process.
        /// </summary>
        public Setup()
        {
            RunSetup();
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private void RunSetup()
        { 
            #region Variables

            DateTime ts = DateTime.Now;
            Settings ret = new Settings();

            #endregion

            #region Welcome

            Console.WriteLine(
                Environment.NewLine +
                @"                           __     " + Environment.NewLine +
                @"    __  ________________ _/ /__   " + Environment.NewLine +
                @"   / / / / ___/ ___/ __ `/ / _ \  " + Environment.NewLine +
                @"  / /_/ (__  ) /__/ /_/ / /  __/  " + Environment.NewLine +
                @"  \__,_/____/\___/\__,_/_/\___/   " + Environment.NewLine +
                Environment.NewLine);

            Console.ResetColor();

            Console.WriteLine("");
            Console.WriteLine("uscale Loadbalancer");
            Console.WriteLine("");
                            //          1         2         3         4         5         6         7
                            // 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("Thank you for using uscale!  We'll put together a basic system configuration");
            Console.WriteLine("so you can be up and running quickly.");
            Console.WriteLine("");
            Console.WriteLine("Press ENTER to get started.");
            Console.WriteLine("");
            Console.WriteLine(Common.Line(79, "-"));
            Console.ReadLine();

            #endregion

            #region Initial-Settings
            
            ret.EnableConsole = true;
            ret.RedirectStatusCode = 302;
            ret.RedirectStatusString = "Moved Temporarily";
                 
            #endregion
              
            #region Server

            ret.Server = new SettingsServer();
            ret.Server.Ssl = false;
            ret.Server.Port = Common.InputInteger("On which TCP port shall this node listen?", 9000, true, false);
            ret.Server.DnsHostname = Common.InputString("On which hostname shall this node listen [+ for all]?", "+", false);

            Console.WriteLine("This node is configured to use HTTP (not HTTPS) and is accessible at:");
            Console.WriteLine("");
            Console.WriteLine("  http://" + ret.Server.DnsHostname + ":" + ret.Server.Port);
             
            Console.WriteLine("");
            Console.WriteLine("Be sure to install an SSL certificate and modify your config file to");
            Console.WriteLine("use SSL to maximize security and set the correct hostname.");
            Console.WriteLine("");

            #endregion

            #region Auth

            ret.Auth = new SettingsAuth();
            ret.Auth.AdminApiKeyHeader = "x-api-key";
            ret.Auth.AdminApiKey = "admin";

            #endregion

            #region Syslog

            ret.Logging = new SettingsLogging();
            ret.Logging.ConsoleLogging = true;
            ret.Logging.SyslogServerIp = "127.0.0.1";
            ret.Logging.SyslogServerPort = 514;
            ret.Logging.LogRequests = false;
            ret.Logging.LogResponses = false;
            ret.Logging.MinimumSeverityLevel = 1;

            #endregion
            
            #region REST

            ret.Rest = new SettingsRest();
            ret.Rest.AcceptInvalidCerts = true;
            ret.Rest.UseWebProxy = false;
            ret.Rest.WebProxyUrl = "";

            #endregion

            #region Hosts

            ret.Hosts = InputHosts();

            #endregion

            #region Overwrite-Existing-Config-Files

            #region System-Config

            if (Common.FileExists("System.json"))
            {
                Console.WriteLine("System configuration file already exists.");
                if (Common.InputBoolean("Do you wish to overwrite this file?", true))
                {
                    Common.DeleteFile("System.json");
                    if (!Common.WriteFile("System.json", Common.SerializeJson(ret), false))
                    {
                        Common.ExitApplication("Setup", "Unable to write System.json", -1);
                        return;
                    }
                }
            }
            else
            {
                if (!Common.WriteFile("System.json", Common.SerializeJson(ret), false))
                {
                    Common.ExitApplication("Setup", "Unable to write System.json", -1);
                    return;
                }
            }

            #endregion

            #endregion

            #region Finish

            Console.WriteLine("");
            Console.WriteLine("All finished!");
            Console.WriteLine("");
            Console.WriteLine("If you ever want to return to this setup wizard, just re-run the application");
            Console.WriteLine("from the terminal with the 'setup' argument.");
            Console.WriteLine("");
            Console.WriteLine("Press ENTER to start.");
            Console.WriteLine("");
            Console.ReadLine();

            #endregion
        }
 
        private List<Host> InputHosts()
        {
            Host ret = new Host();

            //                          1         2         3         4         5         6         7
            //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("");
            Console.WriteLine("uscale distributes traffic destined to a given 'host' to a 'node'");
            Console.WriteLine("mapped to that host.  A host can have multiple nodes mapped to it.");
            Console.WriteLine("Let's define a host that your loadbalancer will monitor.");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine("");
            Console.WriteLine("  Name:  Google");
            Console.WriteLine("         |");
            Console.WriteLine("         |-- Hosts:");
            Console.WriteLine("         |   |-- google.com");
            Console.WriteLine("         |   |-- www.google.com");
            Console.WriteLine("         |");
            Console.WriteLine("         |-- Nodes:");
            Console.WriteLine("             |-- server1.mydomain.com");
            Console.WriteLine("             |-- server2.mydomain.com");
            Console.WriteLine("");
            Console.WriteLine("In this example, a request to either google.com or www.google.com would be");
            Console.WriteLine("distributed to either server1.mydomain.com or server2.mydomain.com.");
            Console.WriteLine("");
            ret.Name = Common.InputString("What is the name of the site?", "Google", false);
            ret.HttpHostNames = InputHostNames(ret.Name);
            ret.Nodes = InputNodes(ret.Name);
            ret.BalancingScheme = BalancingScheme.RoundRobin;
            ret.HandlingMode = HandlingMode.Redirect;
            ret.AcceptInvalidCerts = true;

            List<Host> retList = new List<Host>();
            retList.Add(ret);
            return retList;
        }

        private List<string> InputHostNames(string name)
        {
            List<string> ret = new List<string>();
            //                          1         2         3         4         5         6         7
            //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("");
            Console.WriteLine("Enter at least one hostname that should be matched for: " + name);

            while (true)
            {
                string curr = Common.InputString("Hostname?", null, true);
                if (!String.IsNullOrEmpty(curr))
                {
                    if (ret.Contains(curr)) continue;
                    else ret.Add(curr);
                }
                else
                {
                    if (ret.Count > 0) break;
                }
            }

            return ret;
        }

        private List<Node> InputNodes(string name)
        {
            List<Node> ret = new List<Node>();
            //                          1         2         3         4         5         6         7
            //                 12345678901234567890123456789012345678901234567890123456789012345678901234567890
            Console.WriteLine("");
            Console.WriteLine("Enter at least one node to which requests for " + name + " should be routed.");
            Console.WriteLine("Each node should have a heartbeat URL which always returns a 200/OK to indicate");
            Console.WriteLine("that the node is online.");
            Console.WriteLine("");
            Console.WriteLine("Press ENTER to supply an empty hostname to finish.");
            Console.WriteLine("");

            while (true)
            {
                Node curr = new Node();
                curr.Hostname = Common.InputString("Hostname?", null, true);
                if (String.IsNullOrEmpty(curr.Hostname))
                {
                    if (ret.Count < 1) continue;
                    else break;
                }

                curr.Port = Common.InputInteger("Port?", 80, true, false);
                curr.Ssl = Common.InputBoolean("Ssl?", false);
                curr.HeartbeatUrl = Common.InputString("Heartbeat URL [full URL, i.e. http://host.mydomain.com:80/api/]?", null, false);
                curr.PollingIntervalMsec = 2500;
                curr.MaxFailures = 4;
                ret.Add(curr);
            }

            return ret;
        }

        #endregion
    }
}