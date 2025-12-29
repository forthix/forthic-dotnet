using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Forthic.Modules.Standard;

/// <summary>
/// Boolean operations and comparisons
/// </summary>
public class BooleanModule : Module
{
    public BooleanModule() : base("boolean", "")
    {
        RegisterWords();
    }

    private void RegisterWords()
    {
        // Comparisons
        AddModuleWord("==", Equal);
        AddModuleWord("!=", NotEqual);
        AddModuleWord("<", LessThan);
        AddModuleWord(">", GreaterThan);
        AddModuleWord("<=", LessThanOrEqual);
        AddModuleWord(">=", GreaterThanOrEqual);

        // Logic
        AddModuleWord("AND", And);
        AddModuleWord("OR", Or);
        AddModuleWord("NOT", Not);

        // Literals
        AddModuleWord("TRUE", True);
        AddModuleWord("FALSE", False);
        AddModuleWord("NULL", Null);

        // Utility
        AddModuleWord("IN", In);
    }

    private Task Equal(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(Helpers.AreEqual(a, b));
        return Task.CompletedTask;
    }

    private Task NotEqual(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(!Helpers.AreEqual(a, b));
        return Task.CompletedTask;
    }

    private Task LessThan(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(Compare(a, b) < 0);
        return Task.CompletedTask;
    }

    private Task GreaterThan(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(Compare(a, b) > 0);
        return Task.CompletedTask;
    }

    private Task LessThanOrEqual(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(Compare(a, b) <= 0);
        return Task.CompletedTask;
    }

    private Task GreaterThanOrEqual(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(Compare(a, b) >= 0);
        return Task.CompletedTask;
    }

    private Task And(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(Helpers.IsTruthy(a) && Helpers.IsTruthy(b));
        return Task.CompletedTask;
    }

    private Task Or(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(Helpers.IsTruthy(a) || Helpers.IsTruthy(b));
        return Task.CompletedTask;
    }

    private Task Not(Interpreter interp)
    {
        var val = interp.StackPop();
        interp.StackPush(!Helpers.IsTruthy(val));
        return Task.CompletedTask;
    }

    private Task True(Interpreter interp)
    {
        interp.StackPush(true);
        return Task.CompletedTask;
    }

    private Task False(Interpreter interp)
    {
        interp.StackPush(false);
        return Task.CompletedTask;
    }

    private Task Null(Interpreter interp)
    {
        interp.StackPush(null);
        return Task.CompletedTask;
    }

    private Task In(Interpreter interp)
    {
        var container = interp.StackPop();
        var item = interp.StackPop();

        if (container is List<object?> list)
        {
            foreach (var elem in list)
            {
                if (Helpers.AreEqual(item, elem))
                {
                    interp.StackPush(true);
                    return Task.CompletedTask;
                }
            }
        }
        else if (container is Dictionary<string, object?> dict)
        {
            var key = Helpers.ToString(item);
            interp.StackPush(dict.ContainsKey(key));
            return Task.CompletedTask;
        }

        interp.StackPush(false);
        return Task.CompletedTask;
    }

    private static int Compare(object? a, object? b)
    {
        // Try numeric comparison first
        var aNum = Helpers.ToNumber(a);
        var bNum = Helpers.ToNumber(b);
        if (aNum.HasValue && bNum.HasValue)
        {
            if (aNum.Value < bNum.Value) return -1;
            if (aNum.Value > bNum.Value) return 1;
            return 0;
        }

        // Try string comparison
        if (a is string aStr && b is string bStr)
        {
            return string.Compare(aStr, bStr, StringComparison.Ordinal);
        }

        return 0;
    }
}
