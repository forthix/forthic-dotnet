using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Forthic.Modules.Standard;

/// <summary>
/// String manipulation operations
/// </summary>
public class StringModule : Module
{
    public StringModule() : base("string", "")
    {
        RegisterWords();
    }

    private void RegisterWords()
    {
        // Construction
        AddModuleWord(">STR", ToStr);
        AddModuleWord("CONCAT", Concat);

        // Manipulation
        AddModuleWord("SPLIT", Split);
        AddModuleWord("JOIN", Join);
        AddModuleWord("/N", SlashN);
        AddModuleWord("/R", SlashR);
        AddModuleWord("/T", SlashT);

        // Transform
        AddModuleWord("LOWERCASE", Lowercase);
        AddModuleWord("UPPERCASE", Uppercase);
        AddModuleWord("ASCII", Ascii);
        AddModuleWord("STRIP", Strip);
        AddModuleWord("REPLACE", Replace);

        // Regex
        AddModuleWord("RE-MATCH", ReMatch);
        AddModuleWord("RE-MATCH-ALL", ReMatchAll);
        AddModuleWord("RE-MATCH-GROUP", ReMatchGroup);

        // Encoding
        AddModuleWord("URL-ENCODE", UrlEncode);
        AddModuleWord("URL-DECODE", UrlDecode);
    }

    private Task ToStr(Interpreter interp)
    {
        var val = interp.StackPop();
        interp.StackPush(Helpers.ToString(val));
        return Task.CompletedTask;
    }

    private Task Concat(Interpreter interp)
    {
        var items = interp.StackPop();

        if (items is List<object?> list)
        {
            var sb = new StringBuilder();
            foreach (var item in list)
            {
                sb.Append(Helpers.ToString(item));
            }
            interp.StackPush(sb.ToString());
            return Task.CompletedTask;
        }

        // Two string concatenation
        var b = Helpers.ToString(items);
        var a = Helpers.ToString(interp.StackPop());
        interp.StackPush(a + b);
        return Task.CompletedTask;
    }

    private Task Split(Interpreter interp)
    {
        var delimiter = interp.StackPop();
        var str = interp.StackPop();

        var s = Helpers.ToString(str);
        var delim = Helpers.ToString(delimiter);

        if (string.IsNullOrEmpty(s))
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var parts = s.Split(new[] { delim }, StringSplitOptions.None);
        interp.StackPush(parts.Cast<object?>().ToList());
        return Task.CompletedTask;
    }

    private Task Join(Interpreter interp)
    {
        var delimiter = interp.StackPop();
        var items = interp.StackPop();

        if (items is not List<object?> list)
        {
            interp.StackPush("");
            return Task.CompletedTask;
        }

        var delim = Helpers.ToString(delimiter);
        var strings = list.Select(Helpers.ToString);
        interp.StackPush(string.Join(delim, strings));
        return Task.CompletedTask;
    }

    private Task SlashN(Interpreter interp)
    {
        interp.StackPush("\n");
        return Task.CompletedTask;
    }

    private Task SlashR(Interpreter interp)
    {
        interp.StackPush("\r");
        return Task.CompletedTask;
    }

    private Task SlashT(Interpreter interp)
    {
        interp.StackPush("\t");
        return Task.CompletedTask;
    }

    private Task Lowercase(Interpreter interp)
    {
        var val = interp.StackPop();
        interp.StackPush(Helpers.ToString(val).ToLower());
        return Task.CompletedTask;
    }

    private Task Uppercase(Interpreter interp)
    {
        var val = interp.StackPop();
        interp.StackPush(Helpers.ToString(val).ToUpper());
        return Task.CompletedTask;
    }

    private Task Ascii(Interpreter interp)
    {
        var val = interp.StackPop();
        var str = Helpers.ToString(val);

        if (string.IsNullOrEmpty(str))
        {
            interp.StackPush(0L);
            return Task.CompletedTask;
        }

        interp.StackPush((long)str[0]);
        return Task.CompletedTask;
    }

    private Task Strip(Interpreter interp)
    {
        var val = interp.StackPop();
        interp.StackPush(Helpers.ToString(val).Trim());
        return Task.CompletedTask;
    }

    private Task Replace(Interpreter interp)
    {
        var replaceStr = interp.StackPop();
        var pattern = interp.StackPop();
        var str = interp.StackPop();

        var s = Helpers.ToString(str);
        var p = Helpers.ToString(pattern);
        var r = Helpers.ToString(replaceStr);

        try
        {
            var result = Regex.Replace(s, p, r);
            interp.StackPush(result);
        }
        catch
        {
            // If regex is invalid, return original string
            interp.StackPush(s);
        }

        return Task.CompletedTask;
    }

    private Task ReMatch(Interpreter interp)
    {
        var pattern = interp.StackPop();
        var str = interp.StackPop();

        var s = Helpers.ToString(str);
        var p = Helpers.ToString(pattern);

        try
        {
            var match = Regex.Match(s, p);
            interp.StackPush(match.Success ? match.Value : null);
        }
        catch
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task ReMatchAll(Interpreter interp)
    {
        var pattern = interp.StackPop();
        var str = interp.StackPop();

        var s = Helpers.ToString(str);
        var p = Helpers.ToString(pattern);

        try
        {
            var matches = Regex.Matches(s, p);
            var result = matches.Select(m => (object?)m.Value).ToList();
            interp.StackPush(result);
        }
        catch
        {
            interp.StackPush(new List<object?>());
        }

        return Task.CompletedTask;
    }

    private Task ReMatchGroup(Interpreter interp)
    {
        var groupIndex = interp.StackPop();
        var pattern = interp.StackPop();
        var str = interp.StackPop();

        var s = Helpers.ToString(str);
        var p = Helpers.ToString(pattern);
        var idx = Helpers.ToInt(groupIndex);

        try
        {
            var match = Regex.Match(s, p);
            if (match.Success && idx >= 0 && idx < match.Groups.Count)
            {
                interp.StackPush(match.Groups[idx].Value);
            }
            else
            {
                interp.StackPush(null);
            }
        }
        catch
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task UrlEncode(Interpreter interp)
    {
        var val = interp.StackPop();
        var str = Helpers.ToString(val);
        interp.StackPush(HttpUtility.UrlEncode(str));
        return Task.CompletedTask;
    }

    private Task UrlDecode(Interpreter interp)
    {
        var val = interp.StackPop();
        var str = Helpers.ToString(val);
        interp.StackPush(HttpUtility.UrlDecode(str));
        return Task.CompletedTask;
    }
}
