using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Forthic;

/// <summary>
/// WordErrorHandler is a function that handles errors during word execution
/// Returns Task if error was handled (completes successfully), or throws if it should propagate
/// </summary>
public delegate Task WordErrorHandler(Exception error, IWord word, Interpreter interp);

/// <summary>
/// RuntimeInfo - Metadata about where and how a word can execute
///
/// Used by the ExecutionPlanner to batch remote word execution efficiently.
/// Standard library words can execute in any runtime, while runtime-specific
/// words (like RemoteWord) can only execute in their designated runtime.
/// </summary>
public class RuntimeInfo
{
    public string Runtime { get; set; } = "local";  // "local" | "python" | "ruby" | "rust" | etc.
    public bool IsRemote { get; set; }              // True if this word requires remote execution
    public bool IsStandard { get; set; }            // True if this is a standard library word
    public string[] AvailableIn { get; set; } = new[] { "dotnet" }; // List of runtimes where available
}

/// <summary>
/// IWord - Interface for all executable words in Forthic
///
/// A word is the fundamental unit of execution in Forthic. When interpreted,
/// it performs an action (typically manipulating the stack or control flow).
/// </summary>
public interface IWord
{
    Task Execute(Interpreter interp);
    string GetName();
    string GetString();
    CodeLocation? GetLocation();
    void SetLocation(CodeLocation? location);
    void AddErrorHandler(WordErrorHandler handler);
    void RemoveErrorHandler(WordErrorHandler handler);
    void ClearErrorHandlers();
    List<WordErrorHandler> GetErrorHandlers();
    RuntimeInfo GetRuntimeInfo();
}

/// <summary>
/// BaseWord provides default implementation of IWord interface
/// </summary>
public abstract class BaseWord : IWord
{
    protected string name;
    protected string str;
    protected CodeLocation? location;
    protected List<WordErrorHandler> errorHandlers;

    protected BaseWord(string name)
    {
        this.name = name;
        this.str = name;
        this.location = null;
        this.errorHandlers = new List<WordErrorHandler>();
    }

    public virtual async Task Execute(Interpreter interp)
    {
        throw new ForthicException("Must override Word.Execute");
    }

    public string GetName() => name;
    public string GetString() => str;
    public CodeLocation? GetLocation() => location;
    public void SetLocation(CodeLocation? loc) => location = loc;

    public void AddErrorHandler(WordErrorHandler handler)
    {
        errorHandlers.Add(handler);
    }

    public void RemoveErrorHandler(WordErrorHandler handler)
    {
        errorHandlers.Remove(handler);
    }

    public void ClearErrorHandlers()
    {
        errorHandlers.Clear();
    }

    public List<WordErrorHandler> GetErrorHandlers()
    {
        return new List<WordErrorHandler>(errorHandlers);
    }

    /// <summary>
    /// TryErrorHandlers tries error handlers in order
    /// Returns true if error was handled, false otherwise
    /// </summary>
    protected async Task<bool> TryErrorHandlers(Exception err, Interpreter interp)
    {
        foreach (var handler in errorHandlers)
        {
            try
            {
                await handler(err, this, interp);
                return true; // Handler succeeded
            }
            catch
            {
                // Handler failed, try next one
            }
        }
        return false; // No handler succeeded
    }

    public virtual RuntimeInfo GetRuntimeInfo()
    {
        return new RuntimeInfo
        {
            Runtime = "local",
            IsRemote = false,
            IsStandard = false,
            AvailableIn = new[] { "dotnet" }
        };
    }
}

// ============================================================================
// Concrete Word Types
// ============================================================================

/// <summary>
/// PushValueWord - Word that pushes a value onto the stack
/// </summary>
public class PushValueWord : BaseWord
{
    private readonly object? value;

    public PushValueWord(string name, object? value) : base(name)
    {
        this.value = value;
    }

    public override async Task Execute(Interpreter interp)
    {
        interp.StackPush(value);
        await Task.CompletedTask;
    }
}

/// <summary>
/// ModuleWord - Word that wraps a function with error handler support
/// </summary>
public class ModuleWord : BaseWord
{
    private readonly Func<Interpreter, Task> handler;

    public ModuleWord(string name, Func<Interpreter, Task> handler) : base(name)
    {
        this.handler = handler;
    }

    public override async Task Execute(Interpreter interp)
    {
        try
        {
            await handler(interp);
        }
        catch (Exception e)
        {
            // Try error handlers
            if (await TryErrorHandlers(e, interp))
            {
                return; // Error was handled
            }
            throw; // Re-throw if not handled
        }
    }
}

/// <summary>
/// DefinitionWord - Word defined by a sequence of other words
/// </summary>
public class DefinitionWord : BaseWord
{
    public List<IWord> Words { get; }

    public DefinitionWord(string name) : base(name)
    {
        Words = new List<IWord>();
    }

    public void AddWord(IWord word)
    {
        Words.Add(word);
    }

    public override async Task Execute(Interpreter interp)
    {
        foreach (var word in Words)
        {
            try
            {
                await word.Execute(interp);
            }
            catch (Exception e)
            {
                // Try error handlers
                if (await TryErrorHandlers(e, interp))
                {
                    continue; // Error was handled, continue with next word
                }
                throw; // Re-throw if not handled
            }
        }
    }
}
