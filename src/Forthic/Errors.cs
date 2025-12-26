using System;
using System.Text;

namespace Forthic;

// ============================================================================
// Code Location
// ============================================================================

public class CodeLocation
{
    public string? Source { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public int StartPos { get; set; }
    public int EndPos { get; set; }

    public CodeLocation(string? source = null, int line = 1, int column = 1, int startPos = 0, int endPos = 0)
    {
        Source = source;
        Line = line;
        Column = column;
        StartPos = startPos;
        EndPos = endPos;
    }

    public override string ToString()
    {
        if (string.IsNullOrEmpty(Source))
        {
            return $"line {Line}, col {Column}";
        }
        return $"{Source}:{Line}:{Column}";
    }
}

// ============================================================================
// Base Forthic Exception
// ============================================================================

public class ForthicException : Exception
{
    public string ForthicCode { get; set; } = string.Empty;
    public CodeLocation? Location { get; set; }

    public ForthicException(string message) : base(message)
    {
    }

    public ForthicException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ForthicException WithLocation(CodeLocation location)
    {
        Location = location;
        return this;
    }

    public ForthicException WithForthic(string forthic)
    {
        ForthicCode = forthic;
        return this;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(Message);

        if (Location != null)
        {
            sb.AppendLine($"  at {Location}");
        }

        if (!string.IsNullOrEmpty(ForthicCode))
        {
            sb.AppendLine($"  in: {ForthicCode}");
        }

        if (InnerException != null)
        {
            sb.AppendLine($"  caused by: {InnerException.Message}");
        }

        return sb.ToString();
    }
}

// ============================================================================
// Specific Exception Types
// ============================================================================

public class UnknownWordException : ForthicException
{
    public string Word { get; }

    public UnknownWordException(string word)
        : base($"Unknown word: {word}")
    {
        Word = word;
    }
}

public class UnknownModuleException : ForthicException
{
    public string Module { get; }

    public UnknownModuleException(string module)
        : base($"Unknown module: {module}")
    {
        Module = module;
    }
}

public class StackUnderflowException : ForthicException
{
    public StackUnderflowException()
        : base("Stack underflow")
    {
    }
}

public class WordExecutionException : ForthicException
{
    public string Word { get; }

    public WordExecutionException(string word, Exception innerException)
        : base($"Error executing word: {word}", innerException)
    {
        Word = word;
    }
}

public class MissingSemicolonException : ForthicException
{
    public MissingSemicolonException()
        : base("Missing semicolon (;) to end definition")
    {
    }
}

public class ExtraSemicolonException : ForthicException
{
    public ExtraSemicolonException()
        : base("Extra semicolon (;) outside of definition")
    {
    }
}

public class ModuleException : ForthicException
{
    public string Module { get; }

    public ModuleException(string module, string message)
        : base($"Module error in {module}: {message}")
    {
        Module = module;
    }
}

public class IntentionalStopException : ForthicException
{
    public IntentionalStopException(string message)
        : base(message)
    {
    }
}

public class InvalidVariableNameException : ForthicException
{
    public string VariableName { get; }

    public InvalidVariableNameException(string variableName)
        : base($"Invalid variable name: {variableName}")
    {
        VariableName = variableName;
    }
}
