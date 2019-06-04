﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Uscale.Classes
{
    /// <summary>
    /// Load-balancing scheme.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BalancingScheme
    {
        [EnumMember(Value = "RoundRobin")]
        RoundRobin
    }
}
