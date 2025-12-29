using System;

namespace Forthic.Modules.Standard;

/// <summary>
/// Common helper functions shared across standard modules
/// </summary>
public static class Helpers
{
    public static bool IsTruthy(object? val)
    {
        if (val == null) return false;

        return val switch
        {
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => d != 0.0,
            string s => s != "",
            _ => true  // Objects/arrays are truthy
        };
    }

    public static bool AreEqual(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // Try direct comparison for same types
        switch (a)
        {
            case string aStr when b is string bStr:
                return aStr == bStr;
            case bool aBool when b is bool bBool:
                return aBool == bBool;
        }

        // Numeric comparisons with type coercion
        var aNum = ToNumber(a);
        var bNum = ToNumber(b);
        if (aNum.HasValue && bNum.HasValue)
        {
            return Math.Abs(aNum.Value - bNum.Value) < double.Epsilon;
        }

        return false;
    }

    public static double? ToNumber(object? val)
    {
        return val switch
        {
            int i => i,
            long l => l,
            double d => d,
            float f => f,
            decimal dec => (double)dec,
            _ => null
        };
    }

    public static string ToString(object? val)
    {
        if (val == null) return "";
        if (val is string s) return s;
        if (val is int i) return i.ToString();
        if (val is long l) return l.ToString();
        if (val is double d) return d.ToString("G");  // General format (no trailing zeros)
        if (val is bool b) return b ? "true" : "false";
        return val.ToString() ?? "";
    }

    public static string ToLowerCase(object? val)
    {
        return ToString(val).ToLower();
    }

    public static int ToInt(object? val)
    {
        return val switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            _ => 0
        };
    }

    public static Exception ForthicError(string message)
    {
        return new Exception(message);
    }
}
