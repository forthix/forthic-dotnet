using Xunit;
using System;
using System.Threading.Tasks;

namespace Forthic.Tests;

public class InterpreterTests
{
    [Fact]
    public void InitialState()
    {
        var interp = new Interpreter();
        Assert.Equal(0, interp.GetStack().Length);
        Assert.Equal("", interp.CurModule().GetName());
    }

    [Fact]
    public async Task PushString()
    {
        var interp = new Interpreter();
        await interp.Run("\"hello\"");
        Assert.Equal(1, interp.GetStack().Length);
        Assert.Equal("hello", interp.StackPop());
    }

    [Fact]
    public async Task Comment()
    {
        var interp = new Interpreter();
        await interp.Run("# This is a comment");
        Assert.Equal(0, interp.GetStack().Length);

        // Test comment with code
        var interp2 = new Interpreter();
        await interp2.Run("\"before\" # This is a comment");
        Assert.Equal(1, interp2.GetStack().Length);
    }

    [Fact]
    public async Task EmptyArray()
    {
        var interp = new Interpreter();
        await interp.Run("[]");
        Assert.Equal(1, interp.GetStack().Length);

        var result = interp.StackPop();
        var arr = result as object[];
        Assert.NotNull(arr);
        Assert.Empty(arr);
    }

    [Fact]
    public async Task ArrayWithItems()
    {
        var interp = new Interpreter();
        await interp.Run("[1 2 3]");
        Assert.Equal(1, interp.GetStack().Length);

        var result = interp.StackPop();
        var arr = result as object[];
        Assert.NotNull(arr);
        Assert.Equal(3, arr.Length);
        Assert.Equal(1L, arr[0]);
        Assert.Equal(2L, arr[1]);
        Assert.Equal(3L, arr[2]);
    }

    [Fact]
    public async Task StartModule()
    {
        var interp = new Interpreter();
        await interp.Run("{");
        // Module stack should have 2 modules (app + pushed app)
        // Note: moduleStack is private, so we can't directly test it
        // We just verify it doesn't error
    }

    [Fact]
    public async Task ModuleNested()
    {
        var interp = new Interpreter();
        await interp.Run("{mymodule");
        Assert.Equal("mymodule", interp.CurModule().GetName());
    }

    [Fact]
    public async Task ModuleClosure()
    {
        var interp = new Interpreter();
        await interp.Run("{mymodule }");
        // Back to app module
        Assert.Equal("", interp.CurModule().GetName());
    }

    [Fact]
    public async Task Definition()
    {
        var interp = new Interpreter();
        await interp.Run(": PUSH_42 42 ;");

        // Word should be defined
        var word = interp.CurModule().FindDictionaryWord("PUSH_42");
        Assert.NotNull(word);
    }

    [Fact]
    public async Task DefinitionExecution()
    {
        var interp = new Interpreter();
        await interp.Run(": PUSH_42 42 ; PUSH_42");

        // Should have 42 on stack
        Assert.Equal(1, interp.GetStack().Length);
        Assert.Equal(42L, interp.StackPop());
    }

    [Fact]
    public async Task Memo()
    {
        var interp = new Interpreter();
        await interp.Run("@: CONSTANT 42 ;");

        // Should have created the memo word and its variants
        var memoWord = interp.CurModule().FindDictionaryWord("CONSTANT");
        Assert.NotNull(memoWord);

        var refreshWord = interp.CurModule().FindDictionaryWord("CONSTANT!");
        Assert.NotNull(refreshWord);

        var refreshAtWord = interp.CurModule().FindDictionaryWord("CONSTANT!@");
        Assert.NotNull(refreshAtWord);
    }

    [Fact]
    public async Task Literals()
    {
        var interp = new Interpreter();

        // Test boolean
        await interp.Run("TRUE FALSE");
        Assert.Equal(false, interp.StackPop());
        Assert.Equal(true, interp.StackPop());

        // Test integer
        await interp.Run("42");
        Assert.Equal(42L, interp.StackPop());

        // Test float
        await interp.Run("3.14");
        Assert.Equal(3.14, interp.StackPop());
    }

    [Fact]
    public async Task UnknownWord()
    {
        var interp = new Interpreter();
        var ex = await Assert.ThrowsAsync<UnknownWordException>(() => interp.Run("UNKNOWN_WORD"));
        Assert.Contains("Unknown word", ex.Message);
    }

    [Fact]
    public void StackUnderflow()
    {
        var interp = new Interpreter();
        Assert.Throws<StackUnderflowException>(() => interp.StackPop());
    }

    [Fact]
    public async Task MissingSemicolon()
    {
        var interp = new Interpreter();
        var ex = await Assert.ThrowsAsync<MissingSemicolonException>(() => interp.Run(": WORD"));
        Assert.Contains("Missing semicolon", ex.Message);
    }

    [Fact]
    public async Task ExtraSemicolon()
    {
        var interp = new Interpreter();
        var ex = await Assert.ThrowsAsync<ExtraSemicolonException>(() => interp.Run(";"));
        Assert.Contains("Extra semicolon", ex.Message);
    }
}
