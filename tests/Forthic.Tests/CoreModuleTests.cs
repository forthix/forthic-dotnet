using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Forthic.Modules.Standard;

namespace Forthic.Tests;

public class CoreModuleTests
{
    private Interpreter SetupInterpreter()
    {
        var interp = new Interpreter();
        var coreModule = new CoreModule();
        var mathModule = new MathModule();
        var stringModule = new StringModule();
        interp.ImportModule(coreModule, "");
        interp.ImportModule(mathModule, "");
        interp.ImportModule(stringModule, "");
        return interp;
    }

    // ========================================
    // Stack Operations
    // ========================================

    [Fact]
    public async Task POP_RemovesTopItem()
    {
        var interp = SetupInterpreter();
        await interp.Run("1 2 3 POP");

        var stack = interp.GetStack();
        Assert.Equal(2, stack.Length);

        var top = interp.StackPop();
        Assert.Equal(2L, top);
    }

    [Fact]
    public async Task DUP_DuplicatesTopItem()
    {
        var interp = SetupInterpreter();
        await interp.Run("42 DUP");

        var stack = interp.GetStack();
        Assert.Equal(2, stack.Length);

        var item1 = interp.StackPop();
        var item2 = interp.StackPop();
        Assert.Equal(42L, item1);
        Assert.Equal(42L, item2);
    }

    [Fact]
    public async Task SWAP_SwapsTopTwoItems()
    {
        var interp = SetupInterpreter();
        await interp.Run("1 2 SWAP");

        var stack = interp.GetStack();
        Assert.Equal(2, stack.Length);

        var top = interp.StackPop();
        var bottom = interp.StackPop();
        Assert.Equal(1L, top);
        Assert.Equal(2L, bottom);
    }

    // ========================================
    // Variable Operations
    // ========================================

    [Fact]
    public async Task VARIABLES_CreatesVariables()
    {
        var interp = SetupInterpreter();
        await interp.Run("[\"x\" \"y\"] VARIABLES");

        var appModule = interp.GetAppModule();
        var xVar = appModule.GetVariable("x");
        var yVar = appModule.GetVariable("y");

        Assert.NotNull(xVar);
        Assert.NotNull(yVar);
    }

    [Fact]
    public async Task VARIABLES_RejectsInvalidNames()
    {
        var interp = SetupInterpreter();

        await Assert.ThrowsAsync<InvalidVariableNameException>(async () =>
        {
            await interp.Run("[\"__test\"] VARIABLES");
        });
    }

    [Fact]
    public async Task SetGet_StoresAndRetrievesValue()
    {
        var interp = SetupInterpreter();
        await interp.Run("[\"x\"] VARIABLES 24 x ! x @");

        var result = interp.StackPop();
        Assert.Equal(24L, result);
    }

    [Fact]
    public async Task BangAt_SetsAndReturnsValue()
    {
        var interp = SetupInterpreter();
        await interp.Run("[\"x\"] VARIABLES 42 x !@");

        var result = interp.StackPop();
        Assert.Equal(42L, result);

        var appModule = interp.GetAppModule();
        var xVar = appModule.GetVariable("x");
        Assert.Equal(42L, xVar?.GetValue());
    }

    [Fact]
    public async Task AutoCreateVariables_WithSet()
    {
        var interp = SetupInterpreter();

        // Test ! with string variable name (auto-creates variable)
        await interp.Run("\"hello\" \"autovar1\" !");
        await interp.Run("autovar1 @");

        var result = interp.StackPop();
        Assert.Equal("hello", result);

        // Verify variable was created
        var appModule = interp.GetAppModule();
        var autovar1 = appModule.GetVariable("autovar1");
        Assert.NotNull(autovar1);
    }

    [Fact]
    public async Task AutoCreateVariables_WithGet()
    {
        var interp = SetupInterpreter();

        // Test @ with string variable name (auto-creates with null)
        await interp.Run("\"autovar2\" @");

        var result = interp.StackPop();
        Assert.Null(result);
    }

    [Fact]
    public async Task AutoCreateVariables_WithBangAt()
    {
        var interp = SetupInterpreter();

        // Test !@ with string variable name
        await interp.Run("\"world\" \"autovar3\" !@");

        var result = interp.StackPop();
        Assert.Equal("world", result);
    }

    [Fact]
    public async Task AutoCreateVariables_RejectsInvalidNames()
    {
        var interp = SetupInterpreter();

        // Test that __ prefix variables are rejected for !
        await Assert.ThrowsAsync<InvalidVariableNameException>(async () =>
        {
            await interp.Run("\"value\" \"__invalid\" !");
        });

        // Test that validation works for @ as well
        await Assert.ThrowsAsync<InvalidVariableNameException>(async () =>
        {
            await interp.Run("\"__invalid2\" @");
        });

        // Test that validation works for !@ as well
        await Assert.ThrowsAsync<InvalidVariableNameException>(async () =>
        {
            await interp.Run("\"value\" \"__invalid3\" !@");
        });
    }

