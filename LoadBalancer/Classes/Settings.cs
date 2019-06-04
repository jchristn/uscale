using System;
using System.Collections.Generic;

namespace Uscale.Classes
{
    /// <summary>
    /// Loadbalancer settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable the console.
        /// </summary>
        public bool EnableConsole;

        /// <summary>
        /// Status code to use while redirecting HTTP requests.
        /// </summary>
        public int RedirectStatusCode;

        /// <summary>
        /// Status code text string to use while redirecting HTTP requests.
        /// </summary>
        public string RedirectStatusString;

        /// <summary>
        /// List of virtual Hosts accessible through the loadbalancer.
        /// </summary>
        public List<Host> Hosts;

        /// <summary>
        /// Server settings.
        /// </summary>
        public SettingsServer Server;

        /// <summary>
        /// Authentication settings.
        /// </summary>
        public SettingsAuth Auth;

        /// <summary>
        /// Logging settings.
        /// </summary>
        public SettingsLogging Logging;

        /// <summary>
        /// REST settings.
        /// </summary>
        public SettingsRest Rest;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Settings()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Load settings from file.
        /// </summary>
        /// <param name="filename">Filename and path.</param>
        /// <returns>Server settings.</returns>
        public static Settings FromFile(string filename)
        {
            return Common.DeserializeJson<Settings>(Common.ReadTextFile(filename));
        }

        #endregion

        #region Private-Methods

        #endregion
    }
    
    /// <summary>
    /// Server settings.
    /// </summary>
    public class SettingsServer
    {
        #region Public-Members

        /// <summary>
        /// Hostname on which the loadbalancer is listening.
        /// </summary>
        public string DnsHostname;

        /// <summary>
        /// TCP port number on which the loadbalancer is listening.
        /// </summary>
        public int Port;

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool Ssl;
        
        #endregion
    }
      
    /// <summary>
    /// Authentication settings.
    /// </summary>
    public class SettingsAuth
    {
        #region Public-Members

        /// <summary>
        /// API key header for admin APIs.
        /// </summary>
        public string AdminApiKeyHeader;

        /// <summary>
        /// Admin API key.
        /// </summary>
        public string AdminApiKey;

        #endregion
    }

    /// <summary>
    /// Logging settings.
    /// </summary>
    public class SettingsLogging
    {
        #region Public-Members

        /// <summary>
        /// IP address of the syslog server.
        /// </summary>
        public string SyslogServerIp;

        /// <summary>
        /// Port number of the syslog server.
        /// </summary>
        public int SyslogServerPort;

        /// <summary>
        /// Minimum severity level required to send a log message.
        /// </summary>
        public int MinimumSeverityLevel;

        /// <summary>
        /// Enable or disable logging of incoming requests.
        /// </summary>
        public bool LogRequests;

        /// <summary>
        /// Enable or disable logging of outgoing responses.
        /// </summary>
        public bool LogResponses;

        /// <summary>
        /// Enable or disable console logging.
        /// </summary>
        public bool ConsoleLogging;

        #endregion
    }
    
    /// <summary>
    /// REST settings.
    /// </summary>
    public class SettingsRest
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable use of a web proxy for outgoing requests.
        /// </summary>
        public bool UseWebProxy;

        /// <summary>
        /// Accept SSL certificates that cannot be validated.
        /// </summary>
        public bool AcceptInvalidCerts;

        /// <summary>
        /// URL for the web proxy.
        /// </summary>
        public string WebProxyUrl;

        #endregion
    }
}
