namespace Uscale.Classes
{
    using System;

    /// <summary>
    /// Node that can service requests made to a virtual Host.
    /// </summary>
    public class Node
    {
        #region Public-Members

        /// <summary>
        /// Node's hostname.
        /// </summary>
        public string Hostname { get; set; } = null;

        /// <summary>
        /// The port on which the node is listening.
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool Ssl { get; set; } = false;

        /// <summary>
        /// Heartbeat URL that the loadbalancer should poll to determine whether or not the Node is online.
        /// </summary>
        public string HeartbeatUrl { get; set; } = null;

        /// <summary>
        /// The interval in milliseconds by which the loadbalancer should poll the Node.
        /// </summary>
        public int PollingIntervalMsec { get; set; } = 5000;

        /// <summary>
        /// Timestamp for the last attempt made by the loadbalancer to inquire against the Node.
        /// </summary>
        public DateTime? LastAttempt { get; set; } = null;

        /// <summary>
        /// Timestamp for the last successful attempt made by the loadbalancer inquiring against the Node.
        /// </summary>
        public DateTime? LastSuccess { get; set; } = null;

        /// <summary>
        /// Timestamp for the last failed attempt made by the loadbalancer inquiring against the Node.
        /// </summary>
        public DateTime? LastFailure { get; set; } = null;

        /// <summary>
        /// Maximum number of failures tolerated before the Node is removed from rotation for this Host.
        /// </summary>
        public int MaxFailures { get; set; } = 5;

        /// <summary>
        /// Number of failures that have been encountered consecutively without a successful inquiry.
        /// </summary>
        public int NumFailures { get; set; } = 0;

        /// <summary>
        /// Indicates if the Node is marked as failed.
        /// </summary>
        public bool Failed { get; set; } = false;

        /// <summary>
        /// Enable or disable debug logging for this node.
        /// </summary>
        public bool Debug { get; set; } = false;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Node()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    } 
}
