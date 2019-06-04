using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyslogLogging;
using RestWrapper;

namespace Uscale.Classes
{
    /// <summary>
    /// Console manager.
    /// </summary>
    public class ConsoleManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private bool _Enabled { get; set; }
        private Settings _Settings { get; set; }
        private ConnectionManager _Connections { get; set; }
        private HostManager _Hosts { get; set; }
        private Func<bool> _ExitDelegate;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="settings">Server settings.</param>
        /// <param name="conn">Connection manager instance.</param>
        /// <param name="hosts">Host manager instance.</param>
        /// <param name="exitApplication">Function to call when exiting the application.</param>
        public ConsoleManager(
            Settings settings,
            ConnectionManager conn,
            HostManager hosts,
            Func<bool> exitApplication)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (hosts == null) throw new ArgumentNullException(nameof(hosts));
            if (exitApplication == null) throw new ArgumentNullException(nameof(exitApplication));

            _Enabled = true;
            _Settings = settings;
            _Connections = conn;
            _Hosts = hosts;
            _ExitDelegate = exitApplication;

            Task.Run(() => ConsoleWorker());
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Stop the console manager.
        /// </summary>
        public void Stop()
        {
            _Enabled = false;
            return;
        }

        #endregion

        #region Private-Methods

        private void ConsoleWorker()
        {
            string userInput = "";
            while (_Enabled)
            {
                Console.Write("Command (? for help) > ");
                userInput = Console.ReadLine();

                if (userInput == null) continue;
                switch (userInput.ToLower().Trim())
                {
                    case "?":
                        Menu();
                        break;

                    case "c":
                    case "cls":
                    case "clear":
                        Console.Clear();
                        break;

                    case "q":
                    case "quit":
                        _Enabled = false;
                        _ExitDelegate();
                        break;

                    case "list_hosts":
                        ListHosts();
                        break;

                    case "list_connections":
                        ListConnections();
                        break;

                    default:
                        Console.WriteLine("Unknown command.  '?' for help.");
                        break;
                }
            }
        }

        private void Menu()
        {
            Console.WriteLine(Common.Line(79, "-"));
            Console.WriteLine("  ?                         help / this menu");
            Console.WriteLine("  cls / c                   clear the console");
            Console.WriteLine("  quit / q                  exit the application");
            Console.WriteLine("  list_hosts                list the monitored hosts");
            Console.WriteLine("  list_connections          list active connections");
            Console.WriteLine("");
            return;
        }
         
        private void ListHosts()
        {
            List<Host> hosts = _Hosts.Get();

            if (hosts != null && hosts.Count > 0)
            {
                Console.WriteLine(hosts.Count + " Hosts");
                foreach (Host currHost in hosts)
                {
                    Console.WriteLine("  " + currHost.Name);

                    if (currHost.HttpHostNames != null && currHost.HttpHostNames.Count > 0)
                    {
                        Console.WriteLine("    HTTP host names: ");
                        foreach (string currHttpHostName in currHost.HttpHostNames)
                        {
                            Console.WriteLine("      " + currHttpHostName);
                        } 
                    }
                    else
                    {
                        Console.WriteLine("    HTTP host names: (null)");
                    }

                    Console.WriteLine("    Balancing scheme: " + currHost.BalancingScheme.ToString());
                    Console.WriteLine("    Handling mode: " + currHost.HandlingMode.ToString());

                    if (currHost.Nodes != null && currHost.Nodes.Count > 0)
                    {
                        Console.WriteLine("    Nodes: " + currHost.Nodes.Count);

                        foreach (Node currNode in currHost.Nodes)
                        {
                            Console.WriteLine("      " + currNode.Hostname);
                            Console.WriteLine("      - Heartbeat URL: " + currNode.HeartbeatUrl);
                            Console.WriteLine("      - Last attempt: " + currNode.LastAttempt);
                            Console.WriteLine("      - Last success: " + currNode.LastSuccess);
                            Console.WriteLine("      - Last failure: " + currNode.LastFailure);
                            Console.WriteLine("      - " + currNode.NumFailures + " failures / " + currNode.MaxFailures + " max");
                            Console.WriteLine("      - " + (Common.IsTrue(currNode.Failed) ? "**Failed**" : "Healthy"));
                        }
                    }
                    else
                    {
                        Console.WriteLine("    Nodes: (null)");
                    }
                }
            }
            else
            {
                Console.WriteLine("(null)");
            }

            Console.WriteLine("");
        }

        private void ListConnections()
        {
            List<Connection> conns = _Connections.GetActiveConnections();
            
            if (conns != null && conns.Count > 0)
            {
                Console.WriteLine(conns.Count + " Connections");
                foreach (Connection currConn in conns)
                {
                    Console.WriteLine("  " + currConn.SourceIp + ":" + currConn.SourcePort +
                        " " + currConn.Method + " " + currConn.RawUrl +
                        " " + currConn.HttpHostName + " " + currConn.NodeName);
                }
            }
            else
            {
                Console.WriteLine("(null)");
            }

            Console.WriteLine("");
        }

        #endregion 
    }
}
