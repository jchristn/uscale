namespace Uscale.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using GetSomeInput;

    /// <summary>
    /// Console manager.
    /// </summary>
    public class ConsoleManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings { get; set; } = null;
        private ContextManager _Connections { get; set; } = null;
        private HostManager _Hosts { get; set; } = null;
        private Func<bool> _ExitDelegate = null;

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
            ContextManager conn,
            HostManager hosts,
            Func<bool> exitApplication)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (hosts == null) throw new ArgumentNullException(nameof(hosts));
            if (exitApplication == null) throw new ArgumentNullException(nameof(exitApplication));

            _Settings = settings;
            _Connections = conn;
            _Hosts = hosts;
            _ExitDelegate = exitApplication;

            Task.Run(() => ConsoleWorker());
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private void ConsoleWorker()
        {
            while (_Settings.EnableConsole)
            {
                string userInput = Inputty.GetString("Command [?/help]:", null, false);

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
                        _Settings.EnableConsole = false;
                        _ExitDelegate();
                        break;

                    case "hosts":
                        ListHosts();
                        break;

                    case "conns":
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
            Console.WriteLine("");
            Console.WriteLine("Available commands:");
            Console.WriteLine("  ?                         help / this menu");
            Console.WriteLine("  cls / c                   clear the console");
            Console.WriteLine("  quit / q                  exit the application");
            Console.WriteLine("  hosts                     list the monitored hosts");
            Console.WriteLine("  conns                     list active connections");
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
                            Console.WriteLine("      - " + (currNode.Failed ? "**Failed**" : "Healthy"));
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
