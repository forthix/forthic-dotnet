using System;
using System.Globalization;
using System.Text.RegularExpressions;
using NodaTime;
using NodaTime.Text;

namespace Forthic;

// ============================================================================
// Boolean Literals
// ============================================================================

public static class BooleanLiterals
{
    /// <summary>
    /// Parse boolean literals: TRUE, FALSE
    /// </summary>
    public static object? ToBool(string str)
    {
        if (str == "TRUE") return true;
        if (str == "FALSE") return false;
        return null;
    }
}

// ============================================================================
// Numeric Literals
// ============================================================================

public static class NumericLiterals
{
    /// <summary>
    /// Parse float literals: 3.14, -2.5, 0.0
    /// Must contain a decimal point
    /// </summary>
    public static object? ToFloat(string str)
    {
        if (!str.Contains('.')) return null;
        if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }
        return null;
    }

    /// <summary>
    /// Parse integer literals: 42, -10, 0
    /// Must not contain a decimal point
    /// </summary>
    public static object? ToInt(string str)
    {
        if (str.Contains('.')) return null;
        if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result))
        {
            // Verify it's actually an integer string (not "42abc")
            if (result.ToString() == str)
            {
                return result;
            }
        }
        return null;
    }
}

// ============================================================================
// Time Literals
// ============================================================================

public static class TimeLiterals
{
    /// <summary>
    /// Parse time literals: 9:00, 11:30 PM, 22:15
    /// </summary>
    public static object? ToTime(string str)
    {
        // Pattern: HH:MM or HH:MM AM/PM
        var regex = new Regex(@"^(\d{1,2}):(\d{2})(?:\s*(AM|PM))?$");
        var match = regex.Match(str);
        if (!match.Success) return null;

        if (!int.TryParse(match.Groups[1].Value, out int hours)) return null;
        if (!int.TryParse(match.Groups[2].Value, out int minutes)) return null;
        string meridiem = match.Groups[3].Value;

        // Adjust for AM/PM
        if (meridiem == "PM" && hours < 12)
        {
            hours += 12;
        }
        else if (meridiem == "AM" && hours == 12)
        {
            hours = 0;
        }
        else if (meridiem == "AM" && hours > 12)
        {
            // Handle invalid cases like "22:15 AM"
            hours -= 12;
        }

        if (hours > 23 || minutes >= 60) return null;

        return new LocalTime(hours, minutes);
    }
}

// ============================================================================
// Date Literals
// ============================================================================

public static class DateLiterals
{
    /// <summary>
    /// Create a date literal handler
    /// Parses: 2020-06-05, YYYY-MM-DD (with wildcards)
    /// </summary>
    public static LiteralHandler ToLiteralDate(DateTimeZone timezone)
    {
        return (string str) =>
        {
            // Pattern: YYYY-MM-DD or wildcards (YYYY, MM, DD)
            var regex = new Regex(@"^(\d{4}|YYYY)-(\d{2}|MM)-(\d{2}|DD)$");
            var match = regex.Match(str);
            if (!match.Success) return null;

            var now = SystemClock.Instance.GetCurrentInstant().InZone(timezone).Date;

            int year = match.Groups[1].Value == "YYYY" ? now.Year : int.Parse(match.Groups[1].Value);
            int month = match.Groups[2].Value == "MM" ? now.Month : int.Parse(match.Groups[2].Value);
            int day = match.Groups[3].Value == "DD" ? now.Day : int.Parse(match.Groups[3].Value);

            try
            {
                return new LocalDate(year, month, day);
            }
            catch
            {
                return null;
            }
        };
    }
}

// ============================================================================
// ZonedDateTime Literals
// ============================================================================

public static class DateTimeLiterals
{
    /// <summary>
    /// Create a zoned datetime literal handler
    /// Parses:
    /// - 2025-05-24T10:15:00[America/Los_Angeles] (IANA named timezone, RFC 9557)
    /// - 2025-05-24T10:15:00-07:00[America/Los_Angeles] (offset + IANA timezone)
    /// - 2025-05-24T10:15:00Z (UTC)
    /// - 2025-05-24T10:15:00-05:00 (offset timezone)
    /// - 2025-05-24T10:15:00 (uses interpreter's timezone)
    /// </summary>
    public static LiteralHandler ToZonedDateTime(DateTimeZone timezone)
    {
        return (string str) =>
        {
            if (!str.Contains('T')) return null;

            try
            {
                // Handle IANA named timezone in bracket notation (RFC 9557)
                // Examples: 2025-05-20T08:00:00[America/Los_Angeles]
                //           2025-05-20T08:00:00-07:00[America/Los_Angeles]
                if (str.Contains('[') && str.EndsWith(']'))
                {
                    // NodaTime's ZonedDateTimePattern can parse RFC 9557 format
                    var pattern = ZonedDateTimePattern.ExtendedFormatOnlyIso;
                    var result = pattern.Parse(str);
                    if (result.Success)
                    {
                        return result.Value;
                    }

                    // Fallback: manually extract timezone
                    int bracketStart = str.IndexOf('[');
                    int bracketEnd = str.IndexOf(']');
                    string tzName = str.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                    string dtStr = str.Substring(0, bracketStart);

                    DateTimeZone tz;
                    try
                    {
                        tz = DateTimeZoneProviders.Tzdb[tzName];
                    }
                    catch
                    {
                        return null;
                    }

                    // Try parsing with offset
                    if (dtStr.Contains('+') || (dtStr.LastIndexOf('-') > 10))
                    {
                        var offsetPattern = OffsetDateTimePattern.ExtendedIso;
                        var offsetResult = offsetPattern.Parse(dtStr);
                        if (offsetResult.Success)
                        {
                            // Convert OffsetDateTime → Instant → ZonedDateTime (target timezone)
                            return offsetResult.Value.ToInstant().InZone(tz);
                        }
                    }

                    // Parse as plain datetime and assign timezone
                    var localDtPattern = LocalDateTimePattern.ExtendedIso;
                    var localDtResult = localDtPattern.Parse(dtStr);
                    if (localDtResult.Success)
                    {
                        return localDtResult.Value.InZoneLeniently(tz);
                    }

                    return null;
                }

                // Handle explicit UTC (Z suffix)
                if (str.EndsWith('Z'))
                {
                    var pattern = InstantPattern.ExtendedIso;
                    var result = pattern.Parse(str);
                    if (result.Success)
                    {
                        return result.Value.InUtc();
                    }
                    return null;
                }

                // Handle explicit timezone offset (+05:00, -05:00)
                var offsetRegex = new Regex(@"[+-]\d{2}:\d{2}$");
                if (offsetRegex.IsMatch(str))
                {
                    var offsetPattern = OffsetDateTimePattern.ExtendedIso;
                    var offsetResult = offsetPattern.Parse(str);
                    if (offsetResult.Success)
                    {
                        // Convert OffsetDateTime → Instant → ZonedDateTime (UTC)
                        return offsetResult.Value.ToInstant().InUtc();
                    }
                    return null;
                }

                // No timezone specified, use interpreter's timezone
                var localPattern = LocalDateTimePattern.ExtendedIso;
                var localResult = localPattern.Parse(str);
                if (localResult.Success)
                {
                    return localResult.Value.InZoneLeniently(timezone);
                }
                return null;
            }
            catch
            {
                return null;
            }
        };
    }
}
