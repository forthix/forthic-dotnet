using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forthic.Modules.Standard;

/// <summary>
/// Record (dictionary/map) manipulation operations
/// </summary>
public class RecordModule : Module
{
    public RecordModule() : base("record", "")
    {
        RegisterWords();
    }

    private void RegisterWords()
    {
        // Creation
        AddModuleWord("REC", CreateRecord);
        AddModuleWord("<REC!", SetRecordValue);

        // Access
        AddModuleWord("REC@", GetRecordValue);
        AddModuleWord("|REC@", PipeRecAt);
        AddModuleWord("KEYS", Keys);
        AddModuleWord("VALUES", Values);

        // Transform
        AddModuleWord("RELABEL", Relabel);
        AddModuleWord("INVERT-KEYS", InvertKeys);
        AddModuleWord("REC-DEFAULTS", RecDefaults);
        AddModuleWord("<DEL", Del);
    }

    private Task CreateRecord(Interpreter interp)
    {
        var arr = interp.StackPop();

        if (arr is not List<object?> pairs)
        {
            interp.StackPush(new Dictionary<string, object?>());
            return Task.CompletedTask;
        }

        var result = new Dictionary<string, object?>();
        foreach (var item in pairs)
        {
            if (item is List<object?> pair && pair.Count >= 2)
            {
                var key = Helpers.ToString(pair[0]);
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = pair[1];
                }
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task SetRecordValue(Interpreter interp)
    {
        var field = interp.StackPop();
        var value = interp.StackPop();
        var record = interp.StackPop();

        var rec = record as Dictionary<string, object?> ?? new Dictionary<string, object?>();
        var result = new Dictionary<string, object?>(rec);

        // Support both string and array of field names
        var fields = field is List<object?> fieldArr
            ? fieldArr.Select(Helpers.ToString).ToList()
            : new List<string> { Helpers.ToString(field) };

        if (fields.Count == 0)
        {
            interp.StackPush(result);
            return Task.CompletedTask;
        }

        // Drill down, creating nested records as needed
        var curRec = result;
        for (int i = 0; i < fields.Count - 1; i++)
        {
            var fieldName = fields[i];
            if (curRec.TryGetValue(fieldName, out var existing) && existing is Dictionary<string, object?> existingDict)
            {
                var newRec = new Dictionary<string, object?>(existingDict);
                curRec[fieldName] = newRec;
                curRec = newRec;
            }
            else
            {
                var newRec = new Dictionary<string, object?>();
                curRec[fieldName] = newRec;
                curRec = newRec;
            }
        }

        curRec[fields[^1]] = value;
        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task GetRecordValue(Interpreter interp)
    {
        var field = interp.StackPop();
        var record = interp.StackPop();

        if (record is not Dictionary<string, object?> rec)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        // Support both string and array of field names
        var fields = field is List<object?> fieldArr
            ? fieldArr.Select(Helpers.ToString).ToList()
            : new List<string> { Helpers.ToString(field) };

        var result = DrillForValue(rec, fields);
        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task PipeRecAt(Interpreter interp)
    {
        var field = interp.StackPop();
        var records = interp.StackPop();

        if (records is not List<object?> list)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var fields = field is List<object?> fieldArr
            ? fieldArr.Select(Helpers.ToString).ToList()
            : new List<string> { Helpers.ToString(field) };

        var result = new List<object?>();
        foreach (var item in list)
        {
            if (item is Dictionary<string, object?> rec)
            {
                result.Add(DrillForValue(rec, fields));
            }
            else
            {
                result.Add(null);
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Keys(Interpreter interp)
    {
        var record = interp.StackPop();

        if (record is Dictionary<string, object?> rec)
        {
            interp.StackPush(rec.Keys.Cast<object?>().ToList());
        }
        else
        {
            interp.StackPush(new List<object?>());
        }

        return Task.CompletedTask;
    }

    private Task Values(Interpreter interp)
    {
        var record = interp.StackPop();

        if (record is Dictionary<string, object?> rec)
        {
            interp.StackPush(rec.Values.ToList());
        }
        else
        {
            interp.StackPush(new List<object?>());
        }

        return Task.CompletedTask;
    }

    private Task Relabel(Interpreter interp)
    {
        var newKeys = interp.StackPop();
        var oldKeys = interp.StackPop();
        var container = interp.StackPop();

        if (container is not Dictionary<string, object?> rec ||
            oldKeys is not List<object?> oldList ||
            newKeys is not List<object?> newList ||
            oldList.Count != newList.Count)
        {
            interp.StackPush(container);
            return Task.CompletedTask;
        }

        var newToOld = new Dictionary<string, string>();
        for (int i = 0; i < oldList.Count; i++)
        {
            var oldKey = Helpers.ToString(oldList[i]);
            var newKey = Helpers.ToString(newList[i]);
            newToOld[newKey] = oldKey;
        }

        var result = new Dictionary<string, object?>();
        foreach (var kvp in newToOld)
        {
            if (rec.TryGetValue(kvp.Value, out var val))
            {
                result[kvp.Key] = val;
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task InvertKeys(Interpreter interp)
    {
        var record = interp.StackPop();

        if (record is not Dictionary<string, object?> rec)
        {
            interp.StackPush(new Dictionary<string, object?>());
            return Task.CompletedTask;
        }

        // Invert two-level nested record structure
        // Input:  {A: {X: 1, Y: 2}, B: {X: 3, Y: 4}}
        // Output: {X: {A: 1, B: 3}, Y: {A: 2, B: 4}}
        var result = new Dictionary<string, object?>();

        foreach (var firstKvp in rec)
        {
            if (firstKvp.Value is Dictionary<string, object?> subRecord)
            {
                foreach (var secondKvp in subRecord)
                {
                    if (!result.ContainsKey(secondKvp.Key))
                    {
                        result[secondKvp.Key] = new Dictionary<string, object?>();
                    }
                    ((Dictionary<string, object?>)result[secondKvp.Key]!)[firstKvp.Key] = secondKvp.Value;
                }
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task RecDefaults(Interpreter interp)
    {
        var keyVals = interp.StackPop();
        var record = interp.StackPop();

        var rec = record as Dictionary<string, object?> ?? new Dictionary<string, object?>();
        var result = new Dictionary<string, object?>(rec);

        if (keyVals is not List<object?> pairs)
        {
            interp.StackPush(result);
            return Task.CompletedTask;
        }

        foreach (var item in pairs)
        {
            if (item is List<object?> pair && pair.Count >= 2)
            {
                var key = Helpers.ToString(pair[0]);
                if (!result.TryGetValue(key, out var val) || val == null || Helpers.ToString(val) == "")
                {
                    result[key] = pair[1];
                }
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Del(Interpreter interp)
    {
        var key = interp.StackPop();
        var container = interp.StackPop();

        if (container is List<object?> list)
        {
            var idx = Helpers.ToInt(key);
            if (idx >= 0 && idx < list.Count)
            {
                var result = new List<object?>(list);
                result.RemoveAt(idx);
                interp.StackPush(result);
            }
            else
            {
                interp.StackPush(list);
            }
        }
        else if (container is Dictionary<string, object?> dict)
        {
            var keyStr = Helpers.ToString(key);
            var result = new Dictionary<string, object?>(dict);
            result.Remove(keyStr);
            interp.StackPush(result);
        }
        else
        {
            interp.StackPush(container);
        }

        return Task.CompletedTask;
    }

    private static object? DrillForValue(Dictionary<string, object?> record, List<string> fields)
    {
        object? result = record;
        foreach (var field in fields)
        {
            if (result is Dictionary<string, object?> rec && rec.TryGetValue(field, out var val))
            {
                result = val;
            }
            else
            {
                return null;
            }
        }
        return result;
    }
}
