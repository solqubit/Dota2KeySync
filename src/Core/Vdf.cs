using System;
using System.Collections.Generic;
using System.Text;

namespace Dota2KeySync.Core
{
    /// <summary>Minimal parser for Valve's text KeyValues (.vdf) format.</summary>
    public static class Vdf
    {
        public static Dictionary<string, object> Parse(string text)
        {
            int pos = 0;
            return ParseObject(text, ref pos, false);
        }

        static Dictionary<string, object> ParseObject(string s, ref int pos, bool nested)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                SkipWhitespaceAndComments(s, ref pos);
                if (pos >= s.Length) return result;
                if (s[pos] == '}')
                {
                    pos++;
                    if (nested) return result;
                    continue;
                }

                string key = ReadToken(s, ref pos);
                if (key == null) return result;

                SkipWhitespaceAndComments(s, ref pos);
                if (pos >= s.Length) return result;

                object value;
                if (s[pos] == '{')
                {
                    pos++;
                    value = ParseObject(s, ref pos, true);
                }
                else
                {
                    value = ReadToken(s, ref pos) ?? "";
                }
                result[key] = value;
            }
        }

        static void SkipWhitespaceAndComments(string s, ref int pos)
        {
            while (pos < s.Length)
            {
                if (char.IsWhiteSpace(s[pos])) { pos++; continue; }
                if (s[pos] == '/' && pos + 1 < s.Length && s[pos + 1] == '/')
                {
                    while (pos < s.Length && s[pos] != '\n') pos++;
                    continue;
                }
                break;
            }
        }

        static string ReadToken(string s, ref int pos)
        {
            if (pos >= s.Length) return null;
            var sb = new StringBuilder();
            if (s[pos] == '"')
            {
                pos++;
                while (pos < s.Length && s[pos] != '"')
                {
                    char c = s[pos];
                    if (c == '\\' && pos + 1 < s.Length)
                    {
                        char n = s[pos + 1];
                        if (n == 'n') sb.Append('\n');
                        else if (n == 't') sb.Append('\t');
                        else sb.Append(n);
                        pos += 2;
                        continue;
                    }
                    sb.Append(c);
                    pos++;
                }
                pos++; // closing quote
                return sb.ToString();
            }
            // unquoted token
            while (pos < s.Length && !char.IsWhiteSpace(s[pos]) && s[pos] != '{' && s[pos] != '}' && s[pos] != '"')
            {
                sb.Append(s[pos]);
                pos++;
            }
            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}
