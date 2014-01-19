using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class LexicalAnalyzer
    {
        // tridni promenne pro lexikalni analyzu
        private bool isPrint;
        private string errorMsg;

        // tridni promenne pro praci se souborem
        private string fileName;
        private StreamReader file;
        private bool eof;

        // tridni promenne pro precteny znak
        private char ch, next;
        private int chLine, chCol;

        // tridni promenne pro precteny token
        private string tokenText;
        int lineStart, colStart;

        public LexicalAnalyzer(string fileName, bool isPrint)
        {
            this.fileName = fileName;
            this.isPrint = isPrint;
        }

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

        private char ReadChar()
        {
            if (eof)
            {
                ch = '\u0000';
                return ch;
            }

            // precti znak ze souboru
            ch = Convert.ToChar(file.Read());
            chCol++;
            if (ch == '\u000a') // toto je novy radek
            {
                chLine++;
                chCol = 0;
            }
            // zjisti, co je dalsi znak
            next = Convert.ToChar((!file.EndOfStream) ? file.Peek() : 0);
            eof = file.EndOfStream;

            return ch;
        }

        private TokenType GetToken()
        {
            tokenText = "";
            tokenText += ReadChar();

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
                        tokenText += ReadChar();
                        return TokenType.PlusPlus;
                    }
                    return TokenType.Plus;
                case '-':
                    if (next == '-')
                    {
                        tokenText += ReadChar();
                        return TokenType.MinusMinus;
                    }
                    return TokenType.Minus;
                case '*':
                    return TokenType.Multi;
                case '=':
                    if (next == '=')
                    {
                        tokenText += ReadChar();
                        return TokenType.EqualsEquals;
                    }
                    return TokenType.Equals;
                case '<':
                    if (next == '=')
                    {
                        tokenText += ReadChar();
                        return TokenType.LessOrEquals;
                    }
                    return TokenType.Less;
                case '>':
                    if (next == '=')
                    {
                        tokenText += ReadChar();
                        return TokenType.MoreOrEquals;
                    }
                    return TokenType.More;
                case '|':
                    if (next == '|')
                    {
                        tokenText += ReadChar();
                        return TokenType.Or;
                    }
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, chLine, chCol);
                    return TokenType.Error;
                case '&':
                    if (next == '&')
                    {
                        tokenText += ReadChar();
                        return TokenType.And;
                    }
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, chLine, chCol);
                    return TokenType.Error;
                case '!':
                    if (next == '=')
                    {
                        tokenText += ReadChar();
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
                        while ((next != '\u000a') && (next != '\u000d') && (!eof))
                        {
                            tokenText += ReadChar();
                        }
                        return TokenType.Comment;
                    }
                    if (next == '*')
                    {
                        bool eoc = false;
                        while (!eoc && !eof)
                        {
                            tokenText += ReadChar();
                            if ((ch == '*') && (next == '/'))
                            {
                                tokenText += ReadChar();
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
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, chLine, chCol);
                    return TokenType.Error;

                // reservovana slova, identifikatory & cisla & nezname znaky
                default:

                    if (IsCharacter(ch)) // identifikator | rezervovane slovo
                    {
                        while (IsCharacterOrDigitChar(next))
                        {
                            tokenText += ReadChar();
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
                                        tokenText += ReadChar();
                                        continue;
                                    }
                                    // pismeno v cisle znamena syntaktickou chybu
                                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", next, chLine, chCol + 1);
                                    return TokenType.Error;
                                }

                                numEnd = true;
                            }
                            else
                                tokenText += ReadChar();
                        }

                        char last = tokenText[tokenText.Length - 1];
                        if (!(IsDigit(last) || (hexa && IsHexaDigit(last)))) // tato podminka se vylepsi...
                        {
                            errorMsg = string.Format("Spatny format cisla '{0}', radek {1}, sloupec {2}", tokenText, lineStart, colStart);
                            return TokenType.Error;
                        }
                        return TokenType.Number;
                    }

                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, chLine, chCol);
                    return TokenType.Error;
            }

        }

        public List<TokenModel> GetAllTokens()
        {
            return new List<TokenModel>(GetTokens());
        }

        public IEnumerable<TokenModel> GetTokens()
        {
            file = new StreamReader(fileName);
            chLine = 1;
            chCol = 0;

            TokenType token;

            while (!eof)
            {
                lineStart = chLine;
                colStart = chCol;
                token = GetToken();

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

            Console.WriteLine("End: file is closed");
            yield return new TokenModel { Token = TokenType.End };
        }
    }
}
