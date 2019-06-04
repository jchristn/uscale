using System;
using SyslogLogging;
using WatsonWebserver;

namespace Uscale.Classes
{
    /// <summary>
    /// Metadata about connection from a client.
    /// </summary>
    public class Connection
    {
        #region Public-Members

        /// <summary>
        /// Thread ID.
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// Client's IP address.
        /// </summary>
        public string SourceIp { get; set; }

        /// <summary>
        /// Client's port number.
        /// </summary>
        public int SourcePort { get; set; }

        /// <summary>
        /// HTTP method.
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// Raw URL.
        /// </summary>
        public string RawUrl { get; set; }

        /// <summary>
        /// Hostname being accessed.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// HTTP hostname as specified in the header.
        /// </summary>
        public string HttpHostName { get; set; }

        /// <summary>
        /// Name of the node to which this client is connected.
        /// </summary>
        public string NodeName { get; set; }

        /// <summary>
        /// Start time for the connection.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time for the connection.
        /// </summary>
        public DateTime? EndTime { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Connection()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
