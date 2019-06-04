using System;
using System.Collections.Generic;

namespace Uscale.Classes
{
    /// <summary>
    /// Node that can service requests made to a virtual Host.
    /// </summary>
    public class Node
    {
        #region Public-Members

        /// <summary>
        /// Node's hostname.
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// The port on which the node is listening.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool Ssl { get; set; }

        /// <summary>
        /// Heartbeat URL that the loadbalancer should poll to determine whether or not the Node is online.
        /// </summary>
        public string HeartbeatUrl { get; set; }

        /// <summary>
        /// The interval in milliseconds by which the loadbalancer should poll the Node.
        /// </summary>
        public int PollingIntervalMsec { get; set; }

        /// <summary>
        /// Timestamp for the last attempt made by the loadbalancer to inquire against the Node.
        /// </summary>
        public DateTime? LastAttempt { get; set; }

        /// <summary>
        /// Timestamp for the last successful attempt made by the loadbalancer inquiring against the Node.
        /// </summary>
        public DateTime? LastSuccess { get; set; }

        /// <summary>
        /// Timestamp for the last failed attempt made by the loadbalancer inquiring against the Node.
        /// </summary>
        public DateTime? LastFailure { get; set; }

        /// <summary>
        /// Maximum number of failures tolerated before the Node is removed from rotation for this Host.
        /// </summary>
        public int MaxFailures { get; set; }

        /// <summary>
        /// Number of failures that have been encountered consecutively without a successful inquiry.
        /// </summary>
        public int? NumFailures { get; set; }

        /// <summary>
        /// Indicates if the Node is marked as failed.
        /// </summary>
        public bool Failed { get; set; }

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
