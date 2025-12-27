using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Forthic;

/// <summary>
/// Module - Container for words, variables, and imported modules
///
/// Modules provide namespacing and code organization in Forthic.
/// Each module maintains its own dictionary of words, variables, and imported modules.
///
/// Features:
/// - Word and variable management
/// - Module importing with optional prefixes
/// - Exportable word lists for controlled visibility
/// - Module duplication for isolated execution contexts
/// </summary>
public class Module
{
    public string Name { get; }
    public string ForthicCode { get; }

    protected List<IWord> words;
    protected List<string> exportable;
    protected Dictionary<string, Variable> variables;
    protected Dictionary<string, Module> modules;
    protected Dictionary<string, HashSet<string>> modulePrefixes; // module_name -> set of prefixes
    protected Interpreter? interp;

    public Module(string name, string forthicCode = "")
    {
        Name = name;
        ForthicCode = forthicCode;
        words = new List<IWord>();
        exportable = new List<string>();
        variables = new Dictionary<string, Variable>();
        modules = new Dictionary<string, Module>();
        modulePrefixes = new Dictionary<string, HashSet<string>>();
        interp = null;
    }

    public string GetName() => Name;

    public void SetInterp(Interpreter interpreter)
    {
        interp = interpreter;
    }

    public Interpreter GetInterp()
    {
        if (interp == null)
        {
            throw new ModuleException(Name, "Module has no interpreter");
        }
        return interp;
    }

    // ============================================================================
    // Duplication Methods
    // ============================================================================

    public Module Dup()
    {
        var result = new Module(Name, ForthicCode);
        result.words = new List<IWord>(words);
        result.exportable = new List<string>(exportable);

        foreach (var kvp in variables)
        {
            result.variables[kvp.Key] = kvp.Value.Dup();
        }

        foreach (var kvp in modules)
        {
            result.modules[kvp.Key] = kvp.Value;
        }

        return result;
    }

    public Module Copy(Interpreter interpreter)
    {
        var result = Dup();

        // Restore module_prefixes
        foreach (var kvp in modulePrefixes)
        {
            var moduleName = kvp.Key;
            var prefixes = kvp.Value;
            var module = modules[moduleName];

            foreach (var prefix in prefixes)
            {
                result.ImportModule(prefix, module, interpreter);
            }
        }

        return result;
    }

    // ============================================================================
    // Module Management
    // ============================================================================

    public Module? FindModule(string name)
    {
        return modules.TryGetValue(name, out var module) ? module : null;
    }

    public void RegisterModule(string moduleName, string prefix, Module module)
    {
        modules[moduleName] = module;

        if (!modulePrefixes.ContainsKey(moduleName))
        {
            modulePrefixes[moduleName] = new HashSet<string>();
        }
        modulePrefixes[moduleName].Add(prefix);
    }

    public void ImportModule(string prefix, Module module, Interpreter interpreter)
    {
        var newModule = module.Dup();

        var exportedWords = newModule.ExportableWords();
        foreach (var word in exportedWords)
        {
            if (prefix == "")
            {
                // Unprefixed import - add word directly
                AddWord(word);
            }
            else
            {
                // Prefixed import - create ExecuteWord
                var prefixedWord = new ExecuteWord($"{prefix}.{word.GetName()}", word);
                AddWord(prefixedWord);
            }
        }

        RegisterModule(module.Name, prefix, newModule);
    }

    // ============================================================================
    // Word Management
    // ============================================================================

    public void AddWord(IWord word)
    {
        words.Add(word);
    }

    public ModuleMemoWord AddMemoWords(IWord word)
    {
        var memoWord = new ModuleMemoWord(word);
        words.Add(memoWord);
        words.Add(new ModuleMemoBangWord(memoWord));
        words.Add(new ModuleMemoBangAtWord(memoWord));
        return memoWord;
    }

    public void AddExportable(params string[] names)
    {
        exportable.AddRange(names);
    }

