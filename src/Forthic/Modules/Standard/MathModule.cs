using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forthic.Modules.Standard;

/// <summary>
/// Mathematical operations and utilities
/// </summary>
public class MathModule : Module
{
    public MathModule() : base("math", "")
    {
        RegisterWords();
    }

    private void RegisterWords()
    {
        // Arithmetic Operations
        AddModuleWord("+", Plus);
        AddModuleWord("ADD", Plus);
        AddModuleWord("-", Minus);
        AddModuleWord("SUBTRACT", Minus);
        AddModuleWord("*", Times);
        AddModuleWord("MULTIPLY", Times);
        AddModuleWord("/", Divide);
        AddModuleWord("DIVIDE", Divide);
        AddModuleWord("MOD", Mod);

        // Aggregates
        AddModuleWord("SUM", Sum);
        AddModuleWord("MEAN", Mean);
        AddModuleWord("MAX", Max);
        AddModuleWord("MIN", Min);

        // Type conversion
        AddModuleWord(">INT", ToInt);
        AddModuleWord(">FLOAT", ToFloat);
        AddModuleWord("ROUND", Round);
        AddModuleWord(">FIXED", ToFixed);

        // Math functions
        AddModuleWord("ABS", Abs);
        AddModuleWord("SQRT", Sqrt);
        AddModuleWord("FLOOR", Floor);
        AddModuleWord("CEIL", Ceil);
        AddModuleWord("CLAMP", Clamp);

        // Special values
        AddModuleWord("INFINITY", Infinity);
        AddModuleWord("UNIFORM-RANDOM", UniformRandom);
    }

    // ========================================
    // Arithmetic Operations
    // ========================================

    private Task Plus(Interpreter interp)
    {
        var b = interp.StackPop();

        // Case 1: Array on top of stack
        if (b is List<object?> arr)
        {
            double result = 0;
            foreach (var val in arr)
            {
                if (val != null)
                {
                    var num = Helpers.ToNumber(val);
                    if (num.HasValue)
                        result += num.Value;
                }
            }
            interp.StackPush(result);
            return Task.CompletedTask;
        }

        // Case 2: Two numbers
        var a = interp.StackPop();
        var numA = Helpers.ToNumber(a);
        var numB = Helpers.ToNumber(b);

        if (!numA.HasValue || !numB.HasValue)
        {
            interp.StackPush(0.0);
            return Task.CompletedTask;
        }

        interp.StackPush(numA.Value + numB.Value);
        return Task.CompletedTask;
    }

    private Task Minus(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();

        var numA = Helpers.ToNumber(a);
        var numB = Helpers.ToNumber(b);

        if (!numA.HasValue || !numB.HasValue)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        interp.StackPush(numA.Value - numB.Value);
        return Task.CompletedTask;
    }

    private Task Times(Interpreter interp)
    {
        var b = interp.StackPop();

        // Case 1: Array on top of stack
        if (b is List<object?> arr)
        {
            double result = 1;
            foreach (var val in arr)
            {
                if (val == null)
                {
                    interp.StackPush(null);
                    return Task.CompletedTask;
                }
                var num = Helpers.ToNumber(val);
                if (!num.HasValue)
                {
                    interp.StackPush(null);
                    return Task.CompletedTask;
                }
                result *= num.Value;
            }
            interp.StackPush(result);
            return Task.CompletedTask;
        }

        // Case 2: Two numbers
        var a = interp.StackPop();
        var numA = Helpers.ToNumber(a);
        var numB = Helpers.ToNumber(b);

        if (!numA.HasValue || !numB.HasValue)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        interp.StackPush(numA.Value * numB.Value);
        return Task.CompletedTask;
    }

    private Task Divide(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();

        var numA = Helpers.ToNumber(a);
        var numB = Helpers.ToNumber(b);

        if (!numA.HasValue || !numB.HasValue || numB.Value == 0)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        interp.StackPush(numA.Value / numB.Value);
        return Task.CompletedTask;
    }

    private Task Mod(Interpreter interp)
    {
        var n = interp.StackPop();
        var m = interp.StackPop();

        var numM = Helpers.ToNumber(m);
        var numN = Helpers.ToNumber(n);

        if (!numM.HasValue || !numN.HasValue)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        interp.StackPush(numM.Value % numN.Value);
        return Task.CompletedTask;
    }

    // ========================================
    // Aggregates
    // ========================================

