using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;

namespace Forthic;

/// <summary>
/// LiteralHandler tries to parse a string as a literal value
/// Returns value if successful, null otherwise
/// </summary>
public delegate object? LiteralHandler(string str);

/// <summary>
/// Interpreter - Core Forthic interpreter
///
/// Core interpreter that tokenizes and executes Forthic code.
/// Manages the data stack, module stack, and execution context.
/// </summary>
public class Interpreter
{
    private DateTimeZone timezone;
    private Stack stack;
    private Module appModule;
    private List<Module> moduleStack;
    private Dictionary<string, Module> registeredModules;
    private List<Tokenizer> tokenizerStack;
    private Token? previousToken;
    private bool isCompiling;
    private bool isMemoDefinition;
    private DefinitionWord? curDefinition;
    private List<LiteralHandler> literalHandlers;

    public Interpreter(params Module[] modules)
    {
        timezone = DateTimeZoneProviders.Tzdb["UTC"];
        stack = new Stack();
        appModule = new Module("");
        appModule.SetInterp(this);
        moduleStack = new List<Module> { appModule };
        registeredModules = new Dictionary<string, Module>();
        tokenizerStack = new List<Tokenizer>();
        previousToken = null;
        isCompiling = false;
        isMemoDefinition = false;
        curDefinition = null;
        literalHandlers = new List<LiteralHandler>();

        RegisterStandardLiterals();

        // Import provided modules (unprefixed)
        foreach (var module in modules)
        {
            ImportModule(module, "");
        }
    }

    // ============================================================================
    // Stack Operations
    // ============================================================================

    public void StackPush(object? val)
    {
        stack.Push(val);
    }

    public object? StackPop()
    {
        return stack.Pop();
    }

    public object? StackPeek()
    {
        return stack.Peek();
    }

    public Stack GetStack() => stack;

    // ============================================================================
    // Module Operations
    // ============================================================================

    public Module GetAppModule() => appModule;

    public Module CurModule() => moduleStack[^1];

    public void ModuleStackPush(Module module)
    {
        moduleStack.Add(module);
    }

    public Module ModuleStackPop()
    {
        if (moduleStack.Count <= 1)
        {
            throw new ForthicException("Cannot pop app module from module stack");
        }
        var module = moduleStack[^1];
        moduleStack.RemoveAt(moduleStack.Count - 1);
        return module;
    }

    public void RegisterModule(Module module)
    {
        registeredModules[module.Name] = module;
        module.SetInterp(this);
    }

    public Module FindModule(string name)
    {
        if (!registeredModules.TryGetValue(name, out var module))
        {
            throw new UnknownModuleException(name);
        }
        return module;
    }

    public void UseModules(params object[] names)
    {
        foreach (var name in names)
        {
            string moduleName;
            string prefix = "";

            if (name is object[] arr)
            {
                moduleName = (string)arr[0];
                if (arr.Length >= 2)
                {
                    prefix = (string)arr[1];
                }
            }
            else
            {
                moduleName = (string)name;
            }

            var module = FindModule(moduleName);
            appModule.ImportModule(prefix, module, this);
        }
    }

    public void ImportModule(Module module, string prefix = "")
    {
        RegisterModule(module);
        appModule.ImportModule(prefix, module, this);
    }

    // ============================================================================
    // Tokenizer Operations
    // ============================================================================

    public Tokenizer GetTokenizer() => tokenizerStack[^1];

    // ============================================================================
    // Literal Handlers
    // ============================================================================

    private void RegisterStandardLiterals()
    {
        // Order matters: more specific handlers first
        literalHandlers = new List<LiteralHandler>
        {
            BooleanLiterals.ToBool,
            NumericLiterals.ToFloat,
            DateTimeLiterals.ToZonedDateTime(timezone),
            DateLiterals.ToLiteralDate(timezone),
            TimeLiterals.ToTime,
            NumericLiterals.ToInt,
        };
    }

    public void RegisterLiteralHandler(LiteralHandler handler)
    {
        literalHandlers.Insert(0, handler);
    }

    private IWord? FindLiteralWord(string name)
    {
        foreach (var handler in literalHandlers)
        {
            var value = handler(name);
            if (value != null)
            {
                return new PushValueWord(name, value);
            }
        }
        return null;
    }

    // ============================================================================
    // Find Word
    // ============================================================================

    public IWord FindWord(string name)
    {
        // 1. Check module stack (from top to bottom)
        for (int j = moduleStack.Count - 1; j >= 0; j--)
        {
            var module = moduleStack[j];
            var word = module.FindWord(name);
            if (word != null) return word;
        }

        // 2. Check literal handlers
        var literalWord = FindLiteralWord(name);
        if (literalWord != null) return literalWord;

        // 3. Not found
        throw new UnknownWordException(name);
    }

    // ============================================================================
    // Main Execution
    // ============================================================================

    public async Task Run(string code)
    {
        var tokenizer = new Tokenizer(code, null, false);
        tokenizerStack.Add(tokenizer);

        await RunWithTokenizer(tokenizer);

        tokenizerStack.RemoveAt(tokenizerStack.Count - 1);
    }

    private async Task RunWithTokenizer(Tokenizer tokenizer)
    {
        while (true)
        {
            var token = tokenizer.NextToken();

            await HandleToken(token);

            if (token.Type == TokenType.EOS)
            {
                break;
            }

            previousToken = token;
        }
    }

    // ============================================================================
    // Token Handling
    // ============================================================================

