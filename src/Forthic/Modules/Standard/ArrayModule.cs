using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forthic.Modules.Standard;

/// <summary>
/// Array manipulation operations
/// </summary>
public class ArrayModule : Module
{
    public ArrayModule() : base("array", "")
    {
        RegisterWords();
    }

    private void RegisterWords()
    {
        // Construction
        AddModuleWord("APPEND", Append);
        AddModuleWord("REVERSE", Reverse);
        AddModuleWord("UNIQUE", Unique);

        // Access
        AddModuleWord("LENGTH", Length);
        AddModuleWord("NTH", Nth);
        AddModuleWord("LAST", Last);
        AddModuleWord("SLICE", Slice);
        AddModuleWord("TAKE", Take);
        AddModuleWord("DROP", Drop);

        // Set operations
        AddModuleWord("DIFFERENCE", Difference);
        AddModuleWord("INTERSECTION", Intersection);
        AddModuleWord("UNION", Union);

        // Sort
        AddModuleWord("SORT", Sort);
        AddModuleWord("SHUFFLE", Shuffle);
        AddModuleWord("ROTATE", Rotate);

        // Combine
        AddModuleWord("ZIP", Zip);
        AddModuleWord("ZIP-WITH", ZipWith);
        AddModuleWord("FLATTEN", Flatten);

        // Transform
        AddModuleWord("MAP", Map);
        AddModuleWord("SELECT", Select);
        AddModuleWord("REDUCE", Reduce);

        // Group
        AddModuleWord("INDEX", Index);
        AddModuleWord("BY-FIELD", ByField);
        AddModuleWord("GROUP-BY-FIELD", GroupByField);
        AddModuleWord("GROUP-BY", GroupBy);
        AddModuleWord("GROUPS-OF", GroupsOf);

        // Utility
        AddModuleWord("FOREACH", ForEach);
        AddModuleWord("<REPEAT", Repeat);
        AddModuleWord("UNPACK", Unpack);
        AddModuleWord("KEY-OF", KeyOf);
    }

    private Task Append(Interpreter interp)
    {
        var item = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            list = new List<object?>();
        }

        var result = new List<object?>(list) { item };
        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Reverse(Interpreter interp)
    {
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(arr);
            return Task.CompletedTask;
        }

