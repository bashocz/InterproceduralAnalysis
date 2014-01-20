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
        int tokenStartLine, tokenStartCol;

        public LexicalAnalyzer(string fileName, bool isPrint)
        {
            this.fileName = fileName;
            this.isPrint = isPrint;
        }

        private Dictionary<string, TokenTypes> rw;

        private Dictionary<string, TokenTypes> RW
        {
            get { return rw ?? (rw = CreateRW()); }
        }

        private Dictionary<string, TokenTypes> CreateRW()
        {
            Dictionary<string, TokenTypes> d = new Dictionary<string, TokenTypes>();

            // reservovana slova
            d.Add("var", TokenTypes.VarRW);
            d.Add("function", TokenTypes.FunctionRW);
            d.Add("if", TokenTypes.IfRW);
            d.Add("else", TokenTypes.ElseRW);
            d.Add("for", TokenTypes.ForRW);
            d.Add("while", TokenTypes.WhileRW);
            d.Add("goto", TokenTypes.GotoRW);
            d.Add("return", TokenTypes.ReturnRW);

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

        private TokenTypes GetToken()
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
                    return TokenTypes.Whitespace;

                // zavorky
                case '(':
                    return TokenTypes.ParenthesisLeft;
                case ')':
                    return TokenTypes.ParenthesisRight;
                case '{':
                    return TokenTypes.BraceLeft;
                case '}':
                    return TokenTypes.BraceRight;

                // operatory
                case '+':
                    if (next == '+')
                    {
                        tokenText += ReadChar();
                        return TokenTypes.PlusPlus;
                    }
                    return TokenTypes.Plus;
                case '-':
                    if (next == '-')
                    {
                        tokenText += ReadChar();
                        return TokenTypes.MinusMinus;
                    }
                    return TokenTypes.Minus;
                case '*':
                    return TokenTypes.Multi;
                case '=':
                    if (next == '=')
                    {
                        tokenText += ReadChar();
                        return TokenTypes.EqualsEquals;
                    }
                    return TokenTypes.Equals;
                case '<':
                    if (next == '=')
                    {
                        tokenText += ReadChar();
                        return TokenTypes.LessOrEquals;
                    }
                    return TokenTypes.Less;
                case '>':
                    if (next == '=')
                    {
                        tokenText += ReadChar();
                        return TokenTypes.MoreOrEquals;
                    }
                    return TokenTypes.More;
                case '|':
                    if (next == '|')
                    {
                        tokenText += ReadChar();
                        return TokenTypes.Or;
                    }
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, chLine, chCol);
                    return TokenTypes.Error;
                case '&':
                    if (next == '&')
                    {
                        tokenText += ReadChar();
                        return TokenTypes.And;
                    }
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, chLine, chCol);
                    return TokenTypes.Error;
                case '!':
                    if (next == '=')
                    {
                        tokenText += ReadChar();
                        return TokenTypes.NotEquals;
                    }
                    return TokenTypes.Neg;

                // oddelovace
                case ';':
                    return TokenTypes.Semicolon;
                case ',':
                    return TokenTypes.Comma;
                case ':':
                    return TokenTypes.Colon;

                // komentare
                case '/':
                    if (next == '/')
                    {
                        // precist komentar do konce radku
                        while ((next != '\u000a') && (next != '\u000d') && (!eof))
                        {
                            tokenText += ReadChar();
                        }
                        return TokenTypes.Comment;
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
                            return TokenTypes.Error;
                        }
                        return TokenTypes.Comment;
                    }
                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, chLine, chCol);
                    return TokenTypes.Error;

                // reservovana slova, identifikatory & cisla & nezname znaky
                default:

                    if (IsCharacter(ch)) // identifikator | rezervovane slovo
                    {
                        while (IsCharacterOrDigitChar(next))
                        {
                            tokenText += ReadChar();
                        }

                        TokenTypes t;
                        if (!RW.TryGetValue(tokenText, out t))
                            t = TokenTypes.Identifier;
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
                                    return TokenTypes.Error;
                                }

                                numEnd = true;
                            }
                            else
                                tokenText += ReadChar();
                        }

                        char last = tokenText[tokenText.Length - 1];
                        if (!(IsDigit(last) || (hexa && IsHexaDigit(last)))) // tato podminka se vylepsi...
                        {
                            errorMsg = string.Format("Spatny format cisla '{0}', radek {1}, sloupec {2}", tokenText, tokenStartLine, tokenStartCol);
                            return TokenTypes.Error;
                        }
                        return TokenTypes.Number;
                    }

                    errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, chLine, chCol);
                    return TokenTypes.Error;
            }

        }

        private IEnumerable<TokenModel> GetTokensFromFile()
        {
            file = new StreamReader(fileName);
            chLine = 1;
            chCol = 0;

            TokenTypes token;

            while (!eof)
            {
                tokenStartLine = chLine;
                tokenStartCol = chCol;
                token = GetToken();

                if (!string.IsNullOrEmpty(errorMsg))
                {
                    yield return new TokenModel { IsError = true, ErrorMessage = errorMsg };
                    file.Close();
                    file.Dispose();
                    yield break;
                }

                if (token == TokenTypes.End) // konec cteni souboru
                    break;

                if (token != TokenTypes.Whitespace)
                    yield return new TokenModel { Token = token, TokenText = tokenText, TokenStartLine = tokenStartLine, TokenStartColumn = tokenStartCol };
            }

            file.Close();
            file.Dispose();

            yield return new TokenModel { Token = TokenTypes.End, TokenText = "End: file is closed" };
        }

        private List<TokenModel> tokens;
        private int tokenIdx;
        private TokenModel errorToken;

        public bool ReadNextToken()
        {
            if (tokens == null)
            {
                tokens = new List<TokenModel>(GetTokensFromFile());
                errorToken = null;
                tokenIdx = -1;
                if (tokens.Count == 0)
                    errorToken = ActualToken = new TokenModel { IsError = true, ErrorMessage = "Zadny token, prazdny soubor..." };
            }
            if (errorToken != null)
                return false;
            if (tokenIdx >= tokens.Count)
                return true; // konec souboru, vsechny tokeny precteny :-)

            bool loop = true;
            while (loop)
            {
                tokenIdx++;
                if (isPrint)
                    Console.WriteLine("{0}: '{1}'", tokens[tokenIdx].Token, tokens[tokenIdx].TokenText);
                loop = ((tokenIdx < tokens.Count) && (tokens[tokenIdx].Token == TokenTypes.Comment));
            }
            ActualToken = tokens[tokenIdx];
            if (ActualToken.IsError)
            {
                errorToken = ActualToken;
                return false;
            }

            int nextIdx = tokenIdx + 1;
            while ((nextIdx < tokens.Count) && (tokens[nextIdx].Token == TokenTypes.Comment))
                nextIdx++;
            if (nextIdx < tokens.Count)
                NextToken = tokens[nextIdx];

            return true;
        }

        public TokenModel ActualToken { get; private set; }
        public TokenModel NextToken { get; private set; }
    }
}
