using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Winch.Core;

namespace ArchipelagoDredge.Network;

/// <summary>
///     Reads slot-data values without caring whether the APWorld sent them as an int, a bool or a string.
/// </summary>
public static class SlotDataParser
{
    public static bool GetBool(Dictionary<string, object> slotData, string key, bool defaultValue = false)
    {
        if (slotData == null || !slotData.TryGetValue(key, out var value))
        {
            WinchCore.Log.Debug($"Slot data '{key}' missing; using {defaultValue}");
            return defaultValue;
        }

        if (TryParseBool(value, out var result))
        {
            return result;
        }

        WinchCore.Log.Warn($"Slot data '{key}' has unexpected value '{value}'; using {defaultValue}");
        return defaultValue;
    }

    private static bool TryParseBool(object value, out bool result)
    {
        switch (value)
        {
            case JValue jValue:
                return TryParseBool(jValue.Value, out result);
            case bool b:
                result = b;
                return true;
            case long or int or short or byte:
                result = Convert.ToInt64(value) != 0;
                return true;
            case double or float or decimal:
                result = Convert.ToDouble(value) != 0;
                return true;
            case string s when bool.TryParse(s, out var parsedBool):
                result = parsedBool;
                return true;
            case string s when long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong):
                result = parsedLong != 0;
                return true;
            default:
                result = false;
                return false;
        }
    }
}
