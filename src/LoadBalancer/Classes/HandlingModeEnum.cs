namespace Uscale.Classes
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Handling mode.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HandlingModeEnum
    {
        /// <summary>
        /// Redirect.
        /// </summary>
        [EnumMember(Value = "Redirect")]
        Redirect,
        /// <summary>
        /// Proxy.
        /// </summary>
        [EnumMember(Value = "Proxy")]
        Proxy
    }
}
