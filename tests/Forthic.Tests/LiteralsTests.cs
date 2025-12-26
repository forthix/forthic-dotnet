using Xunit;
using Forthic;
using NodaTime;

namespace Forthic.Tests;

public class LiteralsTests
{
    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("FALSE", false)]
    public void ToBool_Valid(string input, bool expected)
    {
        var result = BooleanLiterals.ToBool(input);
        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("invalid")]
    public void ToBool_Invalid(string input)
    {
        var result = BooleanLiterals.ToBool(input);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("42", 42L)]
    [InlineData("-10", -10L)]
    [InlineData("0", 0L)]
    [InlineData("1000000", 1000000L)]
    public void ToInt_Valid(string input, long expected)
    {
        var result = NumericLiterals.ToInt(input);
        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("3.14")]
    [InlineData("abc")]
    [InlineData("42abc")]
    public void ToInt_Invalid(string input)
    {
        var result = NumericLiterals.ToInt(input);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData("-2.5", -2.5)]
    [InlineData("0.0", 0.0)]
    public void ToFloat_Valid(string input, double expected)
    {
        var result = NumericLiterals.ToFloat(input);
        Assert.NotNull(result);
        Assert.Equal(expected, (double)result, precision: 4);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("abc")]
    public void ToFloat_Invalid(string input)
    {
        var result = NumericLiterals.ToFloat(input);
        Assert.Null(result);
    }

    [Theory]
    [InlineData("9:00", 9, 0)]
    [InlineData("14:30", 14, 30)]
    [InlineData("2:30 PM", 14, 30)]
    [InlineData("9:00 AM", 9, 0)]
    [InlineData("12:00 PM", 12, 0)]
    [InlineData("12:00 AM", 0, 0)]
    public void ToTime_Valid(string input, int expectedHour, int expectedMinute)
    {
        var result = TimeLiterals.ToTime(input);
        Assert.NotNull(result);
        var time = (LocalTime)result;
        Assert.Equal(expectedHour, time.Hour);
        Assert.Equal(expectedMinute, time.Minute);
    }

    [Fact]
    public void ToTime_Invalid()
    {
        var result = TimeLiterals.ToTime("25:00");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("2020-06-05", 2020, 6, 5)]
    public void ToLiteralDate_Valid(string input, int expectedYear, int expectedMonth, int expectedDay)
    {
        var handler = DateLiterals.ToLiteralDate(DateTimeZone.Utc);
        var result = handler(input);
        Assert.NotNull(result);
        var date = (LocalDate)result;
        Assert.Equal(expectedYear, date.Year);
        Assert.Equal(expectedMonth, date.Month);
        Assert.Equal(expectedDay, date.Day);
    }

    [Fact]
    public void ToLiteralDate_YearWildcard()
    {
        var handler = DateLiterals.ToLiteralDate(DateTimeZone.Utc);
        var result = handler("YYYY-06-05");
        Assert.NotNull(result);
        var date = (LocalDate)result;
        // Year will be current year
        Assert.Equal(6, date.Month);
        Assert.Equal(5, date.Day);
    }

    [Fact]
    public void ToLiteralDate_MonthWildcard()
    {
        var handler = DateLiterals.ToLiteralDate(DateTimeZone.Utc);
        var result = handler("2020-MM-05");
        Assert.NotNull(result);
        var date = (LocalDate)result;
        Assert.Equal(2020, date.Year);
        // Month will be current month
        Assert.Equal(5, date.Day);
    }

    [Fact]
    public void ToLiteralDate_DayWildcard()
    {
        var handler = DateLiterals.ToLiteralDate(DateTimeZone.Utc);
        var result = handler("2020-06-DD");
        Assert.NotNull(result);
        var date = (LocalDate)result;
        Assert.Equal(2020, date.Year);
        Assert.Equal(6, date.Month);
        // Day will be current day
    }

    [Fact]
    public void ToLiteralDate_AllWildcards()
    {
        var handler = DateLiterals.ToLiteralDate(DateTimeZone.Utc);
        var result = handler("YYYY-MM-DD");
        Assert.NotNull(result);
        // Should return a valid date with current values
    }

    [Theory]
    [InlineData("2020/06/05")]
    [InlineData("not-a-date")]
    public void ToLiteralDate_Invalid(string input)
    {
        var handler = DateLiterals.ToLiteralDate(DateTimeZone.Utc);
        var result = handler(input);
        Assert.Null(result);
    }

    [Fact]
    public void ToZonedDateTime_UTC()
    {
        var handler = DateTimeLiterals.ToZonedDateTime(DateTimeZone.Utc);
        var result = handler("2025-05-24T10:15:00Z");
        Assert.NotNull(result);
        var dt = (ZonedDateTime)result;
        Assert.Equal(2025, dt.Year);
        Assert.Equal(5, dt.Month);
        Assert.Equal(24, dt.Day);
        Assert.Equal(10, dt.Hour);
        Assert.Equal(15, dt.Minute);
    }

    [Fact]
    public void ToZonedDateTime_Offset()
    {
        var handler = DateTimeLiterals.ToZonedDateTime(DateTimeZone.Utc);
        var result = handler("2025-05-24T10:15:00-05:00");
        Assert.NotNull(result);
        var dt = (ZonedDateTime)result;
        Assert.Equal(2025, dt.Year);
    }

    [Fact]
    public void ToZonedDateTime_Plain()
    {
        var handler = DateTimeLiterals.ToZonedDateTime(DateTimeZone.Utc);
        var result = handler("2025-05-24T10:15:00");
        Assert.NotNull(result);
        var dt = (ZonedDateTime)result;
        Assert.Equal(2025, dt.Year);
        Assert.Equal(5, dt.Month);
        Assert.Equal(24, dt.Day);
    }

    [Fact]
    public void ToZonedDateTime_IANA()
    {
        var handler = DateTimeLiterals.ToZonedDateTime(DateTimeZone.Utc);
        var result = handler("2025-05-20T08:00:00[America/Los_Angeles]");
        Assert.NotNull(result);
        var dt = (ZonedDateTime)result;
        Assert.Equal(2025, dt.Year);
        Assert.Equal(5, dt.Month);
        Assert.Equal(20, dt.Day);
    }

    [Fact]
    public void ToZonedDateTime_OffsetWithIANA()
    {
        var handler = DateTimeLiterals.ToZonedDateTime(DateTimeZone.Utc);
        var result = handler("2025-05-20T08:00:00-07:00[America/Los_Angeles]");
        Assert.NotNull(result);
        var dt = (ZonedDateTime)result;
        Assert.Equal(2025, dt.Year);
    }

    [Theory]
    [InlineData("not-a-datetime")]
    [InlineData("2025-05-24 10:15:00")]
    public void ToZonedDateTime_Invalid(string input)
    {
        var handler = DateTimeLiterals.ToZonedDateTime(DateTimeZone.Utc);
        var result = handler(input);
        Assert.Null(result);
    }
}
