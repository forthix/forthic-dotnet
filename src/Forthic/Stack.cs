using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Forthic;

/// <summary>
/// Stack - Wrapper for the interpreter's data stack
///
/// Provides LIFO stack operations for Forthic interpreter.
/// All stack values are stored as object? to allow dynamic typing.
/// </summary>
public class Stack
{
    private readonly List<object?> items;

    /// <summary>
    /// Creates a new Stack with optional initial items
    /// </summary>
    public Stack(params object?[] initialItems)
    {
        items = new List<object?>(initialItems ?? Array.Empty<object?>());
    }

    /// <summary>
    /// Push adds a value to the top of the stack
    /// </summary>
    public void Push(object? val)
    {
        items.Add(val);
    }

    /// <summary>
    /// Pop removes and returns the top value from the stack
    /// Throws StackUnderflowException if stack is empty
    /// </summary>
    public object? Pop()
    {
        if (items.Count == 0)
        {
            throw new StackUnderflowException();
        }
        var val = items[^1];
        items.RemoveAt(items.Count - 1);
        return val;
    }

    /// <summary>
    /// Peek returns the top value without removing it
    /// Throws StackUnderflowException if stack is empty
    /// </summary>
    public object? Peek()
    {
        if (items.Count == 0)
        {
            throw new StackUnderflowException();
        }
        return items[^1];
    }

    /// <summary>
    /// Length returns the number of items on the stack
    /// </summary>
    public int Length => items.Count;

    /// <summary>
    /// Clear removes all items from the stack
    /// </summary>
    public void Clear()
    {
        items.Clear();
    }

    /// <summary>
    /// Items returns a copy of the stack items
    /// </summary>
    public List<object?> Items()
    {
        return new List<object?>(items);
    }

    /// <summary>
    /// RawItems returns the internal items list (for internal use only)
    /// </summary>
    public List<object?> RawItems()
    {
        return items;
    }

    /// <summary>
    /// Indexer for array-style access (0 = bottom, Length-1 = top)
    /// </summary>
    public object? this[int index]
    {
        get
        {
            if (index < 0 || index >= items.Count)
            {
                throw new IndexOutOfRangeException(
                    $"Stack index out of bounds: {index} (length: {items.Count})");
            }
            return items[index];
        }
        set
        {
            if (index < 0 || index >= items.Count)
            {
                throw new IndexOutOfRangeException(
                    $"Stack index out of bounds: {index} (length: {items.Count})");
            }
            items[index] = value;
        }
    }

    /// <summary>
    /// String returns a formatted string representation for debugging
    /// </summary>
    public override string ToString()
    {
        return $"Stack[{items.Count} items]";
    }

    /// <summary>
    /// ToJSON returns the stack items as a JSON string
    /// </summary>
    public string ToJSON()
    {
        return JsonSerializer.Serialize(items);
    }
}
