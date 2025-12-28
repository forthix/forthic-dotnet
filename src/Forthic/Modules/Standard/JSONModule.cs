using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Forthic.Modules.Standard;

/// <summary>
/// JSON encoding and decoding operations
/// </summary>
public class JSONModule : Module
{
    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true
    };

    public JSONModule() : base("json", "")
    {
        RegisterWords();
    }

    private void RegisterWords()
    {
        AddModuleWord(">JSON", ToJson);
        AddModuleWord("JSON>", FromJson);
        AddModuleWord("JSON-PRETTIFY", JsonPrettify);
    }

    private Task ToJson(Interpreter interp)
    {
        var val = interp.StackPop();

        try
        {
            var json = JsonSerializer.Serialize(val);
            interp.StackPush(json);
        }
        catch
        {
            interp.StackPush("null");
        }

        return Task.CompletedTask;
    }

    private Task FromJson(Interpreter interp)
    {
        var val = interp.StackPop();

        if (val == null)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        var jsonStr = Helpers.ToString(val);

        try
        {
            var result = JsonSerializer.Deserialize<object>(jsonStr);
            interp.StackPush(ConvertJsonElement(result));
        }
        catch
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task JsonPrettify(Interpreter interp)
    {
        var val = interp.StackPop();

        try
        {
            var json = JsonSerializer.Serialize(val, PrettyOptions);
            interp.StackPush(json);
        }
        catch
        {
            interp.StackPush("null");
        }

        return Task.CompletedTask;
    }

    private static object? ConvertJsonElement(object? obj)
    {
        if (obj is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => ConvertJsonObject(element),
                JsonValueKind.Array => ConvertJsonArray(element),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => null
            };
        }
        return obj;
    }

    private static Dictionary<string, object?> ConvertJsonObject(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = ConvertJsonElement(property.Value);
        }
        return dict;
    }

    private static List<object?> ConvertJsonArray(JsonElement element)
    {
        var list = new List<object?>();
        foreach (var item in element.EnumerateArray())
        {
            list.Add(ConvertJsonElement(item));
        }
        return list;
    }
}