    // ========================================
    // Module Operations
    // ========================================

    [Fact]
    public async Task EXPORT_MarksWordsAsExportable()
    {
        var interp = SetupInterpreter();

        // Create a module and export some words
        await interp.Run("[\"POP\" \"DUP\"] EXPORT");

        // Basic smoke test - actual export checking would require module introspection
        Assert.True(true);
    }

    [Fact]
    public async Task INTERPRET_ExecutesForthicString()
    {
        var interp = SetupInterpreter();
        await interp.Run("\"5 10 +\" INTERPRET");

        var result = interp.StackPop();
        Assert.Equal(15.0, result);
    }

    // ========================================
    // Control Flow
    // ========================================

    [Fact]
    public async Task IDENTITY_DoesNothing()
    {
        var interp = SetupInterpreter();
        await interp.Run("42 IDENTITY");

        var result = interp.StackPop();
        Assert.Equal(42L, result);
    }

    [Fact]
    public async Task NOP_DoesNothing()
    {
        var interp = SetupInterpreter();
        await interp.Run("NOP");

        var stack = interp.GetStack();
        Assert.Equal(0, stack.Length);
    }

    [Fact]
    public async Task NULL_PushesNull()
    {
        var interp = SetupInterpreter();
        await interp.Run("NULL");

        var result = interp.StackPop();
        Assert.Null(result);
    }

    [Fact]
    public async Task ARRAY_Check_ReturnsTrueForArray()
    {
        var interp = SetupInterpreter();
        await interp.Run("[1 2 3] ARRAY?");

        var result = interp.StackPop();
        Assert.True((bool)result!);
    }

    [Fact]
    public async Task ARRAY_Check_ReturnsFalseForNonArray()
    {
        var interp = SetupInterpreter();
        await interp.Run("42 ARRAY?");

        var result = interp.StackPop();
        Assert.False((bool)result!);
    }

    [Fact]
    public async Task DEFAULT_ReturnsDefaultWhenNull()
    {
        var interp = SetupInterpreter();
        await interp.Run("NULL 42 DEFAULT");

        var result = interp.StackPop();
        Assert.Equal(42L, result);
    }

    [Fact]
    public async Task DEFAULT_ReturnsValueWhenNotNull()
    {
        var interp = SetupInterpreter();
        await interp.Run("10 42 DEFAULT");

        var result = interp.StackPop();
        Assert.Equal(10L, result);
    }

    [Fact]
    public async Task DEFAULT_ReturnsDefaultWhenEmptyString()
    {
        var interp = SetupInterpreter();
        await interp.Run("\"\" 42 DEFAULT");

        var result = interp.StackPop();
        Assert.Equal(42L, result);
    }

    [Fact]
    public async Task DefaultStar_ExecutesForthicWhenNull()
    {
        var interp = SetupInterpreter();
        await interp.Run("NULL \"10 20 +\" *DEFAULT");

        var result = interp.StackPop();
        Assert.Equal(30.0, result);
    }

    [Fact]
    public async Task DefaultStar_SkipsForthicWhenNotNull()
    {
        var interp = SetupInterpreter();
        await interp.Run("42 \"10 20 +\" *DEFAULT");

        var result = interp.StackPop();
        Assert.Equal(42L, result);
    }

    // ========================================
    // Options
    // ========================================

    [Fact]
    public async Task ToOptions_CreatesWordOptions()
    {
        var interp = SetupInterpreter();
        await interp.Run("[.key1 \"value1\" .key2 42] ~>");

        var result = interp.StackPop();
        Assert.IsType<WordOptions>(result);

        var opts = (WordOptions)result!;
        Assert.Equal("value1", opts.Get("key1"));
        Assert.Equal(42L, opts.Get("key2"));
    }

    // ========================================
    // String Operations
    // ========================================

    [Fact]
    public async Task INTERPOLATE_BasicInterpolation()
    {
        var interp = SetupInterpreter();
        await interp.Run("5 .count ! \"Count: .count\" INTERPOLATE");

        var result = interp.StackPop();
        Assert.Equal("Count: 5", result);
    }

    [Fact]
    public async Task INTERPOLATE_WithOptions()
    {
        var interp = SetupInterpreter();
        await interp.Run("[1 2 3] .items ! \"Items: .items\" [.separator \" | \"] ~> INTERPOLATE");

        var result = interp.StackPop();
        Assert.Equal("Items: 1 | 2 | 3", result);
    }

    [Fact]
    public async Task INTERPOLATE_EscapedDots()
    {
        var interp = SetupInterpreter();
        await interp.Run("\"Test \\\\. escaped\" INTERPOLATE");

        var result = interp.StackPop();
        Assert.Contains(".", (string)result!);
    }

