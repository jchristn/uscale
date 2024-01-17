namespace Uscale.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Virtual host exposed by the loadbalancer.
    /// </summary>
    public class Host
    {
        #region Public-Members

        /// <summary>
        /// Name of the host.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// HTTP hostnames on which the loadbalancer should listen for this host.
        /// </summary>
        public List<string> HttpHostNames { get; set; } = new List<string>();

        /// <summary>
        /// List of Nodes that are able to service requests for this host.
        /// </summary>
        public List<Node> Nodes { get; set; } = new List<Node>();

        /// <summary>
        /// The last index used when routing a client request.
        /// </summary>
        public int LastIndex { get; set; } = 0;

        /// <summary>
        /// The balancing scheme used for distributing load.
        /// </summary>
        public BalancingSchemeEnum BalancingScheme { get; set; } = BalancingSchemeEnum.RoundRobin;

        /// <summary>
        /// How the loadbalancer handles incoming requests and services backend requests to a Node.
        /// </summary>
        public HandlingModeEnum HandlingMode { get; set; } = HandlingModeEnum.Proxy;

        /// <summary>
        /// Accept or reject certificates that cannot be validated.
        /// </summary>
        public bool AcceptInvalidCerts { get; set; } = false;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Host()
        {

        }

        #endregion

        #region Public-Members

        #endregion

        #region Private-Members

        #endregion
    }
}
