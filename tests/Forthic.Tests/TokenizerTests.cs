using Xunit;
using Forthic;

namespace Forthic.Tests;

public class TokenizerTests
{
    [Fact]
    public void SingleWord()
    {
        var tokenizer = new Tokenizer("WORD");
        var token = tokenizer.NextToken();

        Assert.NotNull(token);
        Assert.Equal(TokenType.Word, token.Type);
        Assert.Equal("WORD", token.String);
    }

    [Fact]
    public void MultipleWords()
    {
        var tokenizer = new Tokenizer("WORD1 WORD2 WORD3");
        var expected = new[] { "WORD1", "WORD2", "WORD3" };

        foreach (var exp in expected)
        {
            var token = tokenizer.NextToken();
            Assert.NotNull(token);
            Assert.Equal(TokenType.Word, token.Type);
            Assert.Equal(exp, token.String);
        }
    }

    [Fact]
    public void ArrayTokens()
    {
        var tokenizer = new Tokenizer("[ 1 2 3 ]");

        var token1 = tokenizer.NextToken();
        Assert.Equal(TokenType.StartArray, token1?.Type);

        var token2 = tokenizer.NextToken();
        Assert.Equal(TokenType.Word, token2?.Type);
        Assert.Equal("1", token2?.String);

        // Skip remaining tokens
        tokenizer.NextToken();
        tokenizer.NextToken();

        var token_end = tokenizer.NextToken();
        Assert.Equal(TokenType.EndArray, token_end?.Type);
    }

    [Fact]
    public void ModuleTokens()
    {
        var tokenizer = new Tokenizer("{module}");

        var token1 = tokenizer.NextToken();
        Assert.Equal(TokenType.StartModule, token1?.Type);
        Assert.Equal("module", token1?.String);

        var token2 = tokenizer.NextToken();
        Assert.Equal(TokenType.EndModule, token2?.Type);
    }

    [Fact]
    public void DefinitionTokens()
    {
        var tokenizer = new Tokenizer(": DOUBLE 2 * ;");

        var token1 = tokenizer.NextToken();
        Assert.Equal(TokenType.StartDef, token1?.Type);
        Assert.Equal("DOUBLE", token1?.String);

        // Skip middle tokens
        tokenizer.NextToken();
        tokenizer.NextToken();

        var token_end = tokenizer.NextToken();
        Assert.Equal(TokenType.EndDef, token_end?.Type);
    }

    [Theory]
    [InlineData("\"hello world\"", "hello world")]
    [InlineData("'hello world'", "hello world")]
    [InlineData("^hello world^", "hello world")]
    [InlineData("\"\"\"multi\nline\nstring\"\"\"", "multi\nline\nstring")]
    [InlineData("\"\"", "")]
    public void Strings(string input, string expected)
    {
        var tokenizer = new Tokenizer(input);
        var token = tokenizer.NextToken();

        Assert.NotNull(token);
        Assert.Equal(TokenType.String, token.Type);
        Assert.Equal(expected, token.String);
    }

    [Fact]
    public void Comments()
    {
        var tokenizer = new Tokenizer("WORD1 # this is a comment\nWORD2");

        var token1 = tokenizer.NextToken();
        Assert.Equal(TokenType.Word, token1?.Type);
        Assert.Equal("WORD1", token1?.String);

        var token2 = tokenizer.NextToken();
        Assert.Equal(TokenType.Comment, token2?.Type);
        Assert.Contains("this is a comment", token2?.String);

        var token3 = tokenizer.NextToken();
        Assert.Equal(TokenType.Word, token3?.Type);
        Assert.Equal("WORD2", token3?.String);
    }

    [Theory]
    [InlineData(".field", TokenType.DotSymbol, "field")]
    [InlineData(".field-name", TokenType.DotSymbol, "field-name")]
    [InlineData(".", TokenType.Word, ".")]
    public void DotSymbols(string input, TokenType expectedType, string expectedString)
    {
        var tokenizer = new Tokenizer(input);
        var token = tokenizer.NextToken();

        Assert.NotNull(token);
        Assert.Equal(expectedType, token.Type);
        Assert.Equal(expectedString, token.String);
    }

    [Fact]
    public void Memo()
    {
        var tokenizer = new Tokenizer("@: MEMOIZED 2 * ;");

        var token1 = tokenizer.NextToken();
        Assert.Equal(TokenType.StartMemo, token1?.Type);
        Assert.Equal("MEMOIZED", token1?.String);

        var token2 = tokenizer.NextToken();
        Assert.Equal(TokenType.Word, token2?.Type);
        Assert.Equal("2", token2?.String);
    }

    [Fact]
    public void RFC9557DateTime()
    {
        var tokenizer = new Tokenizer("2025-05-20T08:00:00[America/Los_Angeles]");
        var token = tokenizer.NextToken();

        Assert.NotNull(token);
        Assert.Equal(TokenType.Word, token.Type);
        Assert.Equal("2025-05-20T08:00:00[America/Los_Angeles]", token.String);
    }

    [Fact]
    public void WhitespaceHandling()
    {
        var tokenizer = new Tokenizer("WORD1\t\tWORD2\n\nWORD3");
        var expected = new[] { "WORD1", "WORD2", "WORD3" };

        foreach (var exp in expected)
        {
            var token = tokenizer.NextToken();
            Assert.NotNull(token);
            Assert.Equal(exp, token.String);
        }
    }

    [Fact]
    public void LocationTracking()
    {
        var tokenizer = new Tokenizer("WORD1\nWORD2");

        var token1 = tokenizer.NextToken();
        Assert.Equal(1, token1?.Location.Line);
        Assert.Equal(1, token1?.Location.Column);

        var token2 = tokenizer.NextToken();
        Assert.Equal(2, token2?.Location.Line);
        Assert.Equal(1, token2?.Location.Column);
    }
}