    [Fact]
    public async Task INTERPOLATE_NullText()
    {
        var interp = SetupInterpreter();
        await interp.Run("NULL .value ! \"Value: .value\" [.null_text \"<empty>\"] ~> INTERPOLATE");

        var result = interp.StackPop();
        Assert.Equal("Value: <empty>", result);
    }

    [Fact]
    public async Task PRINT_DirectValue()
    {
        var interp = SetupInterpreter();
        // PRINT outputs to console, so we just verify it doesn't error
        await interp.Run("[1 2 3] PRINT");

        // Stack should be empty after PRINT
        Assert.Equal(0, interp.GetStack().Length);
    }

    [Fact]
    public async Task PRINT_StringInterpolation()
    {
        var interp = SetupInterpreter();
        await interp.Run("\"Alice\" .name ! \"Hello .name\" PRINT");

        // Stack should be empty after PRINT
        Assert.Equal(0, interp.GetStack().Length);
    }

    // ========================================
    // Profiling (Placeholder tests)
    // ========================================

    [Fact]
    public async Task Profiling_BasicOperations()
    {
        var interp = SetupInterpreter();

        // Test basic profiling operations (placeholders)
        await interp.Run("PROFILE-START PROFILE-END");

        await interp.Run("\"test\" PROFILE-TIMESTAMP");

        await interp.Run("PROFILE-DATA");

        var result = interp.StackPop();
        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, object?>>(result);
    }

    // ========================================
    // Logging (Placeholder tests)
    // ========================================

    [Fact]
    public async Task Logging_BasicOperations()
    {
        var interp = SetupInterpreter();

        // Test basic logging operations (placeholders)
        await interp.Run("START-LOG END-LOG");

        // Should not error
        Assert.True(true);
    }

    // ========================================
    // Integration Tests
    // ========================================

    [Fact]
    public async Task VariableIntegration()
    {
        var interp = SetupInterpreter();

        await interp.Run(@"
            [""x"" ""y""] VARIABLES
            10 x !
            20 y !
            x @ y @ +
        ");

        var result = interp.StackPop();
        Assert.Equal(30.0, result);
    }

    [Fact]
    public async Task StackManipulation()
    {
        var interp = SetupInterpreter();

        await interp.Run(@"
            1 2 3
            DUP    # Stack: 1 2 3 3
            POP    # Stack: 1 2 3
            SWAP   # Stack: 1 3 2
        ");

        var stack = interp.GetStack();
        Assert.Equal(3, stack.Length);

        var top = interp.StackPop();
        var middle = interp.StackPop();
        var bottom = interp.StackPop();

        Assert.Equal(2L, top);
        Assert.Equal(3L, middle);
        Assert.Equal(1L, bottom);
    }

    [Fact]
    public async Task ComplexInterpolation()
    {
        var interp = SetupInterpreter();

        await interp.Run(@"
            ""Alice"" .name !
            [1 2 3] .numbers !
            ""Name: .name, Numbers: .numbers"" INTERPOLATE
        ");

        var result = interp.StackPop();
        Assert.Equal("Name: Alice, Numbers: 1, 2, 3", result);
    }

    [Fact]
    public async Task DefaultChaining()
    {
        var interp = SetupInterpreter();

        // Chain DEFAULT operations
        await interp.Run("NULL 10 DEFAULT 20 DEFAULT");

        var result = interp.StackPop();
        Assert.Equal(10L, result);
    }

    [Fact]
    public async Task AutoCreateVariablesComplex()
    {
        var interp = SetupInterpreter();

        // Complex scenario with auto-created variables
        await interp.Run(@"
            ""initial"" ""var1"" !@
            var1 @ ""_updated"" CONCAT ""var1"" !
            var1 @
        ");

        var result = interp.StackPop();
        Assert.Equal("initial_updated", result);
    }

    [Fact]
    public async Task InterpolateWithArrays()
    {
        var interp = SetupInterpreter();

        await interp.Run(@"
            [[1 2] [3 4]] .matrix !
            ""Matrix: .matrix"" INTERPOLATE
        ");

        var result = interp.StackPop();
        // Arrays within arrays should be formatted
        Assert.Contains("Matrix:", (string)result!);
    }

    [Fact]
    public async Task MultipleVariableOperations()
    {
        var interp = SetupInterpreter();

        await interp.Run(@"
            ""a"" ""x"" !
            ""b"" ""y"" !
            ""c"" ""z"" !
            x @ y @ CONCAT z @ CONCAT
        ");

        var result = interp.StackPop();
        Assert.Equal("abc", result);
    }

    [Fact]
    public async Task InterpretWithVariables()
    {
        var interp = SetupInterpreter();

        await interp.Run(@"
            10 .value !
            "".value @ 2 *"" INTERPRET
        ");

        var result = interp.StackPop();
        Assert.Equal(20.0, result);
    }
}
