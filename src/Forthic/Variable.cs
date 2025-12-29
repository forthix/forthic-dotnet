namespace Forthic;

/// <summary>
/// Variable - Named mutable value container
///
/// Represents a variable that can store and retrieve values within a module scope.
/// Variables are accessed by name and can be set to any value type.
/// </summary>
public class Variable
{
    public string Name { get; }
    public object? Value { get; set; }

    public Variable(string name, object? value = null)
    {
        Name = name;
        Value = value;
    }

    public string GetName() => Name;
    public void SetValue(object? val) => Value = val;
    public object? GetValue() => Value;

    public Variable Dup()
    {
        return new Variable(Name, Value);
    }
}
