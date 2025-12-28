using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Forthic.Modules.Standard;

/// <summary>
/// Date and time operations
/// </summary>
public class DateTimeModule : Module
{
    public DateTimeModule() : base("datetime", "")
    {
        RegisterWords();
    }

    private void RegisterWords()
    {
        // Parsing
        AddModuleWord(">DATE", ToDate);
        AddModuleWord(">DATETIME", ToDateTime);

        // Formatting
        AddModuleWord("DATE>STR", DateToStr);
        AddModuleWord("DATETIME>STR", DateTimeToStr);

        // Arithmetic
        AddModuleWord("ADD-DAYS", AddDays);
        AddModuleWord("ADD-HOURS", AddHours);
        AddModuleWord("ADD-MINUTES", AddMinutes);
        AddModuleWord("ADD-SECONDS", AddSeconds);

        // Difference
        AddModuleWord("DIFF-DAYS", DiffDays);
        AddModuleWord("DIFF-HOURS", DiffHours);
        AddModuleWord("DIFF-MINUTES", DiffMinutes);
        AddModuleWord("DIFF-SECONDS", DiffSeconds);

        // Current time
        AddModuleWord("NOW", Now);
        AddModuleWord("TODAY", Today);

        // Components
        AddModuleWord("YEAR", Year);
        AddModuleWord("MONTH", Month);
        AddModuleWord("DAY", Day);
        AddModuleWord("HOUR", Hour);
        AddModuleWord("MINUTE", Minute);
        AddModuleWord("SECOND", Second);
    }

    private Task ToDate(Interpreter interp)
    {
        var val = interp.StackPop();
        var str = Helpers.ToString(val);

        if (DateTime.TryParse(str, out var date))
        {
            interp.StackPush(date.Date);
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task ToDateTime(Interpreter interp)
    {
        var val = interp.StackPop();
        var str = Helpers.ToString(val);

        if (DateTime.TryParse(str, out var dateTime))
        {
            interp.StackPush(dateTime);
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task DateToStr(Interpreter interp)
    {
        var val = interp.StackPop();

        if (val is DateTime dt)
        {
            interp.StackPush(dt.ToString("yyyy-MM-dd"));
        }
        else
        {
            interp.StackPush("");
        }

        return Task.CompletedTask;
    }

    private Task DateTimeToStr(Interpreter interp)
    {
        var val = interp.StackPop();

        if (val is DateTime dt)
        {
            interp.StackPush(dt.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else
        {
            interp.StackPush("");
        }

        return Task.CompletedTask;
    }

    private Task AddDays(Interpreter interp)
    {
        var days = interp.StackPop();
        var val = interp.StackPop();

        if (val is DateTime dt)
        {
            var d = Helpers.ToNumber(days) ?? 0;
            interp.StackPush(dt.AddDays(d));
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task AddHours(Interpreter interp)
    {
        var hours = interp.StackPop();
        var val = interp.StackPop();

        if (val is DateTime dt)
        {
            var h = Helpers.ToNumber(hours) ?? 0;
            interp.StackPush(dt.AddHours(h));
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task AddMinutes(Interpreter interp)
    {
        var minutes = interp.StackPop();
        var val = interp.StackPop();

        if (val is DateTime dt)
        {
            var m = Helpers.ToNumber(minutes) ?? 0;
            interp.StackPush(dt.AddMinutes(m));
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task AddSeconds(Interpreter interp)
    {
        var seconds = interp.StackPop();
        var val = interp.StackPop();

        if (val is DateTime dt)
        {
            var s = Helpers.ToNumber(seconds) ?? 0;
            interp.StackPush(dt.AddSeconds(s));
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task DiffDays(Interpreter interp)
    {
        var end = interp.StackPop();
        var start = interp.StackPop();

        if (start is DateTime dt1 && end is DateTime dt2)
        {
            interp.StackPush((dt2 - dt1).TotalDays);
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task DiffHours(Interpreter interp)
    {
        var end = interp.StackPop();
        var start = interp.StackPop();

        if (start is DateTime dt1 && end is DateTime dt2)
        {
            interp.StackPush((dt2 - dt1).TotalHours);
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task DiffMinutes(Interpreter interp)
    {
        var end = interp.StackPop();
        var start = interp.StackPop();

        if (start is DateTime dt1 && end is DateTime dt2)
        {
            interp.StackPush((dt2 - dt1).TotalMinutes);
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task DiffSeconds(Interpreter interp)
    {
        var end = interp.StackPop();
        var start = interp.StackPop();

        if (start is DateTime dt1 && end is DateTime dt2)
        {
            interp.StackPush((dt2 - dt1).TotalSeconds);
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task Now(Interpreter interp)
    {
        interp.StackPush(DateTime.Now);
        return Task.CompletedTask;
    }

    private Task Today(Interpreter interp)
    {
        interp.StackPush(DateTime.Today);
        return Task.CompletedTask;
    }

    private Task Year(Interpreter interp)
    {
        var val = interp.StackPop();
        if (val is DateTime dt)
        {
            interp.StackPush((long)dt.Year);
        }
        else
        {
            interp.StackPush(null);
        }
        return Task.CompletedTask;
    }

    private Task Month(Interpreter interp)
    {
        var val = interp.StackPop();
        if (val is DateTime dt)
        {
            interp.StackPush((long)dt.Month);
        }
        else
        {
            interp.StackPush(null);
        }
        return Task.CompletedTask;
    }

    private Task Day(Interpreter interp)
    {
        var val = interp.StackPop();
        if (val is DateTime dt)
        {
            interp.StackPush((long)dt.Day);
        }
        else
        {
            interp.StackPush(null);
        }
        return Task.CompletedTask;
    }

    private Task Hour(Interpreter interp)
    {
        var val = interp.StackPop();
        if (val is DateTime dt)
        {
            interp.StackPush((long)dt.Hour);
        }
        else
        {
            interp.StackPush(null);
        }
        return Task.CompletedTask;
    }

    private Task Minute(Interpreter interp)
    {
        var val = interp.StackPop();
        if (val is DateTime dt)
        {
            interp.StackPush((long)dt.Minute);
        }
        else
        {
            interp.StackPush(null);
        }
        return Task.CompletedTask;
    }

    private Task Second(Interpreter interp)
    {
        var val = interp.StackPop();
        if (val is DateTime dt)
        {
            interp.StackPush((long)dt.Second);
        }
        else
        {
            interp.StackPush(null);
        }
        return Task.CompletedTask;
    }
}
