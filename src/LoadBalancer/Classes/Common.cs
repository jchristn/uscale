namespace Uscale.Classes
{
    using System;
    using System.IO;

    /// <summary>
    /// Static methods.
    /// </summary>
    public static class Common
    {
        #region Environment
         
        public static void ExitApplication(string method, string text, int returnCode)
        {
            Console.WriteLine("---");
            Console.WriteLine("");
            Console.WriteLine("The application has exited.");
            Console.WriteLine("");
            Console.WriteLine("  Requested by : " + method);
            Console.WriteLine("  Reason text  : " + text);
            Console.WriteLine("");
            Console.WriteLine("---");
            Environment.Exit(returnCode);
            return;
        }
         
        #endregion

        #region Misc

        public static double TotalMsFrom(DateTime startTime)
        {
            try
            {
                DateTime endTime = DateTime.UtcNow;
                TimeSpan totalTime = (endTime - startTime);
                return totalTime.TotalMilliseconds;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static bool IsLaterThanNow(DateTime dt)
        {
            if (DateTime.Compare(dt, DateTime.UtcNow) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ContainsUnsafeCharacters(string data)
        {
            /*
             * 
             * Returns true if unsafe characters exist
             * 
             * 
             */

            // see https://kb.acronis.com/content/39790

            if (String.IsNullOrEmpty(data)) return false;
            if (data.Equals(".")) return true;
            if (data.Equals("..")) return true;

            if (
                (String.Compare(data.ToLower(), "com1") == 0) ||
                (String.Compare(data.ToLower(), "com2") == 0) ||
                (String.Compare(data.ToLower(), "com3") == 0) ||
                (String.Compare(data.ToLower(), "com4") == 0) ||
                (String.Compare(data.ToLower(), "com5") == 0) ||
                (String.Compare(data.ToLower(), "com6") == 0) ||
                (String.Compare(data.ToLower(), "com7") == 0) ||
                (String.Compare(data.ToLower(), "com8") == 0) ||
                (String.Compare(data.ToLower(), "com9") == 0) ||
                (String.Compare(data.ToLower(), "lpt1") == 0) ||
                (String.Compare(data.ToLower(), "lpt2") == 0) ||
                (String.Compare(data.ToLower(), "lpt3") == 0) ||
                (String.Compare(data.ToLower(), "lpt4") == 0) ||
                (String.Compare(data.ToLower(), "lpt5") == 0) ||
                (String.Compare(data.ToLower(), "lpt6") == 0) ||
                (String.Compare(data.ToLower(), "lpt7") == 0) ||
                (String.Compare(data.ToLower(), "lpt8") == 0) ||
                (String.Compare(data.ToLower(), "lpt9") == 0) ||
                (String.Compare(data.ToLower(), "con") == 0) ||
                (String.Compare(data.ToLower(), "nul") == 0) ||
                (String.Compare(data.ToLower(), "prn") == 0) ||
                (String.Compare(data.ToLower(), "con") == 0)
                )
            {
                return true;
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (
                    ((int)(data[i]) < 32) ||    // below range
                    ((int)(data[i]) > 126) ||   // above range
                    ((int)(data[i]) == 47) ||   // slash /
                    ((int)(data[i]) == 92) ||   // backslash \
                    ((int)(data[i]) == 63) ||   // question mark ?
                    ((int)(data[i]) == 60) ||   // less than < 
                    ((int)(data[i]) == 62) ||   // greater than >
                    ((int)(data[i]) == 58) ||   // colon :
                    ((int)(data[i]) == 42) ||   // asterisk *
                    ((int)(data[i]) == 124) ||  // pipe |
                    ((int)(data[i]) == 34) ||   // double quote "
                    ((int)(data[i]) == 39) ||   // single quote '
                    ((int)(data[i]) == 94)      // caret ^
                    )
                {
                    return true;
                }
            }

            return false;
        }

        public static System.Net.Http.HttpMethod WatsonHttpMethodToSystemNetHttpMethod(WatsonWebserver.Core.HttpMethod method)
        {
            switch (method)
            {
                case WatsonWebserver.Core.HttpMethod.DELETE:
                    return System.Net.Http.HttpMethod.Delete;
                case WatsonWebserver.Core.HttpMethod.GET:
                    return System.Net.Http.HttpMethod.Get;
                case WatsonWebserver.Core.HttpMethod.HEAD:
                    return System.Net.Http.HttpMethod.Head;
                case WatsonWebserver.Core.HttpMethod.OPTIONS:
                    return System.Net.Http.HttpMethod.Options;
                case WatsonWebserver.Core.HttpMethod.PATCH:
#if NET6_0_OR_GREATER
                    return System.Net.Http.HttpMethod.Patch;
#else
                    throw new ArgumentException("Unsupported HTTP method '" + method.ToString() + "'.");
#endif
                case WatsonWebserver.Core.HttpMethod.POST:
                    return System.Net.Http.HttpMethod.Post;
                case WatsonWebserver.Core.HttpMethod.PUT:
                    return System.Net.Http.HttpMethod.Put;
                case WatsonWebserver.Core.HttpMethod.TRACE:
                    return System.Net.Http.HttpMethod.Trace;
                case WatsonWebserver.Core.HttpMethod.UNKNOWN:
                default:
                    throw new ArgumentException("Unknown HTTP method '" + method.ToString() + "'.");
            }
        }

#endregion
    }
}
