using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class LexicalAnalyzer
    {
        private StreamReader file;

        private int line, col;

        private Dictionary<string, TokenType> rw;

        private Dictionary<string, TokenType> RW
        {
            get { return rw ?? (rw = CreateRW()); }
        }

        private Dictionary<string, TokenType> CreateRW()
        {
            Dictionary<string, TokenType> d = new Dictionary<string, TokenType>();

            // reservovana slova
            d.Add("var", TokenType.VarRW);
            d.Add("function", TokenType.FunctionRW);
            d.Add("if", TokenType.IfRW);
            d.Add("else", TokenType.ElseRW);
            d.Add("for", TokenType.ForRW);
            d.Add("while", TokenType.WhileRW);
            d.Add("goto", TokenType.GotoRW);
            d.Add("return", TokenType.ReturnRW);

            return d;
        }

        private bool IsCharacter(char c)
        {
            return ((c >= 'A') && (c <= 'Z')) || ((c >= 'a') && (c <= 'z')) || (c == '_');
        }

        private bool IsDigit(char c)
        {
            return (c >= '0') && (c <= '9');
        }

        private bool IsHexaDigit(char c)
        {
            return IsDigit(c) || (c == 'A') || (c == 'a') || (c == 'B') || (c == 'b') || (c == 'C') || (c == 'c') || (c == 'D') || (c == 'd') || (c == 'E') || (c == 'e') || (c == 'F') || (c == 'f');
        }

        private bool IsCharacterOrDigitChar(char c)
        {
            return IsCharacter(c) || IsDigit(c);
        }

        private bool IsNumberChar(char c)
        {
            return IsDigit(c) || (c == 'X') || (c == 'x');
        }

        private bool ReadChar(out char ch, ref string tokenText, out int lineEnd, out int colEnd)
        {
            ch = '\u0000';
            lineEnd = colEnd = 0;
            if (file.EndOfStream)
                return false;

            ch = Convert.ToChar(file.Read());
            col++;
            if (ch == '\u000a') // novy radek
            {
                line++;
                col = 0;
            }
            tokenText += ch;
            lineEnd = line;
            colEnd = col;
            return true;
        }

        private bool ReadChar(out char ch, ref string tokenText, out int lineEnd, out int colEnd, out char next)
        {
            next = '\u0000';
            bool eof = ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
            next = Convert.ToChar((!file.EndOfStream) ? file.Peek() : -1);
            return eof;
        }

        private TokenType GetToken(out string tokenText, out int lineStart, out int colStart, out int lineEnd, out int colEnd, ref string errorMsg)
        {
            lineStart = line;
            colStart = col;
            tokenText = "";

            char next, ch;

            bool eof = ReadChar(out ch, ref tokenText, out lineEnd, out colEnd, out next);

            switch (ch)
            {
                // bile znaky
                case '\u0009': // TAB
                case '\u000a': // LF
                case '\u000d': // CR
                case ' ': // space
                    return TokenType.Whitespace;

                // zavorky
                case '(':
                    return TokenType.ParenthesisLeft;
                case ')':
                    return TokenType.ParenthesisRight;
                case '{':
                    return TokenType.BraceLeft;
                case '}':
                    return TokenType.BraceRight;

                // operatory
                case '+':
                    if (next == '+')
                    {
                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        return TokenType.PlusPlus;
                    }
                    return TokenType.Plus;
                case '-':
                    if (next == '-')
                    {
                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        return TokenType.MinusMinus;
                    }
                    return TokenType.Minus;
                case '*':
                    return TokenType.Multi;
                case '=':
                    if (next == '=')
                    {
                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        return TokenType.EqualsEquals;
                    }
                    return TokenType.Equals;
                case '<':
                    if (next == '=')
                    {
                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        return TokenType.LessOrEquals;
                    }
                    return TokenType.Less;
                case '>':
                    if (next == '=')
                    {
                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        return TokenType.MoreOrEquals;
                    }
                    return TokenType.More;
                case '|':
                    if (next == '|')
                    {
                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        return TokenType.Or;
                    }
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, lineEnd, colEnd);
                    return TokenType.Error;
                case '&':
                    if (next == '&')
                    {
                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        return TokenType.And;
                    }
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, lineEnd, colEnd);
                    return TokenType.Error;
                case '!':
                    if (next == '=')
                    {
                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        return TokenType.NotEquals;
                    }
                    return TokenType.Neg;

                // oddelovace
                case ';':
                    return TokenType.Semicolon;
                case ',':
                    return TokenType.Comma;
                case ':':
                    return TokenType.Colon;

                // komentare
                case '/':
                    if (next == '/')
                    {
                        // precist komentar do konce radku
                        while ((ch != '\u000a') && (!eof))
                        {
                            eof = ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                        }
                        return TokenType.Comment;
                    }
                    if (next == '*')
                    {
                        bool eoc = false;
                        while (!eoc && !eof)
                        {
                            eof = ReadChar(out ch, ref tokenText, out lineEnd, out colEnd, out next);
                            if ((ch == '*') && (next == '/'))
                            {
                                ReadChar(out ch, ref tokenText, out lineEnd, out colEnd);
                                eoc = true;
                            }
                        }

                        if (!eoc)
                        {
                            errorMsg = "Neukonceny komentar";
                            return TokenType.Error;
                        }
                        return TokenType.Comment;
                    }
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, lineEnd, colEnd);
                    return TokenType.Error;

                // reservovana slova, identifikatory & cisla & nezname znaky
                default:

                    if (IsCharacter(ch)) // identifikator | rezervovane slovo
                    {
                        while (IsCharacterOrDigitChar(next))
                        {
                            ReadChar(out ch, ref tokenText, out lineEnd, out colEnd, out next);
                        }

                        TokenType t;
                        if (!RW.TryGetValue(tokenText, out t))
                            t = TokenType.Identifier;
                        return t;
                    }
                    else if (IsDigit(ch)) // cislo
                    {
                        int len;
                        bool numEnd = false, hexa = false;
                        while (!numEnd)
                        {
                            len = tokenText.Length;

                            if (!(IsDigit(next) || (hexa && IsHexaDigit(next)))) // neni to cislice, ci sestnackova cislice
                            {
                                if (IsCharacter(next))
                                {
                                    if (!hexa && ((next == 'x') || (next == 'X')) && (len == 1) && (tokenText[0] == '0'))
                                    {
                                        hexa = true;
                                        ReadChar(out ch, ref tokenText, out lineEnd, out colEnd, out next);
                                        continue;
                                    }
                                    // pismeno v cisle znamena syntaktickou chybu
                                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", next, lineEnd, colEnd + 1);
                                    return TokenType.Error;
                                }

                                numEnd = true;
                            }
                            else
                                ReadChar(out ch, ref tokenText, out lineEnd, out colEnd, out next);
                        }

                        char last = tokenText[tokenText.Length - 1];
                        if (!(IsDigit(last) || (hexa && IsHexaDigit(last)))) // tato podminka se vylepsi...
                        {
                            errorMsg = string.Format("Spatny format cisla '{0}', radek {1}, sloupec {2}", tokenText, lineStart, colStart);
                            return TokenType.Error;
                        }
                        return TokenType.Number;
                    }

                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, lineEnd, colEnd);
                    return TokenType.Error;
            }

        }

        public List<TokenModel> GetAllTokens(string fileName, bool isPrint)
        {
            return new List<TokenModel>(GetTokens(fileName, isPrint));
        }

        public IEnumerable<TokenModel> GetTokens(string fileName, bool isPrint)
        {
            file = new StreamReader(fileName);
            line = 1;
            col = 0;

            string tokenText, errorMsg = "";
            int lineStart, colStart, lineEnd, colEnd;
            TokenType token;

            while (!file.EndOfStream)
            {
                token = GetToken(out tokenText, out lineStart, out colStart, out lineEnd, out colEnd, ref errorMsg);

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    yield return new TokenModel { IsError = true, ErrorMessage = errorMsg };
                    yield break;
                }

                if ((isPrint) && (token != TokenType.Whitespace))
                {
                    Console.WriteLine("{0}: '{1}'", token, tokenText);
                }

                if (token == TokenType.End) // konec cteni souboru
                    break;

                if ((token != TokenType.Comment) && (token != TokenType.Whitespace))
                    yield return new TokenModel { Token = token, TokenText = tokenText, Line = lineStart, Column = colStart };
            }

            file.Close();
            file.Dispose();

            yield return new TokenModel { Token = TokenType.End };
        }
    }
}