    public void AddExportableWord(IWord word)
    {
        words.Add(word);
        exportable.Add(word.GetName());
    }

    public void AddModuleWord(string wordName, Func<Interpreter, Task> handler)
    {
        var word = new ModuleWord(wordName, handler);
        AddExportableWord(word);
    }

    public List<IWord> ExportableWords()
    {
        var exportableSet = new HashSet<string>(exportable);
        return words.Where(w => exportableSet.Contains(w.GetName())).ToList();
    }

    public IWord? FindWord(string name)
    {
        // Check dictionary words first
        var word = FindDictionaryWord(name);
        if (word != null) return word;

        // Check variables
        word = FindVariable(name);
        return word;
    }

    public IWord? FindDictionaryWord(string wordName)
    {
        // Search from end to beginning (last added word wins)
        for (int i = words.Count - 1; i >= 0; i--)
        {
            if (words[i].GetName() == wordName)
            {
                return words[i];
            }
        }
        return null;
    }

    public IWord? FindVariable(string varName)
    {
        if (variables.TryGetValue(varName, out var variable))
        {
            return new PushValueWord(varName, variable);
        }
        return null;
    }

    // ============================================================================
    // Variable Management
    // ============================================================================

    public void AddVariable(string name, object? value = null)
    {
        if (!variables.ContainsKey(name))
        {
            variables[name] = new Variable(name, value);
        }
    }

    public Variable? GetVariable(string name)
    {
        return variables.TryGetValue(name, out var variable) ? variable : null;
    }
}

// ============================================================================
// Additional Word Types for Module System
// ============================================================================

/// <summary>
/// ExecuteWord - Wrapper word that executes another word
/// Used for prefixed module imports (e.g., prefix.word)
/// </summary>
public class ExecuteWord : BaseWord
{
    private readonly IWord targetWord;

    public ExecuteWord(string name, IWord targetWord) : base(name)
    {
        this.targetWord = targetWord;
    }

    public override async Task Execute(Interpreter interp)
    {
        await targetWord.Execute(interp);
    }

    public override RuntimeInfo GetRuntimeInfo()
    {
        return targetWord.GetRuntimeInfo();
    }
}

/// <summary>
/// ModuleMemoWord - Memoized word that caches its result
/// </summary>
public class ModuleMemoWord : BaseWord
{
    private readonly IWord word;
    private bool hasValue;
    public object? Value { get; private set; }

    public ModuleMemoWord(IWord word) : base(word.GetName())
    {
        this.word = word;
        this.hasValue = false;
        this.Value = null;
    }

    public async Task Refresh(Interpreter interp)
    {
        await word.Execute(interp);
        Value = interp.StackPop();
        hasValue = true;
    }

    public override async Task Execute(Interpreter interp)
    {
        if (!hasValue)
        {
            await Refresh(interp);
        }
        interp.StackPush(Value);
    }
}

/// <summary>
/// ModuleMemoBangWord - Forces refresh of a memoized word
/// </summary>
public class ModuleMemoBangWord : BaseWord
{
    private readonly ModuleMemoWord memoWord;

    public ModuleMemoBangWord(ModuleMemoWord memoWord) : base(memoWord.GetName() + "!")
    {
        this.memoWord = memoWord;
    }

    public override async Task Execute(Interpreter interp)
    {
        await memoWord.Refresh(interp);
    }
}

/// <summary>
/// ModuleMemoBangAtWord - Refreshes a memoized word and returns its value
/// </summary>
public class ModuleMemoBangAtWord : BaseWord
{
    private readonly ModuleMemoWord memoWord;

    public ModuleMemoBangAtWord(ModuleMemoWord memoWord) : base(memoWord.GetName() + "!@")
    {
        this.memoWord = memoWord;
    }

    public override async Task Execute(Interpreter interp)
    {
        await memoWord.Refresh(interp);
        interp.StackPush(memoWord.Value);
    }
}
