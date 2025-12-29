using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Forthic;

/// <summary>
/// WordOptions - Type-safe options container for module words
///
/// Overview:
/// WordOptions provides a structured way for Forthic words to accept optional
/// configuration parameters without requiring fixed parameter positions. This
/// enables flexible, extensible APIs similar to keyword arguments.
///
/// Usage in Forthic:
///   [.option_name value ...] ~> WORD
///
/// Example in Forthic code:
///   [1 2 3] '2 *' [.with_key TRUE] ~> MAP
///   [10 20 30] [.comparator "-1 *"] ~> SORT
///   [[[1 2]]] [.depth 1] ~> FLATTEN
///
/// Internal Representation:
/// Created from flat array: [.key1 val1 .key2 val2]
/// Stored as Dictionary internally for efficient lookup
///
/// Note: Dot-symbols in Forthic have the leading '.' already stripped,
/// so keys come in as "key1", "key2", etc.
/// </summary>
public class WordOptions
{
    private readonly Dictionary<string, object?> options;

    /// <summary>
    /// Creates a new WordOptions from a flat array of key-value pairs
    /// flatArray must be object[] with even length: [key1, val1, key2, val2, ...]
    /// Keys must be strings (dot-symbols with . already stripped)
    /// </summary>
    public WordOptions(object[] flatArray)
    {
        if (flatArray == null)
        {
            throw new ArgumentException("Options must be an array");
        }

        if (flatArray.Length % 2 != 0)
        {
            throw new ArgumentException(
                $"Options must be key-value pairs (even length). Got {flatArray.Length} elements");
        }

        options = new Dictionary<string, object?>();

        for (int i = 0; i < flatArray.Length; i += 2)
        {
            var key = flatArray[i];
            var value = flatArray[i + 1];

            // Key must be a string
            if (key is not string keyStr)
            {
                throw new ArgumentException(
                    $"Option key must be a string (dot-symbol). Got: {key?.GetType().Name ?? "null"}");
            }

            options[keyStr] = value;
        }
    }

    /// <summary>
    /// Get option value with optional default
    /// </summary>
    public object? Get(string key, object? defaultValue = null)
    {
        return options.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Check if option exists
    /// </summary>
    public bool Has(string key)
    {
        return options.ContainsKey(key);
    }

    /// <summary>
    /// Get all options as plain dictionary
    /// </summary>
    public Dictionary<string, object?> ToRecord()
    {
        return new Dictionary<string, object?>(options);
    }

    /// <summary>
    /// Get all option keys
    /// </summary>
    public string[] Keys()
    {
        return options.Keys.ToArray();
    }

    /// <summary>
    /// For debugging/display
    /// </summary>
    public override string ToString()
    {
        if (options.Count == 0)
        {
            return "<WordOptions: >";
        }

        var pairs = options.Select(kvp =>
        {
            var valStr = JsonSerializer.Serialize(kvp.Value);
            return $".{kvp.Key} {valStr}";
        });

        return $"<WordOptions: {string.Join(" ", pairs)}>";
    }
}
