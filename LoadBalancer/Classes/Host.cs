using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uscale.Classes
{
    /// <summary>
    /// Virtual host exposed by the loadbalancer.
    /// </summary>
    public class Host
    {
        #region Public-Members

        /// <summary>
        /// Name of the host.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// HTTP hostnames on which the loadbalancer should listen for this host.
        /// </summary>
        public List<string> HttpHostNames { get; set; }

        /// <summary>
        /// List of Nodes that are able to service requests for this host.
        /// </summary>
        public List<Node> Nodes { get; set; }

        /// <summary>
        /// The last index used when routing a client request.
        /// </summary>
        public int LastIndex { get; set; }

        /// <summary>
        /// The balancing scheme used for distributing load.
        /// </summary>
        public BalancingScheme BalancingScheme { get; set; } // RoundRobin

        /// <summary>
        /// How the loadbalancer handles incoming requests and services backend requests to a Node.
        /// </summary>
        public HandlingMode HandlingMode { get; set; } // Proxy, Redirect

        /// <summary>
        /// Accept or reject certificates that cannot be validated.
        /// </summary>
        public bool AcceptInvalidCerts { get; set; }

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