        var result = new List<object?>(list);
        result.Reverse();
        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Unique(Interpreter interp)
    {
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var result = new List<object?>();
        var seen = new HashSet<string>();

        foreach (var item in list)
        {
            var key = Helpers.ToString(item);
            if (seen.Add(key))
            {
                result.Add(item);
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Length(Interpreter interp)
    {
        var arr = interp.StackPop();

        if (arr is List<object?> list)
        {
            interp.StackPush((long)list.Count);
        }
        else
        {
            interp.StackPush(0L);
        }

        return Task.CompletedTask;
    }

    private Task Nth(Interpreter interp)
    {
        var index = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        var idx = Helpers.ToInt(index);
        if (idx < 0 || idx >= list.Count)
        {
            interp.StackPush(null);
        }
        else
        {
            interp.StackPush(list[idx]);
        }

        return Task.CompletedTask;
    }

    private Task Last(Interpreter interp)
    {
        var arr = interp.StackPop();

        if (arr is List<object?> list && list.Count > 0)
        {
            interp.StackPush(list[^1]);
        }
        else
        {
            interp.StackPush(null);
        }

        return Task.CompletedTask;
    }

    private Task Slice(Interpreter interp)
    {
        var end = interp.StackPop();
        var start = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var startIdx = Helpers.ToInt(start);
        var endIdx = Helpers.ToInt(end);

        if (startIdx < 0) startIdx += list.Count;
        if (endIdx < 0) endIdx += list.Count;

        startIdx = Math.Max(0, Math.Min(startIdx, list.Count));
        endIdx = Math.Max(0, Math.Min(endIdx, list.Count));

        if (startIdx > endIdx)
        {
            var result = new List<object?>();
            for (int i = startIdx - 1; i >= endIdx; i--)
            {
                result.Add(list[i]);
            }
            interp.StackPush(result);
        }
        else
        {
            interp.StackPush(list.GetRange(startIdx, endIdx - startIdx));
        }

        return Task.CompletedTask;
    }

    private Task Take(Interpreter interp)
    {
        var n = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var count = Helpers.ToInt(n);
        count = Math.Min(count, list.Count);

        interp.StackPush(list.Take(count).ToList());
        return Task.CompletedTask;
    }

    private Task Drop(Interpreter interp)
    {
        var n = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var count = Helpers.ToInt(n);
        count = Math.Min(count, list.Count);

        interp.StackPush(list.Skip(count).ToList());
        return Task.CompletedTask;
    }

    private Task Difference(Interpreter interp)
    {
        var arr2 = interp.StackPop();
        var arr1 = interp.StackPop();

        if (arr1 is not List<object?> list1 || arr2 is not List<object?> list2)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var set2 = new HashSet<string>(list2.Select(Helpers.ToString));
        var result = list1.Where(item => !set2.Contains(Helpers.ToString(item))).ToList();

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Intersection(Interpreter interp)
    {
        var arr2 = interp.StackPop();
        var arr1 = interp.StackPop();

        if (arr1 is not List<object?> list1 || arr2 is not List<object?> list2)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var set2 = new HashSet<string>(list2.Select(Helpers.ToString));
        var result = list1.Where(item => set2.Contains(Helpers.ToString(item))).ToList();

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Union(Interpreter interp)
    {
        var arr2 = interp.StackPop();
        var arr1 = interp.StackPop();

        if (arr1 is not List<object?> list1)
        {
            list1 = new List<object?>();
        }
        if (arr2 is not List<object?> list2)
        {
            list2 = new List<object?>();
        }

        var result = new List<object?>(list1);
        var seen = new HashSet<string>(list1.Select(Helpers.ToString));

        foreach (var item in list2)
        {
            if (seen.Add(Helpers.ToString(item)))
            {
                result.Add(item);
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Sort(Interpreter interp)
    {
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(arr);
            return Task.CompletedTask;
        }

        var result = new List<object?>(list);
        result.Sort((a, b) => CompareValues(a, b));

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Shuffle(Interpreter interp)
    {
        var arr = interp.StackPop();

        if (arr is not List<object?> list || list.Count == 0)
        {
            interp.StackPush(arr);
            return Task.CompletedTask;
        }

        var result = new List<object?>(list);
        var rng = Random.Shared;

        // Fisher-Yates shuffle
        for (int i = result.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Rotate(Interpreter interp)
    {
        var n = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list || list.Count == 0)
        {
            interp.StackPush(arr);
            return Task.CompletedTask;
        }

        var count = Helpers.ToInt(n);
        count = count % list.Count;
        if (count < 0) count += list.Count;

        var result = new List<object?>(list.Skip(count));
        result.AddRange(list.Take(count));

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Zip(Interpreter interp)
    {
        var arr2 = interp.StackPop();
        var arr1 = interp.StackPop();

        if (arr1 is not List<object?> list1 || arr2 is not List<object?> list2)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var minLen = Math.Min(list1.Count, list2.Count);
        var result = new List<object?>();

        for (int i = 0; i < minLen; i++)
        {
            result.Add(new List<object?> { list1[i], list2[i] });
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private async Task ZipWith(Interpreter interp)
    {
        var forthicCode = interp.StackPop();
        var arr2 = interp.StackPop();
        var arr1 = interp.StackPop();

        if (arr1 is not List<object?> list1 || arr2 is not List<object?> list2 || forthicCode is not string code)
        {
            interp.StackPush(new List<object?>());
            return;
        }

        var minLen = Math.Min(list1.Count, list2.Count);
        var result = new List<object?>();

        for (int i = 0; i < minLen; i++)
        {
            interp.StackPush(list1[i]);
            interp.StackPush(list2[i]);
            await interp.Run(code);
            result.Add(interp.StackPop());
        }

        interp.StackPush(result);
    }

    private Task Flatten(Interpreter interp)
    {
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(arr);
            return Task.CompletedTask;
        }

        var result = FlattenArray(list, -1);
        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private async Task Map(Interpreter interp)
    {
        var forthicCode = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list || forthicCode is not string code)
        {
            interp.StackPush(new List<object?>());
            return;
        }

        var result = new List<object?>();
        foreach (var item in list)
        {
            interp.StackPush(item);
            await interp.Run(code);
            result.Add(interp.StackPop());
        }

        interp.StackPush(result);
    }

    private async Task Select(Interpreter interp)
    {
        var forthicCode = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list || forthicCode is not string code)
        {
            interp.StackPush(new List<object?>());
            return;
        }

        var result = new List<object?>();
        foreach (var item in list)
        {
            interp.StackPush(item);
            await interp.Run(code);
            var keep = interp.StackPop();
            if (Helpers.IsTruthy(keep))
            {
                result.Add(item);
            }
        }

        interp.StackPush(result);
    }

    private async Task Reduce(Interpreter interp)
    {
        var forthicCode = interp.StackPop();
        var initial = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list || forthicCode is not string code)
        {
            interp.StackPush(initial);
            return;
        }

        var accumulator = initial;
        foreach (var item in list)
        {
            interp.StackPush(accumulator);
            interp.StackPush(item);
            await interp.Run(code);
            accumulator = interp.StackPop();
        }

        interp.StackPush(accumulator);
    }

    private async Task Index(Interpreter interp)
    {
        var forthicCode = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list || forthicCode is not string code)
        {
            interp.StackPush(new Dictionary<string, object?>());
            return;
        }

        var result = new Dictionary<string, object?>();
        foreach (var item in list)
        {
            interp.StackPush(item);
            await interp.Run(code);
            var key = Helpers.ToString(interp.StackPop());
            result[key] = item;
        }

        interp.StackPush(result);
    }

    private Task ByField(Interpreter interp)
    {
        var field = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(new Dictionary<string, object?>());
            return Task.CompletedTask;
        }

        var fieldName = Helpers.ToString(field);
        var result = new Dictionary<string, object?>();

        foreach (var item in list)
        {
            if (item is Dictionary<string, object?> rec && rec.TryGetValue(fieldName, out var val))
            {
                var key = Helpers.ToString(val);
                result[key] = item;
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task GroupByField(Interpreter interp)
    {
        var field = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(new Dictionary<string, object?>());
            return Task.CompletedTask;
        }

        var fieldName = Helpers.ToString(field);
        var result = new Dictionary<string, object?>();

        foreach (var item in list)
        {
            if (item is Dictionary<string, object?> rec && rec.TryGetValue(fieldName, out var val))
            {
                var key = Helpers.ToString(val);
                if (!result.ContainsKey(key))
                {
                    result[key] = new List<object?>();
                }
                ((List<object?>)result[key]!).Add(item);
            }
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private async Task GroupBy(Interpreter interp)
    {
        var forthicCode = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list || forthicCode is not string code)
        {
            interp.StackPush(new Dictionary<string, object?>());
            return;
        }

        var result = new Dictionary<string, object?>();
        foreach (var item in list)
        {
            interp.StackPush(item);
            await interp.Run(code);
            var key = Helpers.ToString(interp.StackPop());

            if (!result.ContainsKey(key))
            {
                result[key] = new List<object?>();
            }
            ((List<object?>)result[key]!).Add(item);
        }

        interp.StackPush(result);
    }

    private Task GroupsOf(Interpreter interp)
    {
        var n = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var size = Helpers.ToInt(n);
        if (size <= 0)
        {
            interp.StackPush(new List<object?>());
            return Task.CompletedTask;
        }

        var result = new List<object?>();
        for (int i = 0; i < list.Count; i += size)
        {
            var group = list.Skip(i).Take(size).ToList();
            result.Add(group);
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private async Task ForEach(Interpreter interp)
    {
        var forthicCode = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list || forthicCode is not string code)
        {
            return;
        }

        foreach (var item in list)
        {
            interp.StackPush(item);
            await interp.Run(code);
            interp.StackPop(); // Discard result
        }
    }

    private Task Repeat(Interpreter interp)
    {
        var n = interp.StackPop();
        var item = interp.StackPop();

        var count = Helpers.ToInt(n);
        var result = new List<object?>();

        for (int i = 0; i < count; i++)
        {
            result.Add(item);
        }

        interp.StackPush(result);
        return Task.CompletedTask;
    }

    private Task Unpack(Interpreter interp)
    {
        var container = interp.StackPop();

        if (container == null)
        {
            return Task.CompletedTask;
        }

        if (container is List<object?> list)
        {
            foreach (var item in list)
            {
                interp.StackPush(item);
            }
        }
        else if (container is Dictionary<string, object?> dict)
        {
            var keys = dict.Keys.OrderBy(k => k).ToList();
            foreach (var key in keys)
            {
                interp.StackPush(dict[key]);
            }
        }

        return Task.CompletedTask;
    }

    private Task KeyOf(Interpreter interp)
    {
        var item = interp.StackPop();
        var arr = interp.StackPop();

        if (arr is not List<object?> list)
        {
            interp.StackPush(null);
            return Task.CompletedTask;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (Helpers.AreEqual(list[i], item))
            {
                interp.StackPush((long)i);
                return Task.CompletedTask;
            }
        }

        interp.StackPush(null);
        return Task.CompletedTask;
    }

    private static int CompareValues(object? a, object? b)
    {
        var aNum = Helpers.ToNumber(a);
        var bNum = Helpers.ToNumber(b);

        if (aNum.HasValue && bNum.HasValue)
        {
            if (aNum.Value < bNum.Value) return -1;
            if (aNum.Value > bNum.Value) return 1;
            return 0;
        }

        if (a is string aStr && b is string bStr)
        {
            return string.Compare(aStr, bStr, StringComparison.Ordinal);
        }

        return 0;
    }

    private static List<object?> FlattenArray(List<object?> arr, int depth)
    {
        if (depth == 0)
        {
            return arr;
        }

        var result = new List<object?>();
        foreach (var item in arr)
        {
            if (item is List<object?> subArr)
            {
                var nextDepth = depth > 0 ? depth - 1 : depth;
                var flattened = FlattenArray(subArr, nextDepth);
                result.AddRange(flattened);
            }
            else
            {
                result.Add(item);
            }
        }

        return result;
    }
}