    private Task Sum(Interpreter interp)
    {
        var items = interp.StackPop();

        if (items is not List<object?> arr)
        {
            interp.StackPush(0.0);
            return Task.CompletedTask;
        }

        double result = 0;
        foreach (var val in arr)
        {
            if (val != null)
            {
                var num = Helpers.ToNumber(val);
                if (num.HasValue)
                    result += num.Value;
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Mean(Interpreter interp)
    {
        var items = interp.StackPop();

        if (items is not List<object?> arr || arr.Count == 0)
        {
            interp.StackPush(0.0);
            return Task.CompletedTask;
        }

        double sum = 0;
        int count = 0;
        foreach (var val in arr)
        {
            if (val != null)
            {
                var num = Helpers.ToNumber(val);
                if (num.HasValue)
                {
                    sum += num.Value;
                    count++;
                }
            }
        }

        interp.StackPush(count > 0 ? sum / count : 0.0);
        return Task.CompletedTask;
    }

    private Task Max(Interpreter interp)
    {
        var items = interp.StackPop();

        if (items is not List<object?> arr || arr.Count == 0)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        double? max = null;
        foreach (var val in arr)
        {
            var num = Helpers.ToNumber(val);
            if (num.HasValue)
            {
                if (!max.HasValue || num.Value > max.Value)
                    max = num.Value;
            }
        }

        interp.StackPush(max);
        return Task.CompletedTask;
    }

    private Task Min(Interpreter interp)
    {
        var items = interp.StackPop();

        if (items is not List<object?> arr || arr.Count == 0)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        double? min = null;
        foreach (var val in arr)
        {
            var num = Helpers.ToNumber(val);
            if (num.HasValue)
            {
                if (!min.HasValue || num.Value < min.Value)
                    min = num.Value;
            }
        }

        interp.StackPush(min);
        return Task.CompletedTask;
    }

    // ========================================
    // Type Conversion
    // ========================================

    private Task ToInt(Interpreter interp)
    {
        var val = interp.StackPop();
        var num = Helpers.ToNumber(val);
        interp.StackPush(num.HasValue ? (long)num.Value : 0L);
        return Task.CompletedTask;
    }

    private Task ToFloat(Interpreter interp)
    {
        var val = interp.StackPop();
        var num = Helpers.ToNumber(val);
        interp.StackPush(num ?? 0.0);
        return Task.CompletedTask;
    }

    private Task Round(Interpreter interp)
    {
        var val = interp.StackPop();
        var num = Helpers.ToNumber(val);
        interp.StackPush(num.HasValue ? Math.Round(num.Value) : 0.0);
        return Task.CompletedTask;
    }

    private Task ToFixed(Interpreter interp)
    {
        var decimals = interp.StackPop();
        var val = interp.StackPop();

        var num = Helpers.ToNumber(val);
        var dec = Helpers.ToInt(decimals);

        if (!num.HasValue)
        {
            interp.StackPush("");
            return Task.CompletedTask;
        }

        interp.StackPush(num.Value.ToString($"F{dec}"));
        return Task.CompletedTask;
    }

    // ========================================
    // Math Functions
    // ========================================

    private Task Abs(Interpreter interp)
    {
        var val = interp.StackPop();
        var num = Helpers.ToNumber(val);
        interp.StackPush(num.HasValue ? Math.Abs(num.Value) : 0.0);
        return Task.CompletedTask;
    }

    private Task Sqrt(Interpreter interp)
    {
        var val = interp.StackPop();
        var num = Helpers.ToNumber(val);
        interp.StackPush(num.HasValue ? Math.Sqrt(num.Value) : 0.0);
        return Task.CompletedTask;
    }

    private Task Floor(Interpreter interp)
    {
        var val = interp.StackPop();
        var num = Helpers.ToNumber(val);
        interp.StackPush(num.HasValue ? Math.Floor(num.Value) : 0.0);
        return Task.CompletedTask;
    }

    private Task Ceil(Interpreter interp)
    {
        var val = interp.StackPop();
        var num = Helpers.ToNumber(val);
        interp.StackPush(num.HasValue ? Math.Ceiling(num.Value) : 0.0);
        return Task.CompletedTask;
    }

    private Task Clamp(Interpreter interp)
    {
        var max = interp.StackPop();
        var min = interp.StackPop();
        var val = interp.StackPop();

        var numVal = Helpers.ToNumber(val);
        var numMin = Helpers.ToNumber(min);
        var numMax = Helpers.ToNumber(max);

        if (!numVal.HasValue || !numMin.HasValue || !numMax.HasValue)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        var result = Math.Max(numMin.Value, Math.Min(numMax.Value, numVal.Value));
        interp.StackPush(result);
        return Task.CompletedTask;
    }

    // ========================================
    // Special Values
    // ========================================

    private Task Infinity(Interpreter interp)
    {
        interp.StackPush(double.PositiveInfinity);
        return Task.CompletedTask;
    }

    private Task UniformRandom(Interpreter interp)
    {
        var max = interp.StackPop();
        var min = interp.StackPop();

        var numMin = Helpers.ToNumber(min) ?? 0.0;
        var numMax = Helpers.ToNumber(max) ?? 1.0;

        var random = Random.Shared.NextDouble();
        var result = numMin + (random * (numMax - numMin));

        interp.StackPush(result);
        return Task.CompletedTask;
    }
}