    private async Task HandleToken(Token token)
    {
        switch (token.Type)
        {
            case TokenType.String:
                await HandleStringToken(token);
                break;
            case TokenType.Comment:
                await HandleCommentToken(token);
                break;
            case TokenType.StartArray:
                await HandleStartArrayToken(token);
                break;
            case TokenType.EndArray:
                await HandleEndArrayToken(token);
                break;
            case TokenType.StartModule:
                await HandleStartModuleToken(token);
                break;
            case TokenType.EndModule:
                await HandleEndModuleToken(token);
                break;
            case TokenType.StartDef:
                await HandleStartDefinitionToken(token);
                break;
            case TokenType.StartMemo:
                await HandleStartMemoToken(token);
                break;
            case TokenType.EndDef:
                await HandleEndDefinitionToken(token);
                break;
            case TokenType.DotSymbol:
                await HandleDotSymbolToken(token);
                break;
            case TokenType.Word:
                await HandleWordToken(token);
                break;
            case TokenType.EOS:
                if (isCompiling)
                {
                    var loc = previousToken?.Location;
                    throw new MissingSemicolonException();
                }
                break;
            default:
                throw new ForthicException($"Unknown token type: {token.Type}");
        }
    }

    private async Task HandleStringToken(Token token)
    {
        var word = new PushValueWord("<string>", token.String);
        await HandleWord(word, token.Location);
    }

    private async Task HandleDotSymbolToken(Token token)
    {
        var word = new PushValueWord("<dot-symbol>", token.String);
        await HandleWord(word, token.Location);
    }

    private async Task HandleCommentToken(Token token)
    {
        await Task.CompletedTask; // No-op
    }

    private async Task HandleStartArrayToken(Token token)
    {
        var word = new PushValueWord("<start_array_token>", token);
        await HandleWord(word, token.Location);
    }

    private async Task HandleEndArrayToken(Token token)
    {
        var word = new EndArrayWord();
        await HandleWord(word, token.Location);
    }

    private async Task HandleStartModuleToken(Token token)
    {
        var word = new StartModuleWord(token.String);

        // Module words are immediate (execute during compilation) and also compiled
        if (isCompiling && curDefinition != null)
        {
            curDefinition.AddWord(word);
        }

        await word.Execute(this);
    }

    private async Task HandleEndModuleToken(Token token)
    {
        var word = new EndModuleWord();

        // Module words are immediate (execute during compilation) and also compiled
        if (isCompiling && curDefinition != null)
        {
            curDefinition.AddWord(word);
        }

        await word.Execute(this);
    }

    private async Task HandleStartDefinitionToken(Token token)
    {
        if (isCompiling)
        {
            var loc = previousToken?.Location;
            throw new MissingSemicolonException();
        }
        curDefinition = new DefinitionWord(token.String);
        isCompiling = true;
        isMemoDefinition = false;
        await Task.CompletedTask;
    }

    private async Task HandleStartMemoToken(Token token)
    {
        if (isCompiling)
        {
            var loc = previousToken?.Location;
            throw new MissingSemicolonException();
        }
        curDefinition = new DefinitionWord(token.String);
        isCompiling = true;
        isMemoDefinition = true;
        await Task.CompletedTask;
    }

    private async Task HandleEndDefinitionToken(Token token)
    {
        if (!isCompiling || curDefinition == null)
        {
            throw new ExtraSemicolonException();
        }

        if (isMemoDefinition)
        {
            CurModule().AddMemoWords(curDefinition);
        }
        else
        {
            CurModule().AddWord(curDefinition);
        }

        isCompiling = false;
        await Task.CompletedTask;
    }

    private async Task HandleWordToken(Token token)
    {
        var word = FindWord(token.String);
        await HandleWord(word, token.Location);
    }

    private async Task HandleWord(IWord word, CodeLocation? location)
    {
        if (isCompiling && curDefinition != null)
        {
            word.SetLocation(location);
            curDefinition.AddWord(word);
        }
        else
        {
            await word.Execute(this);
        }
    }
}

// ============================================================================
// Special Word Types
// ============================================================================

/// <summary>
/// StartModuleWord handles module creation and switching
/// </summary>
public class StartModuleWord : BaseWord
{
    public StartModuleWord(string name) : base(name) { }

    public override async Task Execute(Interpreter interp)
    {
        // Empty name refers to app module
        if (name == "")
        {
            interp.ModuleStackPush(interp.GetAppModule());
            await Task.CompletedTask;
            return;
        }

        // Check if module exists in current module
        var module = interp.CurModule().FindModule(name);
        if (module == null)
        {
            // Create new module
            module = new Module(name);
            interp.CurModule().RegisterModule(name, name, module);

            // If we're at app module, also register with interpreter
            if (interp.CurModule().Name == "")
            {
                interp.RegisterModule(module);
            }
        }

        interp.ModuleStackPush(module);
        await Task.CompletedTask;
    }
}

/// <summary>
/// EndModuleWord pops the current module
/// </summary>
public class EndModuleWord : BaseWord
{
    public EndModuleWord() : base("}") { }

    public override async Task Execute(Interpreter interp)
    {
        interp.ModuleStackPop();
        await Task.CompletedTask;
    }
}

/// <summary>
/// EndArrayWord collects items into an array
/// </summary>
public class EndArrayWord : BaseWord
{
    public EndArrayWord() : base("]") { }

    public override async Task Execute(Interpreter interp)
    {
        var items = new List<object?>();
        while (true)
        {
            var item = interp.StackPop();

            // Check if it's a START_ARRAY token
            if (item is Token token && token.Type == TokenType.StartArray)
            {
                break;
            }

            items.Add(item);
        }

        // Reverse the items
        items.Reverse();
        interp.StackPush(items.ToArray());
        await Task.CompletedTask;
    }
}
