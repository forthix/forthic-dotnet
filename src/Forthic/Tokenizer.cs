using System;
using System.Collections.Generic;
using System.Text;

namespace Forthic;

// ============================================================================
// Token Types
// ============================================================================

public enum TokenType
{
    String = 1,
    Comment,
    StartArray,
    EndArray,
    StartModule,
    EndModule,
    StartDef,
    EndDef,
    StartMemo,
    Word,
    DotSymbol,
    EOS
}

// ============================================================================
// Token
// ============================================================================

public class Token
{
    public TokenType Type { get; set; }
    public string String { get; set; }
    public CodeLocation Location { get; set; }

    public Token(TokenType type, string str, CodeLocation location)
    {
        Type = type;
        String = str;
        Location = location;
    }
}

// ============================================================================
// Tokenizer
// ============================================================================

public class Tokenizer
{
    private readonly CodeLocation _referenceLocation;
    private int _line;
    private int _column;
    private readonly string _inputString;
    private int _inputPos;
    private readonly char[] _whitespace;
    private readonly char[] _quoteChars;
    private int _tokenStartPos;
    private int _tokenLine;
    private int _tokenColumn;
    private readonly StringBuilder _tokenString;
    private readonly bool _streaming;

    public Tokenizer(string inputString, CodeLocation? referenceLocation = null, bool streaming = false)
    {
        _referenceLocation = referenceLocation ?? new CodeLocation();
        _line = _referenceLocation.Line;
        _column = _referenceLocation.Column;
        _inputString = UnescapeString(inputString);
        _inputPos = 0;
        _whitespace = new[] { ' ', '\t', '\n', '\r', '(', ')', ',' };
        _quoteChars = new[] { '"', '\'', '^' };
        _tokenStartPos = 0;
        _tokenLine = 0;
        _tokenColumn = 0;
        _tokenString = new StringBuilder();
        _streaming = streaming;
    }

    // ========================================================================
    // Helper Functions
    // ========================================================================

    private static string UnescapeString(string s)
    {
        return s.Replace("&lt;", "<").Replace("&gt;", ">");
    }

    private void ClearTokenString()
    {
        _tokenString.Clear();
    }

    private void NoteStartToken()
    {
        _tokenStartPos = _inputPos + _referenceLocation.StartPos;
        _tokenLine = _line;
        _tokenColumn = _column;
    }

    private bool IsWhitespace(char ch)
    {
        return Array.IndexOf(_whitespace, ch) >= 0;
    }

    private bool IsQuote(char ch)
    {
        return Array.IndexOf(_quoteChars, ch) >= 0;
    }

    private bool IsTripleQuote(int index, char ch)
    {
        if (!IsQuote(ch)) return false;
        if (index + 2 >= _inputString.Length) return false;
        return _inputString[index + 1] == ch && _inputString[index + 2] == ch;
    }

    private bool IsStartMemo(int index)
    {
        if (index + 1 >= _inputString.Length) return false;
        return _inputString[index] == '@' && _inputString[index + 1] == ':';
    }

