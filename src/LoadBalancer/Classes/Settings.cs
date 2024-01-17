namespace Uscale.Classes
{
    using System.Collections.Generic;
    using WatsonWebserver.Core;

    /// <summary>
    /// Loadbalancer settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable the console.
        /// </summary>
        public bool EnableConsole { get; set; } = true;

        /// <summary>
        /// Status code to use while redirecting HTTP requests.
        /// </summary>
        public int RedirectStatusCode { get; set; } = 302;

        /// <summary>
        /// Status code text string to use while redirecting HTTP requests.
        /// </summary>
        public string RedirectStatusString { get; set; } = "Found";

        /// <summary>
        /// List of virtual Hosts accessible through the loadbalancer.
        /// </summary>
        public List<Host> Hosts { get; set; } = new List<Host>();

        /// <summary>
        /// Server settings.
        /// </summary>
        public SettingsServer Server { get; set; } = new SettingsServer();

        /// <summary>
        /// Authentication settings.
        /// </summary>
        public SettingsAuth Auth { get; set; } = new SettingsAuth();

        /// <summary>
        /// Logging settings.
        /// </summary>
        public SettingsLogging Logging { get; set; } = new SettingsLogging();

        /// <summary>
        /// REST settings.
        /// </summary>
        public SettingsRest Rest { get; set; } = new SettingsRest();

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
        /// Create an instance of WebserverSettings from Settings.
        /// </summary>
        /// <returns></returns>
        public WebserverSettings ToWebserverSettings()
        {
            WebserverSettings ret = new WebserverSettings();

            ret.Hostname = Server.DnsHostname;
            ret.Port = Server.Port;
            ret.Ssl.Enable = Server.Ssl;

            return ret;
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
        public string DnsHostname { get; set; } = null;

        /// <summary>
        /// TCP port number on which the loadbalancer is listening.
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// Enable or disable SSL.
        /// </summary>
        public bool Ssl { get; set; } = false;
        
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
        public string AdminApiKeyHeader { get; set; } = "x-api-key";

        /// <summary>
        /// Admin API key.
        /// </summary>
        public string AdminApiKey { get; set; } = "uscaleadmin";

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
        public string SyslogServerIp { get; set; } = "127.0.0.1";

        /// <summary>
        /// Port number of the syslog server.
        /// </summary>
        public int SyslogServerPort { get; set; } = 514;

        /// <summary>
        /// Minimum severity level required to send a log message.
        /// </summary>
        public int MinimumSeverityLevel { get; set; } = 0;

        /// <summary>
        /// Enable or disable logging of incoming requests.
        /// </summary>
        public bool LogRequests { get; set; } = false;

        /// <summary>
        /// Enable or disable logging of outgoing responses.
        /// </summary>
        public bool LogResponses { get; set; } = false;

        /// <summary>
        /// Enable or disable console logging.
        /// </summary>
        public bool ConsoleLogging { get; set; } = false;

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
        public bool UseWebProxy { get; set; } = false;

        /// <summary>
        /// Accept SSL certificates that cannot be validated.
        /// </summary>
        public bool AcceptInvalidCerts { get; set; } = true;

        /// <summary>
        /// URL for the web proxy.
        /// </summary>
        public string WebProxyUrl { get; set; } = "";

        #endregion
    }
}
