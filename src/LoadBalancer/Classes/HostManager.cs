namespace Uscale.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using SyslogLogging;
    using RestWrapper;
    using System.Threading;

    /// <summary>
    /// Host manager.
    /// </summary>
    public class HostManager : IDisposable
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[HostManager] ";
        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private List<Host> _Hosts = new List<Host>();
        private readonly object _HostsLock = new object();

        private CancellationTokenSource _TokenSource = new CancellationTokenSource();

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

            Task.Run(() => StartMonitorThreads(_Hosts, _TokenSource.Token), _TokenSource.Token);
        }

        #endregion

        #region Public-Members

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {

        }

        /// <summary>
        /// Retrieve list of hosts.
        /// </summary>
        /// <returns>List of Host objects.</returns>
        public List<Host> Get()
        {
            lock (_HostsLock)
            {
                return new List<Host>(_Hosts);
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
                _Logging.Warn(_Header + "null host name supplied");
                return null;
            }

            lock (_HostsLock)
            {
                Host ret = _Hosts.FirstOrDefault(i => i.Name == name);
                if (ret != null && ret != default(Host)) return ret;
            }

            _Logging.Warn(_Header + "unable to find host with name " + name);
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
                _Logging.Warn(_Header + "null hostname supplied");
                return false;
            }

            lock (_HostsLock)
            {
                host = _Hosts.FirstOrDefault(i => i.HttpHostNames.Contains(hostName));
                if (host == null || host == default(Host))
                {
                    _Logging.Warn(_Header + "could not find host with HTTP host name " + hostName);
                    return false;
                }
            }

            if (host.BalancingScheme == BalancingSchemeEnum.RoundRobin)
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

                _Logging.Warn(_Header + "unable to find active host for " + host.Name);
                return false;
            }
            else
            {
                _Logging.Warn(_Header + "invalid load-balancing schema: " + host.BalancingScheme.ToString());
                return false;
            } 
        }

        #endregion

        #region Private-Methods

        private async Task StartMonitorThreads(List<Host> hosts, CancellationToken token)
        {
            if (hosts == null || hosts.Count < 1)
            {
                _Logging.Warn(_Header + "no hosts supplied");
                return;
            }

            foreach (Host currHost in hosts)
            {
                if (currHost.Nodes == null || currHost.Nodes.Count < 1)
                {
                    _Logging.Warn(_Header + "no nodes for host " + currHost.Name);
                    continue;
                }

                foreach (Node currNode in currHost.Nodes)
                {
                    await Task.Run(() => MonitorThread(currHost, currNode, token), token);
                }
            }
        }

        private async Task MonitorThread(Host host, Node node, CancellationToken token)
        {
            _Logging.Debug(_Header + "starting for host " + host.Name + " node " + node.Hostname);

            bool firstRun = true;
                
            while (!token.IsCancellationRequested)
            {
                try
                { 
                    #region Sleep

                    if (!firstRun)
                    {
                        await Task.Delay(node.PollingIntervalMsec);
                    }
                    else
                    {
                        firstRun = false;
                    }

                    #endregion

                    #region Poll

                    using (RestRequest req = new RestRequest(node.HeartbeatUrl, System.Net.Http.HttpMethod.Get))
                    {
                        req.IgnoreCertificateErrors = _Settings.Rest.AcceptInvalidCerts;

                        RestResponse resp = null;

                        try
                        {
                            resp = await req.SendAsync(token);
                        }
                        catch (Exception)
                        {

                        }

                        if (resp == null)
                        {
                            if (node.Debug)
                                _Logging.Warn(_Header + "unable to connect to node " + node.Hostname + ":" + node.Port);

                            AddNodeFailure(host, node);
                        }
                        else if (resp.StatusCode != 200)
                        {
                            if (node.Debug)
                                _Logging.Warn(_Header + "node " + node.Hostname + ":" + node.Port + " inquiry failed");

                            AddNodeFailure(host, node);
                            resp.Dispose();
                        }
                        else
                        {
                            if (node.Debug)
                                _Logging.Debug(_Header + "node " + node.Hostname + ":" + node.Port + " inquiry succeeded");

                            AddNodeSuccess(host, node);
                            resp.Dispose();
                        }
                    }

                    #endregion
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _Logging.Exception(e);
                }
            }

            _Logging.Debug(_Header + "monitor thread exiting");
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
                _Logging.Warn(_Header + "unable to retrieve host with name " + host.Name);
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
                _Logging.Warn(_Header + "unable to retrieve node with hostname " + currNode.Hostname);
                return;
            }

            currNode.LastAttempt = DateTime.UtcNow.ToUniversalTime();
            currNode.LastFailure = currNode.LastAttempt;

            currNode.NumFailures = currNode.NumFailures + 1;
            if (currNode.NumFailures >= currNode.MaxFailures)
            {
                _Logging.Warn(_Header + "marking node " + currNode.Hostname + ":" + currNode.Port + " failed (" + currNode.NumFailures + " failures, max " + currNode.MaxFailures + ")");
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
                _Logging.Warn(_Header + "unable to retrieve host with name " + host.Name);
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
                _Logging.Warn(_Header + "unable to retrieve node with hostname " + currNode.Hostname);
                return;
            }

            currNode.LastAttempt = DateTime.UtcNow.ToUniversalTime();
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
