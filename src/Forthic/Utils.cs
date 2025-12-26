using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NodaTime;
using NodaTime.Text;

namespace Forthic;

// ============================================================================
// Type Checking Utilities
// ============================================================================

public static class TypeUtils
{
    public static bool IsInt(object? value)
    {
        return value is int or long or short or byte or sbyte or uint or ulong or ushort;
    }

    public static bool IsFloat(object? value)
    {
        return value is float or double or decimal;
    }

    public static bool IsString(object? value)
    {
        return value is string;
    }

    public static bool IsBool(object? value)
    {
        return value is bool;
    }

    public static bool IsArray(object? value)
    {
        return value is System.Collections.IList;
    }

    public static bool IsRecord(object? value)
    {
        return value is System.Collections.IDictionary;
    }

    public static long ToInt(object? value)
    {
        return value switch
        {
            null => throw new InvalidCastException("Cannot convert null to int"),
            int i => i,
            long l => l,
            short s => s,
            byte b => b,
            sbyte sb => sb,
            uint ui => ui,
            ulong ul => (long)ul,
            ushort us => us,
            float f => (long)f,
            double d => (long)d,
            decimal dec => (long)dec,
            string str => long.Parse(str),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to int")
        };
    }

    public static double ToFloat(object? value)
    {
        return value switch
        {
            null => throw new InvalidCastException("Cannot convert null to float"),
            float f => f,
            double d => d,
            decimal dec => (double)dec,
            int i => i,
            long l => l,
            short s => s,
            byte b => b,
            sbyte sb => sb,
            uint ui => ui,
            ulong ul => ul,
            ushort us => us,
            string str => double.Parse(str),
            _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to float")
        };
    }

    public static string ToString(object? value)
    {
        return value switch
        {
            null => "null",
            string s => s,
            bool b => b ? "true" : "false",
            _ => value.ToString() ?? "null"
        };
    }
}

// ============================================================================
// String Utilities
// ============================================================================

public static class StringUtils
{
    public static string Trim(string s)
    {
        return s.Trim();
    }

    public static string[] Split(string s, string separator)
    {
        if (string.IsNullOrEmpty(separator))
        {
            // Split into individual characters
            return s.Select(c => c.ToString()).ToArray();
        }
        return s.Split(new[] { separator }, StringSplitOptions.None);
    }

    public static string Join(IEnumerable<string> parts, string separator)
    {
        return string.Join(separator, parts);
    }

    public static string Replace(string s, string oldValue, string newValue)
    {
        return s.Replace(oldValue, newValue);
    }
}

// ============================================================================
// Date/Time Utilities
// ============================================================================

public static class DateTimeUtils
{
    /// <summary>
    /// Parse date in YYYY-MM-DD format
    /// Supports wildcards: YYYY-**-**, ****-MM-**, ****-**-DD
    /// </summary>
    public static LocalDate ParseDate(string s)
    {
        if (s.Contains('*'))
        {
            return ParseDateWithWildcards(s);
        }

        var pattern = LocalDatePattern.Iso;
        var result = pattern.Parse(s);
        if (!result.Success)
        {
            throw new FormatException($"Invalid date format: {s}");
        }
        return result.Value;
    }

    private static LocalDate ParseDateWithWildcards(string s)
    {
        var now = SystemClock.Instance.GetCurrentInstant()
            .InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault())
            .Date;

        var parts = s.Split('-');
        if (parts.Length != 3)
        {
            throw new FormatException($"Invalid date format: {s}");
        }

        var year = parts[0] == "****" ? now.Year : int.Parse(parts[0]);
        var month = parts[1] == "**" ? now.Month : int.Parse(parts[1]);
        var day = parts[2] == "**" ? now.Day : int.Parse(parts[2]);

        return new LocalDate(year, month, day);
    }

    /// <summary>
    /// Parse time in HH:MM or HH:MM:SS format
    /// Also supports 12-hour format with AM/PM (e.g., "2:30 PM")
    /// </summary>
    public static LocalTime ParseTime(string s)
    {
        s = s.Trim();

        // Check for AM/PM format
        var ampmRegex = new Regex(@"^(\d{1,2}):(\d{2})\s*(AM|PM)$", RegexOptions.IgnoreCase);
        var match = ampmRegex.Match(s);
        if (match.Success)
        {
            var hour = int.Parse(match.Groups[1].Value);
            var minute = int.Parse(match.Groups[2].Value);
            var meridiem = match.Groups[3].Value.ToUpper();

            if (meridiem == "PM" && hour < 12)
            {
                hour += 12;
            }
            else if (meridiem == "AM" && hour == 12)
            {
                hour = 0;
            }

            return new LocalTime(hour, minute);
        }

        // Try HH:MM:SS format
        var longPattern = LocalTimePattern.ExtendedIso;
        var longResult = longPattern.Parse(s);
        if (longResult.Success)
        {
            return longResult.Value;
        }

        // Try HH:MM format
        var parts = s.Split(':');
        if (parts.Length == 2)
        {
            var hour = int.Parse(parts[0]);
            var minute = int.Parse(parts[1]);
            return new LocalTime(hour, minute);
        }

        throw new FormatException($"Invalid time format: {s}");
    }

    /// <summary>
    /// Format date as YYYY-MM-DD
    /// </summary>
    public static string FormatDate(LocalDate date)
    {
        return LocalDatePattern.Iso.Format(date);
    }

    /// <summary>
    /// Format time as HH:MM
    /// </summary>
    public static string FormatTime(LocalTime time)
    {
        return time.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Format datetime as RFC3339
    /// </summary>
    public static string FormatDateTime(ZonedDateTime dateTime)
    {
        return dateTime.ToString("uyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
    }

    public static string FormatDateTime(LocalDateTime dateTime)
    {
        return dateTime.ToString("uyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parse RFC3339 datetime string
    /// </summary>
    public static ZonedDateTime ParseDateTime(string s)
    {
        var pattern = InstantPattern.ExtendedIso;
        var result = pattern.Parse(s);
        if (!result.Success)
        {
            throw new FormatException($"Invalid datetime format: {s}");
        }

        return result.Value.InUtc();
    }

    /// <summary>
    /// Convert date to YYYYMMDD integer format
    /// </summary>
    public static long DateToInt(LocalDate date)
    {
        return date.Year * 10000L + date.Month * 100L + date.Day;
    }

    /// <summary>
    /// Convert YYYYMMDD integer to date
    /// </summary>
    public static LocalDate IntToDate(long n)
    {
        var year = (int)(n / 10000);
        var month = (int)((n % 10000) / 100);
        var day = (int)(n % 100);
        return new LocalDate(year, month, day);
    }
}
