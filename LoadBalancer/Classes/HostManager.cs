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
    /// Host manager.
    /// </summary>
    public class HostManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Settings _Settings;
        private LoggingModule _Logging;
        private List<Host> _Hosts;
        private readonly object _HostsLock;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="logging">Logging instance.</param>
        /// <param name="hosts">List of Hosts</param>
        public HostManager(Settings settings, LoggingModule logging, List<Host> hosts)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (logging == null) throw new ArgumentNullException(nameof(logging));
            if (hosts == null) throw new ArgumentNullException(nameof(hosts));

            _Settings = settings;
            _Logging = logging;
            _Hosts = hosts;
            _HostsLock = new object();

            Task.Run(() => StartMonitorThreads(_Hosts));
        }

        #endregion

        #region Public-Members

        /// <summary>
        /// Retrieve list of hosts.
        /// </summary>
        /// <returns>List of Host objects.</returns>
        public List<Host> Get()
        {
            lock (_HostsLock)
            {
                return _Hosts;
            }
        }

        /// <summary>
        /// Retrieve a Host by name.
        /// </summary>
        /// <param name="name">Name of the Host.</param>
        /// <returns>Host object.</returns>
        public Host GetHostByName(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "GetHostByName null host name supplied");
                return null;
            }

            lock (_HostsLock)
            {
                Host ret = _Hosts.FirstOrDefault(i => i.Name == name);
                if (ret != null && ret != default(Host)) return ret;
            }

            _Logging.Log(LoggingModule.Severity.Warn, "GetHostByName unable to find host with name " + name);
            return null;
        }

        /// <summary>
        /// Select a node to service a client's request for a given host.
        /// </summary>
        /// <param name="hostName">Host name.</param>
        /// <param name="host">Host.</param>
        /// <param name="node">Node.</param>
        /// <returns>True if successful.</returns>
        public bool SelectNodeForHost(string hostName, out Host host, out Node node)
        {
            host = null;
            node = null;

            if (String.IsNullOrEmpty(hostName))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "SelectNodeForHost null hostname supplied");
                return false;
            }

            lock (_HostsLock)
            {
                host = _Hosts.FirstOrDefault(i => i.HttpHostNames.Contains(hostName));
                if (host == null || host == default(Host))
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "SelectNodeForHost could not find host with HTTP host name " + hostName);
                    return false;
                }
            }

            if (host.BalancingScheme == BalancingScheme.RoundRobin)
            {
                int maxAttempts = host.Nodes.Count * 5;

                for (int i = 0; i < maxAttempts; i++)
                {
                    if (host.LastIndex >= (host.Nodes.Count - 1)) host.LastIndex = 0;
                    else host.LastIndex = host.LastIndex + 1;

                    node = host.Nodes[host.LastIndex];
                    if (node.Failed) continue;
                    else
                    {
                        UpdateHostIndex(host, host.LastIndex);
                        return true;
                    }
                }

                _Logging.Log(LoggingModule.Severity.Warn, "SelectNodeForHost unable to find active host for " + host.Name);
                return false;
            }
            else
            {
                _Logging.Log(LoggingModule.Severity.Warn, "SelectNodeForHost invalid load-balancing schema: " + host.BalancingScheme.ToString());
                return false;
            } 
        }

        #endregion

        #region Private-Methods

        private void StartMonitorThreads(List<Host> hosts)
        {
            if (hosts == null || hosts.Count < 1)
            {
                _Logging.Log(LoggingModule.Severity.Warn, "StartMonitorThreads no hosts supplied");
                return;
            }

            foreach (Host currHost in hosts)
            {
                if (currHost.Nodes == null || currHost.Nodes.Count < 1)
                {
                    _Logging.Log(LoggingModule.Severity.Warn, "StartMonitorThreads no nodes for host " + currHost.Name);
                    continue;
                }

                foreach (Node currNode in currHost.Nodes)
                {
                    Task.Run(() => MonitorThread(currHost, currNode));
                }
            }
        }

        private void MonitorThread(Host host, Node node)
        {
            try
            {
                _Logging.Log(LoggingModule.Severity.Debug, "MonitorThread starting for host " + host.Name + " node " + node.Hostname);

                bool firstRun = true;
                
                while (true)
                {
                    #region Sleep

                    if (!firstRun)
                    {
                        Task.Delay(node.PollingIntervalMsec).Wait();
                    }
                    else
                    {
                        firstRun = false;
                    }

                    #endregion

                    #region Poll

                    _Logging.Log(LoggingModule.Severity.Debug, "MonitorThread querying node " + node.Hostname + ":" + node.Port);

                    RestRequest req = new RestRequest(
                        node.HeartbeatUrl,
                        HttpMethod.GET,
                        null,
                        null,
                        true);

                    req.IgnoreCertificateErrors = _Settings.Rest.AcceptInvalidCerts;

                    RestResponse resp = req.Send();

                    if (resp == null || resp.StatusCode != 200)
                    {
                        _Logging.Log(LoggingModule.Severity.Warn, "MonitorThread node " + node.Hostname + ":" + node.Port + " inquiry failed");
                        AddNodeFailure(host, node);
                    }
                    else
                    {
                        _Logging.Log(LoggingModule.Severity.Debug, "MonitorThread node " + node.Hostname + ":" + node.Port + " inquiry succeeded");
                        AddNodeSuccess(host, node);
                    }

                    #endregion
                }
            }
            catch (Exception e)
            {
                _Logging.LogException("HostManager", "MonitorThread " + node.Hostname, e);
            }
        }

        private void UpdateHostIndex(Host host, int lastIndex)
        {
            Host tempHost = GetHostByName(host.Name);
            if (tempHost == null || tempHost == default(Host)) return;

            lock (_HostsLock)
            {
                _Hosts.Remove(tempHost);
                tempHost.LastIndex = lastIndex;
                _Hosts.Add(tempHost);
            }
        }

        private void AddNodeFailure(Host host, Node node)
        {
            Host currHost = GetHostByName(host.Name);
            if (currHost == null || currHost == default(Host))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AddNodeFailure unable to retrieve host with name " + host.Name);
                return;
            }

            Node currNode = null;
            foreach (Node tempNode in currHost.Nodes)
            {
                if (tempNode.Hostname.Equals(node.Hostname))
                {
                    currNode = tempNode;
                    break;
                }
            }

            if (currNode == null || currNode == default(Node))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AddNodeFailure unable to retrieve node with hostname " + currNode.Hostname);
                return;
            }

            currNode.LastAttempt = DateTime.Now.ToUniversalTime();
            currNode.LastFailure = currNode.LastAttempt;

            if (currNode.NumFailures == null) currNode.NumFailures = 1;
            else currNode.NumFailures = currNode.NumFailures + 1;

            if (currNode.NumFailures >= currNode.MaxFailures)
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AddNodeFailure marking node " + currNode.Hostname + ":" + currNode.Port + " failed (" + currNode.NumFailures + " failures, max " + currNode.MaxFailures + ")");
                currNode.Failed = true;
            }
            else
            {
                currNode.Failed = false;
            }

            ReplaceNode(currHost, currNode);
        }

        private void AddNodeSuccess(Host host, Node node)
        { 
            Host currHost = GetHostByName(host.Name);
            if (currHost == null || currHost == default(Host))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AddNodeSuccess unable to retrieve host with name " + host.Name);
                return;
            }

            Node currNode = null;
            foreach (Node tempNode in currHost.Nodes)
            {
                if (tempNode.Hostname.Equals(node.Hostname))
                {
                    currNode = tempNode;
                    break;
                }
            }

            if (currNode == null || currNode == default(Node))
            {
                _Logging.Log(LoggingModule.Severity.Warn, "AddNodeSuccess unable to retrieve node with hostname " + currNode.Hostname);
                return;
            }

            currNode.LastAttempt = DateTime.Now.ToUniversalTime();
            currNode.LastSuccess = currNode.LastAttempt;
            currNode.NumFailures = 0;
            currNode.Failed = false;
            
            ReplaceNode(currHost, currNode);
        }

        private void ReplaceNode(Host host, Node node)
        {
            Host tempHost = GetHostByName(host.Name);

            lock (_HostsLock)
            {
                List<Node> tempNodeList = new List<Node>();

                foreach (Node tempNode in tempHost.Nodes)
                {
                    if (tempNode.Hostname.Equals(node.Hostname)) tempNodeList.Add(node);
                    else tempNodeList.Add(tempNode);
                }

                tempHost.Nodes = tempNodeList;

                List<Host> tempHostList = new List<Host>();

                foreach (Host currHost in _Hosts)
                {
                    if (currHost.Name.Equals(tempHost.Name)) tempHostList.Add(tempHost);
                    else tempHostList.Add(currHost);
                }

                _Hosts = tempHostList;
            }
        }

        #endregion
    }
}
