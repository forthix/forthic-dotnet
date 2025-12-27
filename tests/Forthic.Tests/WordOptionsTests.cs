using Xunit;
using System;
using System.Linq;

namespace Forthic.Tests;

public class WordOptionsTests
{
    [Fact]
    public void CreateFromFlatArray()
    {
        var opts = new WordOptions(new object[] { "depth", 2, "with_key", true });

        Assert.Equal(2, opts.Get("depth"));
        Assert.Equal(true, opts.Get("with_key"));
    }

    [Fact]
    public void RequiresArray()
    {
        var ex = Assert.Throws<ArgumentException>(() => new WordOptions(null!));
        Assert.Contains("must be an array", ex.Message);
    }

    [Fact]
    public void RequiresEvenLength()
    {
        var ex = Assert.Throws<ArgumentException>(() => new WordOptions(new object[] { "depth", 2, "with_key" }));
        Assert.Contains("even length", ex.Message);
    }

    [Fact]
    public void RequiresStringKeys()
    {
        var ex = Assert.Throws<ArgumentException>(() => new WordOptions(new object[] { 123, "value" }));
        Assert.Contains("must be a string", ex.Message);
    }

    [Fact]
    public void DefaultValues()
    {
        var opts = new WordOptions(new object[] { "depth", 2 });

        Assert.Equal(2, opts.Get("depth"));
        Assert.Equal("default", opts.Get("missing", "default"));
        Assert.Null(opts.Get("missing"));
    }

    [Fact]
    public void HasMethod()
    {
        var opts = new WordOptions(new object[] { "depth", 2, "name", "test" });

        Assert.True(opts.Has("depth"));
        Assert.True(opts.Has("name"));
        Assert.False(opts.Has("missing"));
    }

    [Fact]
    public void ToRecord()
    {
        var opts = new WordOptions(new object[] { "depth", 2, "name", "test" });
        var record = opts.ToRecord();

        Assert.Equal(2, record["depth"]);
        Assert.Equal("test", record["name"]);
        Assert.Equal(2, record.Count);
    }

    [Fact]
    public void Keys()
    {
        var opts = new WordOptions(new object[] { "depth", 2, "name", "test", "flag", true });
        var keys = opts.Keys().OrderBy(k => k).ToList();

        Assert.Equal(3, keys.Count);
        Assert.Contains("depth", keys);
        Assert.Contains("name", keys);
        Assert.Contains("flag", keys);
    }

    [Fact]
    public void OverrideBehavior()
    {
        var opts = new WordOptions(new object[] { "key", 1, "key", 2, "key", 3 });

        Assert.Equal(3, opts.Get("key"));
    }

    [Fact]
    public void NullAndUndefinedValues()
    {
        var opts = new WordOptions(new object[] { "null_key", null, "defined_key", 42 });

        Assert.Null(opts.Get("null_key"));
        Assert.Equal(42, opts.Get("defined_key"));
        Assert.True(opts.Has("null_key"));
        Assert.True(opts.Has("defined_key"));
    }

    [Fact]
    public void ComplexValues()
    {
        var nestedArray = new object[] { 1, 2, 3 };
        var nestedRecord = new System.Collections.Generic.Dictionary<string, object> { { "x", 10 }, { "y", 20 } };

        var opts = new WordOptions(new object[] { "array", nestedArray, "record", nestedRecord });

        Assert.Same(nestedArray, opts.Get("array"));
        Assert.Same(nestedRecord, opts.Get("record"));
    }

    [Fact]
    public void ToStringMethod()
    {
        var opts = new WordOptions(new object[] { "depth", 2, "name", "test" });
        var str = opts.ToString();

        Assert.Contains("WordOptions", str);
        Assert.Contains("2", str);
    }

    [Fact]
    public void EmptyOptions()
    {
        var opts = new WordOptions(new object[] { });

        Assert.Empty(opts.Keys());
        Assert.False(opts.Has("anything"));
        Assert.Null(opts.Get("anything"));
        Assert.Equal("default", opts.Get("anything", "default"));
    }
}
