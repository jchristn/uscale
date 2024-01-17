namespace Uscale.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Context manager.
    /// </summary>
    public class ContextManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private string _Header = "[ContextManager] ";
        private LoggingModule _Logging = null;
        private List<Connection> _Connections = new List<Connection>();
        private readonly object _Lock = new object();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="logging">LoggingModule instance.</param>
        public ContextManager(LoggingModule logging)
        {
            if (logging == null) throw new ArgumentNullException(nameof(logging));

            _Logging = logging;
            _Connections = new List<Connection>();
            _Lock = new object();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add a connection.
        /// </summary>
        /// <param name="threadId">Thread ID.</param>
        /// <param name="ctx">HTTP context.</param>
        public void Add(int threadId, HttpContextBase ctx)
        {
            if (threadId <= 0) return;
            if (ctx == null) return;

            Connection conn = new Connection();
            conn.ThreadId = threadId;
            conn.SourceIp = ctx.Request.Source.IpAddress;
            conn.SourcePort = ctx.Request.Source.Port;
            conn.Method = ctx.Request.Method;
            conn.RawUrl = ctx.Request.Url.RawWithoutQuery;
            conn.StartTime = DateTime.UtcNow;
            conn.EndTime = DateTime.UtcNow;

            lock (_Lock)
            {
                _Connections.Add(conn);
            }
        }

        /// <summary>
        /// Close a connection.
        /// </summary>
        /// <param name="threadId">Thread ID.</param>
        public void Close(int threadId)
        {
            if (threadId <= 0) return;

            lock (_Lock)
            {
                _Connections = _Connections.Where(x => x.ThreadId != threadId).ToList();
            }
        }
         
        /// <summary>
        /// Update an existing connection.
        /// </summary>
        /// <param name="threadId">Thread ID.</param>
        /// <param name="hostName">Hostname.</param>
        /// <param name="httpHostName">HTTP hostname as specified in the header.</param>
        /// <param name="nodeName">Node name to which the client is connected.</param>
        public void Update(int threadId, string hostName, string httpHostName, string nodeName)
        {
            if (threadId <= 0) return;

            lock (_Lock)
            {
                Connection curr = _Connections.FirstOrDefault(i => i.ThreadId == threadId);
                if (curr == null || curr == default(Connection))
                {
                    _Logging.Warn(_Header + "unable to find connection on thread ID " + threadId);
                    return;
                }

                _Connections.Remove(curr);
                curr.HostName = hostName;
                curr.HttpHostName = httpHostName;
                curr.NodeName = nodeName;
                _Connections.Add(curr);
            }

        }

        /// <summary>
        /// Retrieve active connections.
        /// </summary>
        /// <returns>List of Connection objects.</returns>
        public List<Connection> GetActiveConnections()
        {
            lock (_Lock)
            {
                return new List<Connection>(_Connections);
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
