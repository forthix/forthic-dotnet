# Forthic .NET Runtime

A C#/.NET implementation of the Forthic stack-based concatenative programming language.

## Overview

Forthic is a stack-based, concatenative language designed for composable transformations. This is the official .NET runtime implementation, providing full compatibility with other Forthic runtimes while leveraging .NET's rich ecosystem and LINQ.

## Features

- ✅ Complete Forthic language implementation
- ✅ All 8 standard library modules
- ✅ Attribute-based decorators with reflection
- ✅ LINQ integration for functional operations
- ✅ NodaTime for robust temporal types
- ✅ gRPC support for multi-runtime execution
- ✅ CLI with REPL, script execution, and eval modes
- ✅ Comprehensive xUnit test suite

## Installation

```bash
dotnet add package Forthic
```

## Usage

### As a Library

```csharp
using Forthic;

var interp = new StandardInterpreter();

await interp.RunAsync("[1 2 3] \"2 *\" MAP");

var result = interp.StackPop();
// result is [2, 4, 6]
```

### CLI

```bash
# REPL mode
dotnet forthic repl

# Execute a script
dotnet forthic run script.forthic

# Eval mode (one-liner)
dotnet forthic eval "[1 2 3] LENGTH"
```

## Development

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~InterpreterTests"

# Build for release
dotnet build -c Release
```

## Project Structure

```
forthic-dotnet/
├── src/
│   ├── Forthic/              # Core library
│   │   ├── Interpreter.cs
│   │   ├── Tokenizer.cs
│   │   ├── Module.cs
│   │   ├── Decorators/       # Attribute system
│   │   └── Modules/Standard/ # Standard library (8 modules)
│   ├── Forthic.Grpc/         # gRPC support
│   └── Forthic.Cli/          # CLI tool
└── tests/
    └── Forthic.Tests/        # xUnit tests
```

## Standard Library Modules

- **core**: Stack operations, variables, control flow
- **array**: Data transformation (MAP, SELECT, SORT, etc.)
- **record**: Dictionary operations
- **string**: Text processing
- **math**: Arithmetic operations
- **boolean**: Logical operations
- **datetime**: Date/time manipulation (using NodaTime)
- **json**: JSON serialization (using System.Text.Json)

## Attribute-Based Decorators

Define Forthic words using C# attributes:

```csharp
public class ArrayModule : DecoratedModule
{
    [ForthicWord("( array word -- result )", "Maps a word over an array")]
    public async Task<object> MAP(List<object> array, IWord word)
    {
        var results = new List<object>();
        foreach (var item in array)
        {
            var result = await word.ExecuteAsync(interp);
            results.Add(result);
        }
        return results;
    }
}
```

## LINQ Integration

Leverage LINQ for functional operations:

```csharp
[ForthicWord("( array predicate -- result )", "Filters array")]
public async Task<object> SELECT(List<object> array, IWord predicate)
{
    return array.Where(item => {
        /* evaluate predicate */
        return true;
    }).ToList();
}
```

## Multi-Runtime Execution

This runtime supports calling words from other Forthic runtimes via gRPC:

```csharp
// Call a Python word from .NET
var result = await interp.ExecuteRemoteWordAsync(
    "python-runtime", "MY-WORD", args
);
```

## NodaTime Integration

Robust date/time handling with NodaTime:

```csharp
var zonedDateTime = ZonedDateTime.FromDateTimeOffset(
    DateTimeOffset.Now, DateTimeZone.Utc
);
```

## License

BSD 2-CLAUSE

## Links

- [Forthic Language Specification](https://github.com/forthix/forthic)
- [TypeScript Runtime](https://github.com/forthix/forthic-ts) (reference implementation)
- [NuGet Package](https://www.nuget.org/packages/Forthic)
- [Documentation](https://forthix.github.io/forthic-dotnet)
