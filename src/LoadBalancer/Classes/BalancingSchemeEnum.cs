namespace Uscale.Classes
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Load-balancing scheme.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BalancingSchemeEnum
    {
        /// <summary>
        /// RoundRobin.
        /// </summary>
        [EnumMember(Value = "RoundRobin")]
        RoundRobin
    }
}
