using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Uprise.SpeakBack.Core.Content
{
    /// <summary>
    /// Minimal strict JSON reader for content files (SBPS, phrases, golden vectors). Core cannot use
    /// UnityEngine's JsonUtility or take a package dependency, and the files are small, so a
    /// dependency-free parser is simplest. Objects become <see cref="Dictionary{TKey,TValue}"/> of
    /// string → object, arrays become <see cref="List{T}"/> of object, numbers become double.
    /// </summary>
    public static class MiniJson
    {
        /// <summary>Parses <paramref name="json"/>; throws <see cref="FormatException"/> with the character position on malformed input.</summary>
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var parser = new Parser(json);
            parser.SkipWhitespace();
            var value = parser.ReadValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw parser.Error("Unexpected content after the JSON value");
            return value;
        }

        /// <summary>Parses text whose root must be a JSON object.</summary>
        public static IReadOnlyDictionary<string, object> ParseObject(string json) =>
            Parse(json) as Dictionary<string, object> ?? throw new FormatException("JSON root is not an object.");

        private sealed class Parser
        {
            private readonly string _text;
            private int _pos;

            public Parser(string text)
            {
                _text = text;
                // Tolerate a UTF-8 BOM decoded as U+FEFF.
                if (_text.Length > 0 && _text[0] == '﻿') _pos = 1;
            }

            public bool AtEnd => _pos >= _text.Length;

            public FormatException Error(string message) => new FormatException($"{message} at position {_pos}.");

            public void SkipWhitespace()
            {
                while (!AtEnd && (_text[_pos] == ' ' || _text[_pos] == '\t' || _text[_pos] == '\n' || _text[_pos] == '\r')) _pos++;
            }

            public object ReadValue()
            {
                if (AtEnd) throw Error("Unexpected end of input");
                switch (_text[_pos])
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default: return ReadNumber();
                }
            }

            private Dictionary<string, object> ReadObject()
            {
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                _pos++;
                SkipWhitespace();
                if (Peek('}')) { _pos++; return result; }

                while (true)
                {
                    SkipWhitespace();
                    if (!Peek('"')) throw Error("Expected a property name");
                    var key = ReadString();
                    SkipWhitespace();
                    if (!Peek(':')) throw Error("Expected ':'");
                    _pos++;
                    SkipWhitespace();
                    if (result.ContainsKey(key)) throw Error($"Duplicate property '{key}'");
                    result[key] = ReadValue();
                    SkipWhitespace();
                    if (Peek(',')) { _pos++; continue; }
                    if (Peek('}')) { _pos++; return result; }
                    throw Error("Expected ',' or '}'");
                }
            }

            private List<object> ReadArray()
            {
                var result = new List<object>();
                _pos++;
                SkipWhitespace();
                if (Peek(']')) { _pos++; return result; }

                while (true)
                {
                    SkipWhitespace();
                    result.Add(ReadValue());
                    SkipWhitespace();
                    if (Peek(',')) { _pos++; continue; }
                    if (Peek(']')) { _pos++; return result; }
                    throw Error("Expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                _pos++;
                var builder = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw Error("Unterminated string");
                    var c = _text[_pos++];
                    if (c == '"') return builder.ToString();
                    if (c < 0x20) throw Error("Control character in string");
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    if (AtEnd) throw Error("Unterminated escape");
                    var e = _text[_pos++];
                    switch (e)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 > _text.Length) throw Error("Short \\u escape");
                            if (!int.TryParse(_text.Substring(_pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                            {
                                throw Error("Invalid \\u escape");
                            }

                            builder.Append((char)code);
                            _pos += 4;
                            break;
                        default: throw Error($"Invalid escape '\\{e}'");
                    }
                }
            }

            private double ReadNumber()
            {
                var start = _pos;
                if (Peek('-')) _pos++;
                while (!AtEnd && "0123456789+-.eE".IndexOf(_text[_pos]) >= 0) _pos++;
                var token = _text.Substring(start, _pos - start);
                if (token.Length == 0 || !double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    _pos = start;
                    throw Error("Invalid value");
                }

                return value;
            }

            private void Expect(string literal)
            {
                if (string.CompareOrdinal(_text, _pos, literal, 0, literal.Length) != 0) throw Error($"Expected '{literal}'");
                _pos += literal.Length;
            }

            private bool Peek(char c) => !AtEnd && _text[_pos] == c;
        }
    }
}
