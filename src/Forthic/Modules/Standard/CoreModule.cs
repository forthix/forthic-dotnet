using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Forthic.Modules.Standard;

/// <summary>
/// Core module - Essential interpreter operations
///
/// Provides fundamental operations for:
/// - Stack manipulation (POP, DUP, SWAP)
/// - Variables (VARIABLES, !, @, !@)
/// - Module system (EXPORT, USE-MODULES)
/// - Execution (INTERPRET)
/// - Control flow (IDENTITY, NOP, NULL, ARRAY?, DEFAULT, *DEFAULT)
/// - Options (~>)
/// - Profiling (PROFILE-START, PROFILE-END, PROFILE-TIMESTAMP, PROFILE-DATA)
/// - Logging (START-LOG, END-LOG)
/// - String operations (INTERPOLATE, PRINT)
/// - Debug (PEEK!, STACK!)
/// </summary>
public class CoreModule : Module
{
    public CoreModule() : base("core", "")
    {
        RegisterWords();
    }

    private void RegisterWords()
    {
        // Stack operations
        AddModuleWord("POP", Pop);
        AddModuleWord("DUP", Dup);
        AddModuleWord("SWAP", Swap);

        // Variable operations
        AddModuleWord("VARIABLES", Variables);
        AddModuleWord("!", Set);
        AddModuleWord("@", Get);
        AddModuleWord("!@", SetGet);

        // Module operations
        AddModuleWord("EXPORT", Export);
        AddModuleWord("USE-MODULES", UseModules);

        // Execution
        AddModuleWord("INTERPRET", Interpret);

        // Control flow
        AddModuleWord("IDENTITY", Identity);
        AddModuleWord("NOP", Nop);
        AddModuleWord("NULL", Null);
        AddModuleWord("ARRAY?", ArrayCheck);
        AddModuleWord("DEFAULT", Default);
        AddModuleWord("*DEFAULT", DefaultStar);

        // Options
        AddModuleWord("~>", ToOptions);

        // Profiling (placeholder implementations)
        AddModuleWord("PROFILE-START", ProfileStart);
        AddModuleWord("PROFILE-END", ProfileEnd);
        AddModuleWord("PROFILE-TIMESTAMP", ProfileTimestamp);
        AddModuleWord("PROFILE-DATA", ProfileData);

        // Logging (placeholder implementations)
        AddModuleWord("START-LOG", StartLog);
        AddModuleWord("END-LOG", EndLog);

        // String operations
        AddModuleWord("INTERPOLATE", Interpolate);
        AddModuleWord("PRINT", Print);

        // Debug
        AddModuleWord("PEEK!", Peek);
        AddModuleWord("STACK!", StackDebug);
    }

    // ============================================================================
    // Helper Methods
    // ============================================================================

    private static Variable GetOrCreateVariable(Interpreter interp, string name)
    {
        // Validate variable name - no __ prefix allowed
        if (name.StartsWith("__"))
        {
            throw new InvalidVariableNameException(name);
        }

        var curModule = interp.CurModule();

        // Check if variable already exists
        var variable = curModule.GetVariable(name);

        // Create it if it doesn't exist
        if (variable == null)
        {
            curModule.AddVariable(name, null);
            variable = curModule.GetVariable(name);
        }

        return variable!;
    }

    // ============================================================================
    // Stack Operations
    // ============================================================================

    private Task Pop(Interpreter interp)
    {
        interp.StackPop();
        return Task.CompletedTask;
    }

    private Task Dup(Interpreter interp)
    {
        var a = interp.StackPop();
        interp.StackPush(a);
        interp.StackPush(a);
        return Task.CompletedTask;
    }

    private Task Swap(Interpreter interp)
    {
        var b = interp.StackPop();
        var a = interp.StackPop();
        interp.StackPush(b);
        interp.StackPush(a);
        return Task.CompletedTask;
    }

    // ============================================================================
    // Variable Operations
    // ============================================================================

    private Task Variables(Interpreter interp)
    {
        var varnames = interp.StackPop();
        var curModule = interp.CurModule();

        if (varnames is object[] arr)
        {
            foreach (var v in arr)
            {
                if (v is string varName)
                {
                    // Validate variable name
                    if (varName.StartsWith("__"))
                    {
                        throw new InvalidVariableNameException(varName);
                    }
                    curModule.AddVariable(varName, null);
                }
            }
        }

        return Task.CompletedTask;
    }

    private Task Set(Interpreter interp)
    {
        var variable = interp.StackPop();
        var value = interp.StackPop();

        Variable varObj;

        if (variable is string varName)
        {
            // Auto-create variable if string name
            varObj = GetOrCreateVariable(interp, varName);
        }
        else
        {
            // Use existing variable object
            varObj = (Variable)variable!;
        }

        varObj.SetValue(value);
        return Task.CompletedTask;
    }

