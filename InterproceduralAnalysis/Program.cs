using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterproceduralAnalysis
{
    class Program
    {
        private static string programFile;
        private static bool printLA;

        private static string errorMsg;

        #region Lexikalni analyza

        private static string tokenText;

        private static StreamReader file;
        private static bool eof;

        private static int line, col;
        private static int tokenLine, tokenCol;

        private static char ch;
        private static char next;

        private static Dictionary<string, Tokens> rw;

        private static void InitLA()
        {
            rw = new Dictionary<string, Tokens>();

            // reserved words
            rw.Add("var", Tokens.VarCmd);
            rw.Add("function", Tokens.FunctionCmd);
            rw.Add("if", Tokens.IfCmd);
            rw.Add("else", Tokens.ElseCmd);
            rw.Add("for", Tokens.ForCmd);
            rw.Add("while", Tokens.WhileCmd);
            rw.Add("goto", Tokens.GotoCmd);
            rw.Add("return", Tokens.ReturnCmd);
        }

        private static bool IsCharacter(char c)
        {
            return ((c >= 'A') && (c <= 'Z')) || ((c >= 'a') && (c <= 'z')) || (c == '_');
        }

        private static bool IsDigit(char c)
        {
            return (c >= '0') && (c <= '9');
        }

        private static bool IsHexaDigit(char c)
        {
            return IsDigit(c) || (c == 'A') || (c == 'a') || (c == 'B') || (c == 'b') || (c == 'C') || (c == 'c') || (c == 'D') || (c == 'd') || (c == 'E') || (c == 'e') || (c == 'F') || (c == 'f');
        }

        private static bool IsCharacterOrDigitChar(char c)
        {
            return IsCharacter(c) || IsDigit(c);
        }

        private static bool IsNumberChar(char c)
        {
            return IsDigit(c) || (c == 'X') || (c == 'x');
        }

        private static void ReadChar()
        {
            ch = Convert.ToChar(file.Read());
            next = Convert.ToChar((!file.EndOfStream) ? file.Peek() : 0);
            tokenText += ch;
            col++;
            if (ch == '\u000a')
            {
                line++;
                col = 0;
            }
        }

        private static void ReadToEndOfLine()
        {
            while ((next != '\u000d') && (next != '\u000a'))
            {
                ReadChar();
            }
        }

        private static bool ReadToEndOfComment()
        {
            bool eoc = false;
            while (!eoc && !eof)
            {
                ReadChar();
                if ((ch == '*') && (next == '/'))
                {
                    ReadChar();
                    eoc = true;
                }
            }
            return eoc;
        }

        private static Tokens LA()
        {
            if (!eof)
            {
                // start
                if (file == null)
                {
                    InitLA();
                    file = new StreamReader(programFile);
                    line = 1;
                    col = 0;
                }

                tokenText = "";

                while (!file.EndOfStream)
                {
                    ReadChar();
                    tokenLine = line;
                    tokenCol = col;

                    switch (ch)
                    {
                        // white spaces
                        case '\u0009': // TAB
                        case '\u000a': // LF - new line
                        case '\u000d': // CR
                        case ' ': // space
                            tokenText = tokenText.Substring(0, tokenText.Length - 1); // remove char from tokenText
                            break;

                        // paranthesis, braces
                        case '(':
                            return Tokens.ParenthesisLeft;
                        case ')':
                            return Tokens.ParenthesisRight;
                        case '{':
                            return Tokens.BraceLeft;
                        case '}':
                            return Tokens.BraceRight;

                        // operators
                        case '+':
                            if (next == '+')
                            {
                                ReadChar();
                                return Tokens.PlusPlus;
                            }
                            return Tokens.Plus;
                        case '-':
                            if (next == '-')
                            {
                                ReadChar();
                                return Tokens.MinusMinus;
                            }
                            return Tokens.Minus;
                        case '*':
                            return Tokens.Multi;
                        case '=':
                            if (next == '=')
                            {
                                ReadChar();
                                return Tokens.EqualsEquals;
                            }
                            return Tokens.Equals;
                        case '<':
                            if (next == '=')
                            {
                                ReadChar();
                                return Tokens.LessOrEquals;
                            }
                            return Tokens.Less;
                        case '>':
                            if (next == '=')
                            {
                                ReadChar();
                                return Tokens.MoreOrEquals;
                            }
                            return Tokens.More;
                        case '|':
                            if (next == '|')
                            {
                                ReadChar();
                                return Tokens.Or;
                            }
                            errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, line, col);
                            return Tokens.Error;
                        case '&':
                            if (next == '&')
                            {
                                ReadChar();
                                return Tokens.And;
                            }
                            errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, line, col);
                            return Tokens.Error;
                        case '!':
                            if (next == '=')
                            {
                                ReadChar();
                                return Tokens.NotEquals;
                            }
                            return Tokens.Neg;

                        // delimiters
                        case ';':
                            return Tokens.Semicolon;
                        case ',':
                            return Tokens.Comma;
                        case ':':
                            return Tokens.Colon;

                        // comments
                        case '/':
                            if (next == '/')
                            {
                                ReadToEndOfLine();
                                return Tokens.Comment;
                            }
                            if (next == '*')
                            {
                                if (!ReadToEndOfComment())
                                {
                                    errorMsg = "Neukonceny komentar";
                                    return Tokens.Error;
                                }
                                return Tokens.Comment;
                            }
                            errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, line, col);
                            return Tokens.Error;

                        // commands, variables & numbers & unknown chars
                        default:

                            if (IsCharacter(ch)) // commands, variables
                            {
                                while (IsCharacterOrDigitChar(next))
                                {
                                    ReadChar();
                                }

                                Tokens t;
                                if (!rw.TryGetValue(tokenText, out t))
                                    t = Tokens.Identifier;
                                return t;
                            }
                            else if (IsDigit(ch)) // number
                            {
                                int len;
                                bool numEnd = false, hexa = false;
                                while (!numEnd)
                                {
                                    len = tokenText.Length;

                                    if (!(IsDigit(next) || (hexa && IsHexaDigit(next)))) // it is not digit or hexadigit
                                    {
                                        if (IsCharacter(next))
                                        {
                                            if (!hexa && ((next == 'x') || (next == 'X')) && (len == 1) && (tokenText[0] == '0'))
                                            {
                                                hexa = true;
                                                ReadChar();
                                                continue;
                                            }
                                            // pismeno v cisle znamena syntaktickou chybu
                                            errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", next, line, col + 1);
                                            return Tokens.Error;
                                        }

                                        numEnd = true;
                                    }
                                    else
                                        ReadChar();
                                }

                                char last = tokenText[tokenText.Length - 1];
                                if (!(IsDigit(last) || (hexa && IsHexaDigit(last)))) // tato podminka se vylepsi...
                                {
                                    errorMsg = string.Format("Spatny format cisla '{0}', radek {1}, sloupec {2}", tokenText, line, tokenCol);
                                    return Tokens.Error;
                                }
                                return Tokens.Number;
                            }

                            errorMsg = string.Format("Neznamy znak '{0}', radek {1}, sloupec {2}", ch, line, col);
                            return Tokens.Error;
                    }
                }

                // complete
                file.Close();
                file.Dispose();
                eof = true;
            }
            return Tokens.End;
        }

        #endregion Lexikalni analyza

        #region Syntakticka analyza

        private static Dictionary<string, BaseAstNode> vars;
        private static Dictionary<string, BaseAstNode> fncs;

        private static Dictionary<Tokens, int> pri;

        private static void InitSA()
        {
            vars = new Dictionary<string, BaseAstNode>();
            fncs = new Dictionary<string, BaseAstNode>();
            pri = new Dictionary<Tokens, int>();
            pri.Add(Tokens.Equals, 0);
            pri.Add(Tokens.Plus, 10);
            pri.Add(Tokens.Minus, 10);
            pri.Add(Tokens.Multi, 20);
            pri.Add(Tokens.PlusPlus, 30);
            pri.Add(Tokens.MinusMinus, 30);
            pri.Add(Tokens.EqualsEquals, 40);
            pri.Add(Tokens.Less, 40);
            pri.Add(Tokens.More, 40);
            pri.Add(Tokens.LessOrEquals, 40);
            pri.Add(Tokens.MoreOrEquals, 40);
            pri.Add(Tokens.NotEquals, 40);
            pri.Add(Tokens.Or, 50);
            pri.Add(Tokens.And, 50);
            pri.Add(Tokens.Neg, 60);
        }

        private static VariableAstNode ConvertToVariable(BaseAstNode node)
        {
            return new VariableAstNode
            {
                Token = node.Token,
                TokenText = node.TokenText,
                LineStart = node.LineStart,
                ColStart = node.ColStart,
                LineEnd = node.LineEnd,
                ColEnd = node.ColEnd,
            };
        }

        private static FunctionCallAstNode ConvertToFunctionCall(BaseAstNode node)
        {
            return new FunctionCallAstNode
            {
                Token = node.Token,
                TokenText = node.TokenText,
                LineStart = node.LineStart,
                ColStart = node.ColStart,
                LineEnd = node.LineEnd,
                ColEnd = node.ColEnd,
            };
        }

        private static LabelAstNode ConvertToLabel(BaseAstNode node)
        {
            return new LabelAstNode
            {
                Token = node.Token,
                TokenText = node.TokenText,
                LineStart = node.LineStart,
                ColStart = node.ColStart,
                LineEnd = node.LineEnd,
                ColEnd = node.ColEnd,
            };
        }

        private static int GetOperationPriority(Tokens token)
        {
            int priority;
            if (!pri.TryGetValue(token, out priority))
                priority = 0;
            return priority;
        }

        private static Tokens GetToken()
        {
            Tokens t = Tokens.Comment;
            while (t == Tokens.Comment)
            {
                t = LA();
                if (printLA)
                    Console.WriteLine("{0}: '{1}'", t, tokenText);
            }

            return t;
        }

        private static BaseAstNode GetAstNode(Tokens token)
        {
            BaseAstNode node;

            switch (token)
            {
                case Tokens.FunctionCmd:
                    node = new FunctionAstNode();
                    break;
                case Tokens.IfCmd:
                    node = new IfAstNode();
                    break;
                case Tokens.ForCmd:
                    node = new ForAstNode();
                    break;
                case Tokens.WhileCmd:
                    node = new WhileAstNode();
                    break;
                case Tokens.GotoCmd:
                    node = new GotoAstNode();
                    break;
                case Tokens.ReturnCmd:
                    node = new ReturnAstNode();
                    break;
                case Tokens.Number:
                    node = new NumberAstNode();
                    break;
                case Tokens.BraceLeft:
                    node = new StatementAstNode();
                    break;
                case Tokens.Equals:
                case Tokens.Plus:
                case Tokens.Minus:
                case Tokens.Multi:
                case Tokens.PlusPlus:
                case Tokens.MinusMinus:
                case Tokens.EqualsEquals:
                case Tokens.Less:
                case Tokens.More:
                case Tokens.LessOrEquals:
                case Tokens.MoreOrEquals:
                case Tokens.NotEquals:
                case Tokens.Or:
                case Tokens.And:
                case Tokens.Neg:
                    int priority = GetOperationPriority(token);
                    node = new OperationAstNode { Priority = priority };
                    break;
                default:
                    node = new BaseAstNode();
                    break;
            }

            node.Token = token;
            node.TokenText = tokenText;
            node.LineStart = tokenLine;
            node.ColStart = tokenCol;
            node.LineEnd = line;
            node.ColEnd = col;

            return node;
        }

        private static BaseAstNode actualReadToken = null;
        private static BaseAstNode nextReadToken = null;

        private static BaseAstNode GetAstNode()
        {
            if (actualReadToken == null)
                nextReadToken = GetAstNode(GetToken());
            actualReadToken = nextReadToken;
            if (actualReadToken.Token != Tokens.End)
                nextReadToken = GetAstNode(GetToken());
            return actualReadToken;
        }

        #region SA - global variables declaration

        private static BaseAstNode GetVariables()
        {
            BaseAstNode node = GetAstNode();
            while ((node.Token != Tokens.Semicolon) && (node.Token != Tokens.Error))
            {
                if (node.Token != Tokens.Identifier)
                {
                    errorMsg = string.Format("Je ocekavan identifikator promenne, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    continue;
                }

                if (vars.Keys.Contains(node.TokenText))
                {
                    errorMsg = string.Format("Promenna '{0}' jiz byla definovana, radek {1}, sloupec {2}", node.TokenText, node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    continue;
                }

                BaseAstNode var = node;

                node = GetAstNode();

                BaseAstNode expr = null;
                if (node.Token == Tokens.Equals)
                {
                    node = GetExprAST(out expr);
                    if (node.Token == Tokens.Error)
                        return node;

                    node = GetAstNode();
                }

                vars.Add(var.TokenText, expr);

                switch (node.Token)
                {
                    case Tokens.Comma:
                        node = GetAstNode();
                        break;

                    case Tokens.Semicolon:
                        break;

                    default:
                        errorMsg = string.Format("Je ocekavan znak oddeleni ',' nebo ';' nebo znak prirazeni '=', radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        node = new BaseAstNode { Token = Tokens.Error };
                        break;
                }
            }

            return node;
        }

        #endregion SA - global variables declaration

        #region SA - function

        private static BaseAstNode GetFunctionAST(FunctionAstNode fnc)
        {
            if (fnc == null)
            {
                errorMsg = "Chybne volana funkce 'GetFunctionNode(FunctionAstNode fnc)', parametr 'fnc' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode node = GetAstNode();
            if (node.Token != Tokens.Identifier)
            {
                errorMsg = string.Format("Je ocekavan identifikator funkce, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                return new BaseAstNode { Token = Tokens.Error };
            }
            if (fncs.Keys.Contains(node.TokenText))
            {
                errorMsg = string.Format("Funkce '{0}' jiz byla definovana, radek {1}, sloupec {2}", node.TokenText, node.LineStart, node.ColStart);
                return new BaseAstNode { Token = Tokens.Error };
            }
            fncs.Add(node.TokenText, fnc);
            fnc.Name = node;

            node = GetAstNode();
            if (node.Token != Tokens.ParenthesisLeft)
            {
                errorMsg = string.Format("Je ocekavana leva zavorka '(', radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                return new BaseAstNode { Token = Tokens.Error };
            }

            node = GetAstNode();
            if (node.Token != Tokens.ParenthesisRight)
            {
                errorMsg = string.Format("Je ocekavana prava zavorka ')', radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                return new BaseAstNode { Token = Tokens.Error };
            }

            node = GetAstNode();
            if (node.Token != Tokens.BraceLeft)
            {
                errorMsg = string.Format("Je ocekavana leva slozena zavorka '{', radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                return new BaseAstNode { Token = Tokens.Error };
            }

            node = GetStatementAST(node as StatementAstNode);
            if (node.Token == Tokens.Error)
                return node;
            fnc.Body = node as StatementAstNode;

            return fnc;
        }

        #endregion SA - function

        #region SA - statement

        private static BaseAstNode GetStatementAST(StatementAstNode st)
        {
            if (st == null)
            {
                errorMsg = "Chybne volana funkce 'GetFunctionNode(FunctionAstNode fnc)', parametr 'fnc' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode node = new BaseAstNode { Token = Tokens.Comment };
            while ((node.Token != Tokens.BraceRight) && (node.Token != Tokens.Error))
            {
                node = GetCommandAST();

                switch (node.Token)
                {
                    case Tokens.End:
                        errorMsg = string.Format("Konec programu, blok neni korektne ukoncen, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        node = new BaseAstNode { Token = Tokens.Error };
                        break;

                    case Tokens.BraceLeft:
                        node = GetStatementAST(st); // it is inner statement -> don't care -> just continue with current list
                        break;
                }

                if (node.Token == Tokens.Error)
                    return node;

                if (node.Token == Tokens.BraceRight)
                    break;

                st.Commands.Add(node);
            }

            return st;
        }

        #endregion SA - statement

        #region SA - command

        private static BaseAstNode GetCommandAST()
        {
            bool semicolon = false;

            BaseAstNode node = GetAstNode();

            switch (node.Token)
            {
                case Tokens.End:
                    errorMsg = string.Format("Konec programu, blok neni korektne ukoncen, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    break;

                case Tokens.IfCmd:
                    node = GetIfAST(node as IfAstNode);
                    break;

                case Tokens.ForCmd:
                    node = GetForAST(node as ForAstNode);
                    break;

                case Tokens.WhileCmd:
                    node = GetWhileAST(node as WhileAstNode);
                    break;

                case Tokens.GotoCmd:
                    node = GetGotoAST(node as GotoAstNode);
                    semicolon = true;
                    break;

                case Tokens.ReturnCmd:
                    node = GetReturnAST(node as ReturnAstNode);
                    semicolon = true;
                    break;

                case Tokens.BraceLeft: // it is inner statement -> let caller decide what to do
                case Tokens.BraceRight: // it is end of statement -> let caller decide what to do
                    break;

                case Tokens.Identifier:
                    switch (nextReadToken.Token)
                    {
                        case Tokens.ParenthesisLeft:
                            node = GetFunctionCallAST(ConvertToFunctionCall(node));
                            semicolon = true;
                            break;

                        case Tokens.Equals:
                            node = GetAssignmentAST(ConvertToVariable(node));
                            semicolon = true;
                            break;

                        case Tokens.PlusPlus:
                        case Tokens.MinusMinus:
                            node = GetUnaryOpAST(ConvertToVariable(node));
                            semicolon = true;
                            break;

                        case Tokens.Colon:
                            node = GetLabelAST(ConvertToLabel(node));
                            break;

                        default:
                            errorMsg = string.Format("Je ocekavan znak prirazeni '=', radek {0}, sloupec {1}", node.LineEnd, node.ColEnd);
                            node = new BaseAstNode { Token = Tokens.Error };
                            break;
                    }
                    break;

                case Tokens.PlusPlus:
                case Tokens.MinusMinus:
                    node = GetUnaryOpAST(node as OperationAstNode);
                    semicolon = true;
                    break;

                case Tokens.VarCmd:
                    errorMsg = string.Format("Lokalni promenne nejsou povoleny, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    break;

                case Tokens.FunctionCmd:
                    errorMsg = string.Format("Vnorene funkce nejsou povoleny, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    break;

                default:
                    errorMsg = string.Format("Je ocekavan prikaz, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    break;
            }

            if (semicolon)
            {
                BaseAstNode sc = GetAstNode();
                if (sc.Token != Tokens.Semicolon)
                {
                    errorMsg = string.Format("Je ocekavan ';', radek {0}, sloupec {1}", sc.LineEnd, sc.ColEnd);
                    return new BaseAstNode { Token = Tokens.Error };
                }
            }

            return node;
        }

        #endregion SA - command

        #region SA - command or statement

        private static BaseAstNode CommandOrStatement()
        {
            BaseAstNode tmp;
            if (nextReadToken.Token == Tokens.BraceLeft)
            {
                BaseAstNode st = GetAstNode();
                tmp = GetStatementAST(st as StatementAstNode);
                if (tmp.Token == Tokens.Error)
                    return tmp;
                return st;
            }

            tmp = GetCommandAST();
            return tmp;
        }

        #endregion SA - command or statement

        #region SA - if command

        private static BaseAstNode GetIfAST(IfAstNode cmd)
        {
            if (cmd == null)
            {
                errorMsg = "Chybne volana funkce 'GetIfAST(IfAstNode cmd)', parametr 'cmd' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode cond;
            BaseAstNode tmp = GetCondAST(out cond);
            if (tmp.Token == Tokens.Error)
                return tmp;
            cmd.Condition = cond;

            BaseAstNode st = CommandOrStatement();
            if (st.Token == Tokens.Error)
                return st;
            cmd.IfBody = st;

            if (nextReadToken.Token == Tokens.ElseCmd)
            {
                tmp = GetAstNode();
                st = CommandOrStatement();
                if (st.Token == Tokens.Error)
                    return st;
                cmd.ElseBody = st;
            }

            return cmd;
        }

        #endregion SA - if command

        #region SA - for command

        private static BaseAstNode AssignmentOrUnaryOrFnc()
        {
            BaseAstNode node = GetAstNode();

            switch (node.Token)
            {
                case Tokens.End:
                    errorMsg = string.Format("Konec programu, blok neni korektne ukoncen, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    break;

                case Tokens.Identifier:
                    switch (nextReadToken.Token)
                    {
                        case Tokens.ParenthesisLeft:
                            node = GetFunctionCallAST(ConvertToFunctionCall(node));
                            break;

                        case Tokens.Equals:
                            node = GetAssignmentAST(ConvertToVariable(node));
                            break;

                        case Tokens.PlusPlus:
                        case Tokens.MinusMinus:
                            node = GetUnaryOpAST(ConvertToVariable(node));
                            break;

                        default:
                            errorMsg = string.Format("Je ocekavan znak prirazeni '=', radek {0}, sloupec {1}", node.LineEnd, node.ColEnd);
                            node = new BaseAstNode { Token = Tokens.Error };
                            break;
                    }
                    break;

                case Tokens.PlusPlus:
                case Tokens.MinusMinus:
                    node = GetUnaryOpAST(node as OperationAstNode);
                    break;

                default:
                    errorMsg = string.Format("Je ocekavan prikaz, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    break;
            }

            return node;
        }

        private static BaseAstNode GetForAST(ForAstNode cmd)
        {
            if (cmd == null)
            {
                errorMsg = "Chybne volana funkce 'GetForAST(ForAstNode cmd)', parametr 'cmd' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            // '('
            BaseAstNode pl = GetAstNode();
            if (pl.Token != Tokens.ParenthesisLeft)
            {
                errorMsg = string.Format("Je ocekavan '(', radek {0}, sloupec {1}", pl.LineEnd, pl.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }

            // init command
            if (nextReadToken.Token != Tokens.Semicolon)
            {
                BaseAstNode init = AssignmentOrUnaryOrFnc();
                if (init.Token == Tokens.Error)
                    return init;
                cmd.Init = init;
            }

            // ';'
            BaseAstNode sc = GetAstNode();
            if (sc.Token != Tokens.Semicolon)
            {
                errorMsg = string.Format("Je ocekavan ';', radek {0}, sloupec {1}", sc.LineEnd, sc.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }

            // condition
            BaseAstNode cond;
            BaseAstNode tmp = GetCondAST(out cond);
            if (tmp.Token == Tokens.Error)
                return tmp;
            cmd.Condition = cond;

            // ';'
            sc = GetAstNode();
            if (sc.Token != Tokens.Semicolon)
            {
                errorMsg = string.Format("Je ocekavan ';', radek {0}, sloupec {1}", sc.LineEnd, sc.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }

            // close command
            if (nextReadToken.Token != Tokens.ParenthesisRight)
            {
                BaseAstNode close = AssignmentOrUnaryOrFnc();
                if (close.Token == Tokens.Error)
                    return close;
                cmd.Close = close;
            }

            // ')'
            BaseAstNode pr = GetAstNode();
            if (pr.Token != Tokens.ParenthesisRight)
            {
                errorMsg = string.Format("Je ocekavan ')', radek {0}, sloupec {1}", pr.LineEnd, pr.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }

            // body
            BaseAstNode st = CommandOrStatement();
            if (st.Token == Tokens.Error)
                return st;
            cmd.ForBody = st;

            return cmd;
        }

        #endregion SA - for command

        #region SA - while command

        private static BaseAstNode GetWhileAST(WhileAstNode cmd)
        {
            if (cmd == null)
            {
                errorMsg = "Chybne volana funkce 'GetWhileAST(WhileAstNode cmd)', parametr 'cmd' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode cond;
            BaseAstNode tmp = GetCondAST(out cond);
            if (tmp.Token == Tokens.Error)
                return tmp;
            cmd.Condition = cond;

            BaseAstNode st = CommandOrStatement();
            if (st.Token == Tokens.Error)
                return st;
            cmd.WhileBody = st;

            return cmd;
        }

        #endregion SA - while command

        #region SA - goto command

        private static BaseAstNode GetGotoAST(GotoAstNode cmd)
        {
            if (cmd == null)
            {
                errorMsg = "Chybne volana funkce 'GetGotoAST(GotoAstNode cmd)', parametr 'cmd' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode label = GetAstNode();
            if (label.Token != Tokens.Identifier)
            {
                errorMsg = string.Format("Je ocekavano navesti, radek {0}, sloupec {1}", label.LineEnd, label.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }
            cmd.Label = ConvertToLabel(label);

            return cmd;
        }

        #endregion SA - goto command

        #region SA - label

        private static BaseAstNode GetLabelAST(LabelAstNode label)
        {
            if (label == null)
            {
                errorMsg = "Chybne volana funkce 'GetLabelAST(LabelAstNode label)', parametr 'label' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode colon = GetAstNode();
            if (colon.Token != Tokens.Colon)
            {
                errorMsg = string.Format("Je ocekavana ':', radek {0}, sloupec {1}", colon.LineEnd, colon.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }

            return label;
        }

        #endregion SA - label

        #region SA - return command

        private static BaseAstNode GetReturnAST(ReturnAstNode cmd)
        {
            if (cmd == null)
            {
                errorMsg = "Chybne volana funkce 'GetReturnAST(ReturnAstNode cmd)', parametr 'cmd' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode expr;
            BaseAstNode tmp = GetExprAST(out expr);
            if (tmp.Token == Tokens.Error)
                return tmp;
            cmd.Return = expr;

            return cmd;
        }

        #endregion SA - return command

        #region SA - function call

        private static BaseAstNode GetFunctionCallAST(FunctionCallAstNode cmd)
        {
            if (cmd == null)
            {
                errorMsg = "Chybne volana funkce 'GetFunctionCallAST(FunctionCallAstNode cmd)', parametr 'cmd' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode pl = GetAstNode();
            if (pl.Token != Tokens.ParenthesisLeft)
            {
                errorMsg = string.Format("Je ocekavan '(', radek {0}, sloupec {1}", pl.LineEnd, pl.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode pr = GetAstNode();
            if (pr.Token != Tokens.ParenthesisRight)
            {
                errorMsg = string.Format("Je ocekavan ')', radek {0}, sloupec {1}", pr.LineEnd, pr.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }

            return cmd;
        }

        #endregion SA - function call

        #region SA - assignemt

        private static BaseAstNode GetAssignmentAST(VariableAstNode var)
        {
            if (var == null)
            {
                errorMsg = "Chybne volana funkce 'GetAssignmentAST(VariableAstNode var)', parametr 'var' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            OperationAstNode cmd = GetAstNode() as OperationAstNode;
            if ((cmd == null) || (cmd.Token != Tokens.Equals))
            {
                errorMsg = string.Format("Je ocekavan operator '=', radek {0}, sloupec {1}", cmd.LineEnd, cmd.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }
            cmd.Left = ConvertToVariable(var);

            BaseAstNode expr;
            BaseAstNode tmp = GetExprAST(out expr);
            if (tmp.Token == Tokens.Error)
                return tmp;
            cmd.Right = expr;

            return cmd;
        }

        #endregion SA - assignment

        #region SA - unary operation ++ --

        private static BaseAstNode GetUnaryOpAST(VariableAstNode var)
        {
            if (var == null)
            {
                errorMsg = "Chybne volana funkce 'GetUnaryOpAST(VariableAstNode var)', parametr 'var' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            OperationAstNode cmd = GetAstNode() as OperationAstNode;
            if ((cmd == null) || ((cmd.Token != Tokens.PlusPlus) && (cmd.Token != Tokens.MinusMinus)))
            {
                errorMsg = string.Format("Je ocekavan operator '++' nebo '--', radek {0}, sloupec {1}", cmd.LineEnd, cmd.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }
            cmd.Left = ConvertToVariable(var);

            return cmd;
        }

        private static BaseAstNode GetUnaryOpAST(OperationAstNode cmd)
        {
            if (cmd == null)
            {
                errorMsg = "Chybne volana funkce 'GetUnaryOpAST(OperationAstNode cmd)', parametr 'cmd' je null";
                return new BaseAstNode { Token = Tokens.Error };
            }

            BaseAstNode var = GetAstNode();
            if (var.Token != Tokens.Identifier)
            {
                errorMsg = string.Format("Je ocekavano navesti, radek {0}, sloupec {1}", var.LineEnd, var.ColEnd);
                return new BaseAstNode { Token = Tokens.Error };
            }
            cmd.Right = ConvertToVariable(var);

            return cmd;
        }

        #endregion SA - unary operation ++ --

        #region SA - expression

        private static bool TryParseNumber(string str, out int number)
        {
            number = 0;
            if (str.StartsWith("0x"))
            {
                try
                {
                    Convert.ToInt32(str.Substring(2), 16);
                }
                catch
                {
                    return false;
                }
                return true;
            }
            return int.TryParse(str, out number);
        }

        private static bool GetOperandNode(out BaseAstNode node)
        {
            OperationAstNode nodeO;
            node = GetAstNode();
            switch (node.Token)
            {
                case Tokens.End:
                    errorMsg = string.Format("Konec programu, vyraz neni korektne ukoncen, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    return false;

                case Tokens.Identifier:
                    node = ConvertToVariable(node);
                    if ((nextReadToken.Token == Tokens.PlusPlus) || (nextReadToken.Token == Tokens.MinusMinus))
                    {
                        nodeO = (OperationAstNode)GetAstNode();
                        if (nodeO == null)
                        {
                            errorMsg = string.Format("Nespravny AST datovy typ operatoru, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                            node = new BaseAstNode { Token = Tokens.Error };
                            return false;
                        }
                        nodeO.Left = node;
                        node = nodeO;
                    }
                    return true;

                case Tokens.Number:
                    int number;
                    if (!(node is NumberAstNode) || !TryParseNumber(node.TokenText, out number))
                    {
                        errorMsg = string.Format("Nespravny format cisla, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        node = new BaseAstNode { Token = Tokens.Error };
                        return false;
                    }
                    ((NumberAstNode)node).Number = number;
                    return true;

                case Tokens.Plus:
                case Tokens.Minus:
                    BaseAstNode sgn = node;
                    int sign = 1;
                    if (node.Token == Tokens.Minus)
                        sign = -1;
                    node = GetAstNode();
                    if (!(node is NumberAstNode) || !TryParseNumber(node.TokenText, out number))
                    {
                        errorMsg = string.Format("Nespravny format cisla, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        node = new BaseAstNode { Token = Tokens.Error };
                        return false;
                    }
                    node.TokenText = sgn.TokenText + node.TokenText;
                    ((NumberAstNode)node).Number = sign * number;
                    return true;

                case Tokens.PlusPlus:
                case Tokens.MinusMinus:
                    nodeO = (OperationAstNode)node;
                    if (nodeO == null)
                    {
                        errorMsg = string.Format("Nespravny AST datovy typ operatoru, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        node = new BaseAstNode { Token = Tokens.Error };
                        return false;
                    }
                    node = GetAstNode();
                    if (node.Token != Tokens.Identifier)
                    {
                        errorMsg = string.Format("Je ocekavan identifikator promenne, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        node = new BaseAstNode { Token = Tokens.Error };
                        return false;
                    }
                    nodeO.Right = ConvertToVariable(node);
                    node = nodeO;
                    return true;

                default:
                    return false;
            }
        }

        private static bool GetExprBinaryOperationNode(out BaseAstNode node)
        {
            node = GetAstNode();
            switch (node.Token)
            {
                case Tokens.End:
                    errorMsg = string.Format("Konec programu, vyraz neni korektne ukoncen, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    return false;

                case Tokens.Plus:
                case Tokens.Minus:
                case Tokens.Multi:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsExprToken(Tokens token)
        {
            switch (token)
            {
                case Tokens.Identifier:
                case Tokens.Number:

                case Tokens.ParenthesisLeft:
                case Tokens.ParenthesisRight:

                case Tokens.Plus:
                case Tokens.Minus:
                case Tokens.Multi:
                case Tokens.PlusPlus:
                case Tokens.MinusMinus:
                    return true;

                default:
                    return false;
            }
        }

        private static BaseAstNode GetExprAST(out BaseAstNode expr)
        {
            return GetSubExprAST(out expr, 0);
        }

        private static BaseAstNode GetSubExprAST(out BaseAstNode expr, int level)
        {
            expr = null;

            BaseAstNode node = null;
            List<BaseAstNode> nodes = new List<BaseAstNode>();

            // number or identifier or left parenthesis
            if (!GetOperandNode(out node))
            {
                switch (node.Token)
                {
                    case Tokens.ParenthesisLeft:
                        BaseAstNode nodePR = GetSubExprAST(out node, level + 1);
                        if (nodePR.Token != Tokens.ParenthesisRight)
                        {
                            errorMsg = string.Format("Vyraz neni korektne ukoncen pravou zavorkou, radek {0}, sloupec {1}", nodePR.LineStart, nodePR.ColStart);
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        break;

                    case Tokens.ParenthesisRight:
                        errorMsg = string.Format("Chybna prava zavorka, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        return new BaseAstNode { Token = Tokens.Error };

                    default:
                        errorMsg = string.Format("Prazdny vyraz, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        return new BaseAstNode { Token = Tokens.Error };
                }
            }

            nodes.Add(node);

            while (IsExprToken(nextReadToken.Token))
            {
                if (!GetExprBinaryOperationNode(out node))
                {
                    switch (node.Token)
                    {
                        case Tokens.Identifier:
                        case Tokens.Number:
                        case Tokens.ParenthesisLeft:
                            errorMsg = string.Format("Nespravne formatovany vyraz, je ocekavan operator, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                            return new BaseAstNode { Token = Tokens.Error };
                    }
                    if (node.Token == Tokens.ParenthesisRight)
                    {
                        if (level == 0)
                        {
                            errorMsg = string.Format("Chybna prava zavorka, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        break;
                    }
                    continue;
                }
                if ((node.Token == Tokens.Multi) && (nodes[nodes.Count - 1].Token != Tokens.Number))
                {
                    errorMsg = string.Format("Pred nasobenim muze byt pouze cislo, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    return new BaseAstNode { Token = Tokens.Error };
                }
                nodes.Add(node);

                if (!GetOperandNode(out node))
                {
                    switch (node.Token)
                    {
                        case Tokens.ParenthesisLeft:
                            BaseAstNode nodePR = GetSubExprAST(out node, level + 1);
                            if (nodePR.Token != Tokens.ParenthesisRight)
                            {
                                errorMsg = string.Format("Vyraz neni korektne ukoncen pravou zavorkou, radek {0}, sloupec {1}", nodePR.LineStart, nodePR.ColStart);
                                return new BaseAstNode { Token = Tokens.Error };
                            }
                            break;

                        default:
                            errorMsg = string.Format("Nespravne formatovany vyraz, je ocekavan cislo, promenna nebo leva zavorka, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                            return new BaseAstNode { Token = Tokens.Error };
                    }
                }
                nodes.Add(node);
            }

            if (nodes.Count == 0)
            {
                errorMsg = string.Format("Nespravne formatovany vyraz, je ocekavan cislo, promenna nebo leva zavorka, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                return new BaseAstNode { Token = Tokens.Error };
            }

            if (nodes.Count < 2) // number or variable only
            {
                expr = nodes[0];
            }
            else
            {
                // all *
                int i = 0;
                while (i < nodes.Count)
                {
                    OperationAstNode op = nodes[i] as OperationAstNode;
                    if ((op != null) && (op.Token == Tokens.Multi))
                    {
                        if ((i < 1) || (i >= (nodes.Count - 1)))
                        {
                            errorMsg = "Nespravne formatovany vyraz...";
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        op.Right = nodes[i + 1];
                        op.Left = nodes[i - 1];
                        nodes.RemoveAt(i + 1);
                        nodes.RemoveAt(i - 1);
                    }
                    i++;
                }

                if (nodes.Count < 2)
                {
                    expr = nodes[0];
                }
                else
                {
                    // all + and -
                    i = 1;
                    BaseAstNode left = nodes[0];
                    while (i < nodes.Count)
                    {
                        if (i >= (nodes.Count - 1))
                        {
                            errorMsg = "Nespravne formatovany vyraz...";
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        OperationAstNode op = nodes[i] as OperationAstNode;
                        if (op == null)
                        {
                            errorMsg = "Nespravne formatovany vyraz...";
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        op.Left = left;
                        op.Right = nodes[i + 1];
                        left = op;
                        i += 2;
                    }
                    expr = left;
                }
            }

            return node;
        }

        #endregion SA - expression

        #region SA - condition

        private static bool GetCondBinaryOperationNode(out BaseAstNode node)
        {
            node = GetAstNode();
            switch (node.Token)
            {
                case Tokens.End:
                    errorMsg = string.Format("Konec programu, vyraz neni korektne ukoncen, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    node = new BaseAstNode { Token = Tokens.Error };
                    return false;

                case Tokens.Plus:
                case Tokens.Minus:
                case Tokens.Multi:

                case Tokens.EqualsEquals:
                case Tokens.Less:
                case Tokens.More:
                case Tokens.LessOrEquals:
                case Tokens.MoreOrEquals:
                case Tokens.NotEquals:

                case Tokens.Or:
                case Tokens.And:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsCondToken(Tokens token)
        {
            switch (token)
            {
                case Tokens.Identifier:
                case Tokens.Number:

                case Tokens.ParenthesisLeft:
                case Tokens.ParenthesisRight:

                case Tokens.Plus:
                case Tokens.Minus:
                case Tokens.Multi:
                case Tokens.PlusPlus:
                case Tokens.MinusMinus:

                case Tokens.EqualsEquals:
                case Tokens.Less:
                case Tokens.More:
                case Tokens.LessOrEquals:
                case Tokens.MoreOrEquals:
                case Tokens.NotEquals:

                case Tokens.Or:
                case Tokens.And:
                    return true;

                default:
                    return false;
            }
        }

        private static BaseAstNode GetCondAST(out BaseAstNode expr)
        {
            return GetSubCondAST(out expr, 0);
        }

        private static BaseAstNode GetSubCondAST(out BaseAstNode expr, int level)
        {
            expr = null;

            BaseAstNode node = null;
            List<BaseAstNode> nodes = new List<BaseAstNode>();

            // number or identifier or left parenthesis
            if (!GetOperandNode(out node))
            {
                switch (node.Token)
                {
                    case Tokens.ParenthesisLeft:
                        BaseAstNode nodePR = GetSubCondAST(out node, level + 1);
                        if (nodePR.Token != Tokens.ParenthesisRight)
                        {
                            errorMsg = string.Format("Vyraz neni korektne ukoncen pravou zavorkou, radek {0}, sloupec {1}", nodePR.LineStart, nodePR.ColStart);
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        break;

                    case Tokens.ParenthesisRight:
                        errorMsg = string.Format("Chybna prava zavorka, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        return new BaseAstNode { Token = Tokens.Error };

                    default:
                        errorMsg = string.Format("Prazdny vyraz, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        return new BaseAstNode { Token = Tokens.Error };
                }
            }

            nodes.Add(node);

            while (IsCondToken(nextReadToken.Token))
            {
                if (!GetCondBinaryOperationNode(out node))
                {
                    switch (node.Token)
                    {
                        case Tokens.Identifier:
                        case Tokens.Number:
                        case Tokens.ParenthesisLeft:
                            errorMsg = string.Format("Nespravne formatovany vyraz, je ocekavan operator, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                            return new BaseAstNode { Token = Tokens.Error };
                    }
                    if (node.Token == Tokens.ParenthesisRight)
                    {
                        if (level == 0)
                        {
                            errorMsg = string.Format("Chybna prava zavorka, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        break;
                    }
                    continue;
                }
                if ((node.Token == Tokens.Multi) && (nodes[nodes.Count - 1].Token != Tokens.Number))
                {
                    errorMsg = string.Format("Pred nasobenim muze byt pouze cislo, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                    return new BaseAstNode { Token = Tokens.Error };
                }
                nodes.Add(node);

                if (!GetOperandNode(out node))
                {
                    switch (node.Token)
                    {
                        case Tokens.ParenthesisLeft:
                            BaseAstNode nodePR = GetSubCondAST(out node, level + 1);
                            if (nodePR.Token != Tokens.ParenthesisRight)
                            {
                                errorMsg = string.Format("Vyraz neni korektne ukoncen pravou zavorkou, radek {0}, sloupec {1}", nodePR.LineStart, nodePR.ColStart);
                                return new BaseAstNode { Token = Tokens.Error };
                            }
                            break;

                        default:
                            errorMsg = string.Format("Nespravne formatovany vyraz, je ocekavan cislo, promenna nebo leva zavorka, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                            return new BaseAstNode { Token = Tokens.Error };
                    }
                }
                nodes.Add(node);
            }

            if (nodes.Count == 0)
            {
                errorMsg = string.Format("Nespravne formatovany vyraz, je ocekavan cislo, promenna nebo leva zavorka, radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                return new BaseAstNode { Token = Tokens.Error };
            }

            if (nodes.Count < 2) // number or variable only
            {
                expr = nodes[0];
            }
            else
            {
                // all *
                int i = 0;
                while (i < nodes.Count)
                {
                    OperationAstNode op = nodes[i] as OperationAstNode;
                    if ((op != null) && (op.Token == Tokens.Multi))
                    {
                        if ((i < 1) || (i >= (nodes.Count - 1)))
                        {
                            errorMsg = "Nespravne formatovany vyraz...";
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        op.Right = nodes[i + 1];
                        op.Left = nodes[i - 1];
                        nodes.RemoveAt(i + 1);
                        nodes.RemoveAt(i - 1);
                    }
                    i++;
                }

                if (nodes.Count < 2)
                {
                    expr = nodes[0];
                }
                else
                {
                    // all + and -
                    i = 1;
                    BaseAstNode left = nodes[0];
                    while (i < nodes.Count)
                    {
                        if (i >= (nodes.Count - 1))
                        {
                            errorMsg = "Nespravne formatovany vyraz...";
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        OperationAstNode op = nodes[i] as OperationAstNode;
                        if (op == null)
                        {
                            errorMsg = "Nespravne formatovany vyraz...";
                            return new BaseAstNode { Token = Tokens.Error };
                        }
                        op.Left = left;
                        op.Right = nodes[i + 1];
                        left = op;
                        i += 2;
                    }
                    expr = left;
                }
            }

            return node;
        }

        #endregion SA - condition

        private static void SA()
        {
            InitSA();

            BaseAstNode node = new BaseAstNode { Token = Tokens.Comment };
            while ((node.Token != Tokens.End) && (node.Token != Tokens.Error))
            {
                node = GetAstNode();

                switch (node.Token)
                {
                    case Tokens.VarCmd: // global variable declaration
                        node = GetVariables();
                        break;


                    case Tokens.FunctionCmd: // function declaration
                        node = GetFunctionAST(node as FunctionAstNode);
                        break;


                    case Tokens.End: // end of program
                        break;


                    default:
                        errorMsg = string.Format("Je ocekavano klicove slovo 'var' nebo 'function', radek {0}, sloupec {1}", node.LineStart, node.ColStart);
                        node = new BaseAstNode { Token = Tokens.Error };
                        break;
                }
            }

            if (node.Token == Tokens.Error)
                Console.WriteLine("Error: '{0}'", errorMsg);
        }

        #endregion Syntakticka analyza

        static int Main(string[] args)
        {
            //programName = args[0];
            programFile = @"C:\Users\Míša\Documents\Visual Studio 2010\Projects\InterproceduralAnalysis\InterproceduralAnalysis\program.txt";
            //printLA = arg[1];
            printLA = true;

            if (programFile == null)
            {
                Console.WriteLine("Parametr 'programFile' je povinny.");
                // PrintHelp();
                Console.ReadKey();
                return -1;
            }

            if (!File.Exists(programFile))
            {
                Console.WriteLine(string.Format("Soubor programu '{0}' neexistuje.", programFile));
                // PrintHelp();
                Console.ReadKey();
                return -1;
            }

            SA();

            //Console.WriteLine("Interproceduralni analyza programu: start");
            //if (!XXX())
            //{
            //    Console.ReadKey();
            //    return -1;
            //}
            //Console.WriteLine("Interproceduralni analyza programu: konec");

            Console.ReadKey();
            return 0;
        }
    }
}