    private void AdvancePosition(int numChars)
    {
        if (numChars >= 0)
        {
            for (int i = 0; i < numChars; i++)
            {
                if (_inputPos < _inputString.Length && _inputString[_inputPos] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _inputPos++;
            }
        }
        else
        {
            for (int i = 0; i < -numChars; i++)
            {
                _inputPos--;
                if (_inputPos < 0)
                {
                    throw new InvalidOperationException("Invalid input position");
                }
                if (_inputString[_inputPos] == '\n')
                {
                    _line--;
                    _column = 1;
                }
                else
                {
                    _column--;
                }
            }
        }
    }

    private CodeLocation GetTokenLocation()
    {
        return new CodeLocation(
            _referenceLocation.Source,
            _tokenLine,
            _tokenColumn,
            _tokenStartPos,
            _tokenStartPos + _tokenString.Length
        );
    }

    // ========================================================================
    // Public API
    // ========================================================================

    public Token? NextToken()
    {
        ClearTokenString();
        return TransitionFromSTART();
    }

    // ========================================================================
    // State Transitions
    // ========================================================================

    private Token? TransitionFromSTART()
    {
        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            NoteStartToken();
            AdvancePosition(1);

            if (IsWhitespace(ch))
            {
                continue;
            }
            else if (ch == '#')
            {
                return TransitionFromCOMMENT();
            }
            else if (ch == ':')
            {
                return TransitionFromSTART_DEFINITION();
            }
            else if (IsStartMemo(_inputPos - 1))
            {
                AdvancePosition(1); // Skip over ":" in "@:"
                return TransitionFromSTART_MEMO();
            }
            else if (ch == ';')
            {
                _tokenString.Append(ch);
                return new Token(TokenType.EndDef, ch.ToString(), GetTokenLocation());
            }
            else if (ch == '[')
            {
                _tokenString.Append(ch);
                return new Token(TokenType.StartArray, ch.ToString(), GetTokenLocation());
            }
            else if (ch == ']')
            {
                _tokenString.Append(ch);
                return new Token(TokenType.EndArray, ch.ToString(), GetTokenLocation());
            }
            else if (ch == '{')
            {
                return TransitionFromGATHER_MODULE();
            }
            else if (ch == '}')
            {
                _tokenString.Append(ch);
                return new Token(TokenType.EndModule, ch.ToString(), GetTokenLocation());
            }
            else if (IsTripleQuote(_inputPos - 1, ch))
            {
                AdvancePosition(2); // Skip over 2nd and 3rd quote chars
                return TransitionFromGATHER_TRIPLE_QUOTE_STRING(ch);
            }
            else if (IsQuote(ch))
            {
                return TransitionFromGATHER_STRING(ch);
            }
            else if (ch == '.')
            {
                AdvancePosition(-1); // Back up to beginning of dot symbol
                return TransitionFromGATHER_DOT_SYMBOL();
            }
            else
            {
                AdvancePosition(-1); // Back up to beginning of word
                return TransitionFromGATHER_WORD();
            }
        }
        return new Token(TokenType.EOS, "", GetTokenLocation());
    }