    private Task Get(Interpreter interp)
    {
        var variable = interp.StackPop();

        Variable varObj;

        if (variable is string varName)
        {
            // Auto-create variable if string name
            varObj = GetOrCreateVariable(interp, varName);
        }
        else
        {
            // Use existing variable object
            varObj = (Variable)variable!;
        }

        interp.StackPush(varObj.GetValue());
        return Task.CompletedTask;
    }

    private Task SetGet(Interpreter interp)
    {
        var variable = interp.StackPop();
        var value = interp.StackPop();

        Variable varObj;

        if (variable is string varName)
        {
            // Auto-create variable if string name
            varObj = GetOrCreateVariable(interp, varName);
        }
        else
        {
            // Use existing variable object
            varObj = (Variable)variable!;
        }

        varObj.SetValue(value);
        interp.StackPush(varObj.GetValue());
        return Task.CompletedTask;
    }

    // ============================================================================
    // Module Operations
    // ============================================================================

    private Task Export(Interpreter interp)
    {
        var names = interp.StackPop();
        if (names is object[] arr)
        {
            var strNames = arr.OfType<string>().ToArray();
            interp.CurModule().AddExportable(strNames);
        }
        return Task.CompletedTask;
    }

    private Task UseModules(Interpreter interp)
    {
        var names = interp.StackPop();
        if (names == null)
        {
            return Task.CompletedTask;
        }
        if (names is object[] arr)
        {
            interp.UseModules(arr);
        }
        return Task.CompletedTask;
    }

    // ============================================================================
    // Execution
    // ============================================================================

    private async Task Interpret(Interpreter interp)
    {
        var str = interp.StackPop();
        if (str == null)
        {
            return;
        }
        if (str is string code)
        {
            await interp.Run(code);
        }
    }

    // ============================================================================
    // Control Flow
    // ============================================================================

    private Task Identity(Interpreter interp)
    {
        // No-op
        return Task.CompletedTask;
    }

    private Task Nop(Interpreter interp)
    {
        // No-op
        return Task.CompletedTask;
    }

    private Task Null(Interpreter interp)
    {
        interp.StackPush(null);
        return Task.CompletedTask;
    }

    private Task ArrayCheck(Interpreter interp)
    {
        var value = interp.StackPop();
        var isArray = value is object[];
        interp.StackPush(isArray);
        return Task.CompletedTask;
    }

    private Task Default(Interpreter interp)
    {
        var defaultValue = interp.StackPop();
        var value = interp.StackPop();

        if (value == null || (value is string s && s == ""))
        {
            interp.StackPush(defaultValue);
        }
        else
        {
            interp.StackPush(value);
        }

        return Task.CompletedTask;
    }

    private async Task DefaultStar(Interpreter interp)
    {
        var defaultForthic = interp.StackPop();
        var value = interp.StackPop();

        if (value == null || (value is string s && s == ""))
        {
            if (defaultForthic is string code)
            {
                await interp.Run(code);
                var result = interp.StackPop();
                interp.StackPush(result);
                return;
            }
        }

        interp.StackPush(value);
    }

    // ============================================================================
    // Options
    // ============================================================================

    private Task ToOptions(Interpreter interp)
    {
        var array = interp.StackPop();
        if (array is object[] arr)
        {
            var opts = new WordOptions(arr);
            interp.StackPush(opts);
        }
        else
        {
            throw new ForthicException("~> requires an array");
        }
        return Task.CompletedTask;
    }

    // ============================================================================
    // Profiling (Placeholder implementations)
    // ============================================================================

    private Task ProfileStart(Interpreter interp)
    {
        // TODO: Implement profiling in interpreter
        return Task.CompletedTask;
    }

    private Task ProfileEnd(Interpreter interp)
    {
        // TODO: Implement profiling in interpreter
        return Task.CompletedTask;
    }

    private Task ProfileTimestamp(Interpreter interp)
    {
        var label = interp.StackPop();
        // TODO: Implement profiling
        return Task.CompletedTask;
    }

    private Task ProfileData(Interpreter interp)
    {
        // TODO: Implement profiling
        var result = new Dictionary<string, object?>
        {
            ["word_counts"] = new object[] { },
            ["timestamps"] = new object[] { }
        };
        interp.StackPush(result);
        return Task.CompletedTask;
    }

    // ============================================================================
    // Logging (Placeholder implementations)
    // ============================================================================

