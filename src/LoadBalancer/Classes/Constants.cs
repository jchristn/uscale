using System;

namespace Uscale.Classes
{
    /// <summary>
    /// Constants.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// Logo.
        /// </summary>
        public static string Logo =
                Environment.NewLine +
                @"                           __     " + Environment.NewLine +
                @"    __  ________________ _/ /__   " + Environment.NewLine +
                @"   / / / / ___/ ___/ __ `/ / _ \  " + Environment.NewLine +
                @"  / /_/ (__  ) /__/ /_/ / /  __/  " + Environment.NewLine +
                @"  \__,_/____/\___/\__,_/_/\___/   " + Environment.NewLine +
                Environment.NewLine;

        /// <summary>
        /// System settings file.
        /// </summary>
        public static string SystemSettingsFile = "./system.json";

        /// <summary>
        /// Message returned in response to a loopback API call.
        /// </summary>
        public static string LoopbackMessage = "Hello from uscale!";

        /// <summary>
        /// Content type for JSON data.
        /// </summary>
        public static string ContentTypeJson = "application/json";

        /// <summary>
        /// Content type for text data.
        /// </summary>
        public static string ContentTypeText = "text/plain";

        /// <summary>
        /// Content type for HTML data.
        /// </summary>
        public static string ContentTypeHtml = "text/html";

        /// <summary>
        /// Host header.
        /// </summary>
        public static string HeaderHost = "Host";

        /// <summary>
        /// X-Forwarded-For header.
        /// </summary>
        public static string HeaderForwarded = "X-Forwarded-For";
    }
}