    private Token TransitionFromCOMMENT()
    {
        NoteStartToken();
        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            _tokenString.Append(ch);
            AdvancePosition(1);
            if (ch == '\n')
            {
                AdvancePosition(-1);
                break;
            }
        }
        return new Token(TokenType.Comment, _tokenString.ToString(), GetTokenLocation());
    }

    private Token? TransitionFromSTART_DEFINITION()
    {
        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            AdvancePosition(1);

            if (IsWhitespace(ch))
            {
                continue;
            }
            else if (IsQuote(ch))
            {
                throw new ForthicException("Definition names can't have quotes in them");
            }
            else
            {
                AdvancePosition(-1);
                return TransitionFromGATHER_DEFINITION_NAME();
            }
        }
        throw new ForthicException("Got EOS in START_DEFINITION");
    }

    private Token? TransitionFromSTART_MEMO()
    {
        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            AdvancePosition(1);

            if (IsWhitespace(ch))
            {
                continue;
            }
            else if (IsQuote(ch))
            {
                throw new ForthicException("Memo names can't have quotes in them");
            }
            else
            {
                AdvancePosition(-1);
                return TransitionFromGATHER_MEMO_NAME();
            }
        }
        throw new ForthicException("Got EOS in START_MEMO");
    }

    private void GatherDefinitionName()
    {
        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            AdvancePosition(1);

            if (IsWhitespace(ch))
            {
                break;
            }
            if (IsQuote(ch))
            {
                throw new ForthicException("Definition names can't have quotes in them");
            }
            if (ch == '[' || ch == ']' || ch == '{' || ch == '}')
            {
                throw new ForthicException($"Definition names can't have '{ch}' in them");
            }
            _tokenString.Append(ch);
        }
    }

    private Token TransitionFromGATHER_DEFINITION_NAME()
    {
        NoteStartToken();
        GatherDefinitionName();
        return new Token(TokenType.StartDef, _tokenString.ToString(), GetTokenLocation());
    }

    private Token TransitionFromGATHER_MEMO_NAME()
    {
        NoteStartToken();
        GatherDefinitionName();
        return new Token(TokenType.StartMemo, _tokenString.ToString(), GetTokenLocation());
    }

    private Token TransitionFromGATHER_MODULE()
    {
        NoteStartToken();
        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            AdvancePosition(1);

            if (IsWhitespace(ch))
            {
                break;
            }
            else if (ch == '}')
            {
                AdvancePosition(-1);
                break;
            }
            else
            {
                _tokenString.Append(ch);
            }
        }
        return new Token(TokenType.StartModule, _tokenString.ToString(), GetTokenLocation());
    }

    private Token? TransitionFromGATHER_TRIPLE_QUOTE_STRING(char delim)
    {
        NoteStartToken();

        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];

            if (ch == delim && IsTripleQuote(_inputPos, ch))
            {
                // Check if this triple quote is followed by at least one more quote (greedy mode trigger)
                if (_inputPos + 3 < _inputString.Length && _inputString[_inputPos + 3] == delim)
                {
                    // Greedy mode: include this quote as content and continue looking for the end
                    AdvancePosition(1); // Advance by 1 to catch overlapping sequences
                    _tokenString.Append(delim);
                    continue;
                }

                // Normal behavior: close at first triple quote
                AdvancePosition(3);
                return new Token(TokenType.String, _tokenString.ToString(), GetTokenLocation());
            }
            else
            {
                AdvancePosition(1);
                _tokenString.Append(ch);
            }
        }

        if (_streaming)
        {
            return null;
        }
        throw new ForthicException("Unterminated string");
    }

    private Token? TransitionFromGATHER_STRING(char delim)
    {
        NoteStartToken();

        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            AdvancePosition(1);

            if (ch == delim)
            {
                return new Token(TokenType.String, _tokenString.ToString(), GetTokenLocation());
            }
            else
            {
                _tokenString.Append(ch);
            }
        }

        if (_streaming)
        {
            return null;
        }
        throw new ForthicException("Unterminated string");
    }

    private Token TransitionFromGATHER_WORD()
    {
        NoteStartToken();
        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            AdvancePosition(1);

            if (IsWhitespace(ch))
            {
                break;
            }
            if (ch == ';' || ch == '{' || ch == '}' || ch == '#')
            {
                AdvancePosition(-1);
                break;
            }

            // Handle RFC 9557 datetime with IANA timezone: 2025-05-20T08:00:00[America/Los_Angeles]
            // When we see '[', check if token looks like a datetime (contains 'T')
            // If so, include the bracketed timezone as part of the token
            if (ch == '[')
            {
                if (_tokenString.ToString().Contains('T'))
                {
                    // This looks like a datetime, gather until ']'
                    _tokenString.Append(ch);
                    while (_inputPos < _inputString.Length)
                    {
                        char tzChar = _inputString[_inputPos];
                        AdvancePosition(1);
                        _tokenString.Append(tzChar);
                        if (tzChar == ']') break;
                    }
                    break;
                }
                else
                {
                    // Not a datetime, treat '[' as delimiter
                    AdvancePosition(-1);
                    break;
                }
            }
            if (ch == ']')
            {
                AdvancePosition(-1);
                break;
            }
            _tokenString.Append(ch);
        }
        return new Token(TokenType.Word, _tokenString.ToString(), GetTokenLocation());
    }

    private Token TransitionFromGATHER_DOT_SYMBOL()
    {
        NoteStartToken();
        var fullTokenString = new StringBuilder();

        while (_inputPos < _inputString.Length)
        {
            char ch = _inputString[_inputPos];
            AdvancePosition(1);

            if (IsWhitespace(ch))
            {
                break;
            }
            if (ch == ';' || ch == '[' || ch == ']' || ch == '{' || ch == '}' || ch == '#')
            {
                AdvancePosition(-1);
                break;
            }
            else
            {
                fullTokenString.Append(ch);
                _tokenString.Append(ch);
            }
        }

        string fullToken = fullTokenString.ToString();

        // If dot symbol has no characters after the dot, treat it as a word
        if (fullToken.Length < 2) // "." + at least 1 char = 2 minimum
        {
            return new Token(TokenType.Word, fullToken, GetTokenLocation());
        }

        // For DOT_SYMBOL, return the string without the dot prefix
        string symbolWithoutDot = fullToken.Substring(1);
        return new Token(TokenType.DotSymbol, symbolWithoutDot, GetTokenLocation());
    }
}