    private Task StartLog(Interpreter interp)
    {
        // TODO: Implement logging in interpreter
        return Task.CompletedTask;
    }

    private Task EndLog(Interpreter interp)
    {
        // TODO: Implement logging in interpreter
        return Task.CompletedTask;
    }

    // ============================================================================
    // String Operations
    // ============================================================================

    private Task Interpolate(Interpreter interp)
    {
        // Pop options if present
        var topVal = interp.StackPop();
        string str;
        WordOptions opts;

        // Check if we have options
        if (topVal is WordOptions optsVal)
        {
            opts = optsVal;
            str = (string)(interp.StackPop() ?? "");
        }
        else
        {
            str = (string)(topVal ?? "");
            opts = new WordOptions(Array.Empty<object>());
        }

        var separator = (string)(opts.Get("separator", ", ") ?? ", ");
        var nullText = (string)(opts.Get("null_text", "null") ?? "null");
        var useJson = opts.Get("json", false) is bool jsonVal && jsonVal;

        var result = InterpolateString(interp, str, separator, nullText, useJson);
        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Print(Interpreter interp)
    {
        // Pop options if present
        var topVal = interp.StackPop();
        object? value;
        WordOptions opts;

        // Check if we have options
        if (topVal is WordOptions optsVal)
        {
            opts = optsVal;
            value = interp.StackPop();
        }
        else
        {
            value = topVal;
            opts = new WordOptions(Array.Empty<object>());
        }

        var separator = (string)(opts.Get("separator", ", ") ?? ", ");
        var nullText = (string)(opts.Get("null_text", "null") ?? "null");
        var useJson = opts.Get("json", false) is bool jsonVal && jsonVal;

        string result;
        if (value is string str)
        {
            // String: interpolate variables
            result = InterpolateString(interp, str, separator, nullText, useJson);
        }
        else
        {
            // Non-string: format directly
            result = ValueToString(value, separator, nullText, useJson);
        }

        Console.WriteLine(result);
        return Task.CompletedTask;
    }

    private static string InterpolateString(Interpreter interp, string str, string separator, string nullText, bool useJson)
    {
        if (string.IsNullOrEmpty(str))
        {
            return "";
        }

        // Handle escape sequences by replacing \. with a temporary placeholder
        var escaped = str.Replace("\\.", "\x00ESCAPED_DOT\x00");

        // Replace whitespace-preceded or start-of-string .variable patterns
        var re = new Regex(@"(^|\s)\.([a-zA-Z_][a-zA-Z0-9_-]*)");
        var interpolated = re.Replace(escaped, match =>
        {
            // Extract variable name (skip leading space if present and the dot)
            var trimmed = match.Value.Trim();
            var varName = trimmed.Substring(1); // Remove leading .

            Variable? variable;
            try
            {
                variable = GetOrCreateVariable(interp, varName);
            }
            catch
            {
                return match.Value; // Return original if error
            }

            var value = variable.GetValue();

            // Preserve leading whitespace if it was there
            if (match.Value.StartsWith(" ") || match.Value.StartsWith("\t"))
            {
                return match.Value[0] + ValueToString(value, separator, nullText, useJson);
            }
            return ValueToString(value, separator, nullText, useJson);
        });

        // Restore escaped dots
        return interpolated.Replace("\x00ESCAPED_DOT\x00", ".");
    }

    private static string ValueToString(object? value, string separator, string nullText, bool useJson)
    {
        if (value == null)
        {
            return nullText;
        }

        if (useJson)
        {
            return System.Text.Json.JsonSerializer.Serialize(value);
        }

        if (value is object[] arr)
        {
            var strs = arr.Select(v => ValueToString(v, separator, nullText, false));
            return string.Join(separator, strs);
        }

        if (value is Dictionary<string, object?> dict)
        {
            return System.Text.Json.JsonSerializer.Serialize(dict);
        }

        return Helpers.ToString(value);
    }

    // ============================================================================
    // Debug Operations
    // ============================================================================

    private Task Peek(Interpreter interp)
    {
        var stack = interp.GetStack();
        var items = stack.Items();
        if (items.Count > 0)
        {
            Console.WriteLine(items[^1]);
        }
        else
        {
            Console.WriteLine("<STACK EMPTY>");
        }
        throw new IntentionalStopException("PEEK!");
    }

    private Task StackDebug(Interpreter interp)
    {
        var stack = interp.GetStack();
        var items = stack.Items();

        // Reverse the items
        var reversed = new object?[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            reversed[i] = items[items.Count - 1 - i];
        }

        var json = System.Text.Json.JsonSerializer.Serialize(reversed, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        Console.WriteLine(json);
        throw new IntentionalStopException("STACK!");
    }
}
