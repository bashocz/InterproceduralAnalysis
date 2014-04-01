using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class SyntacticAnalyzer
    {
        // tridni promenne pro syntaktickou analyzu
        private BaseAst actualNode, nextNode;

        private LexicalAnalyzer la;

        public SyntacticAnalyzer(LexicalAnalyzer la)
        {
            this.la = la;
        }

        private ProgramAst program;

        // tridni promenne pro prioritu operatoru
        private const int opMax = 71;
        private IDictionary<TokenTypes, int> op;
        private IDictionary<TokenTypes, int> OP
        {
            get { return op ?? (op = CreateOP()); }
        }

        private IDictionary<TokenTypes, int> CreateOP()
        {
            Dictionary<TokenTypes, int> d = new Dictionary<TokenTypes, int>();
            d.Add(TokenTypes.PlusPlus, 0);
            d.Add(TokenTypes.MinusMinus, 0);
            d.Add(TokenTypes.Neg, 0);
            d.Add(TokenTypes.Multi, 10);
            d.Add(TokenTypes.Plus, 20);
            d.Add(TokenTypes.Minus, 20);
            d.Add(TokenTypes.Less, 30);
            d.Add(TokenTypes.More, 30);
            d.Add(TokenTypes.LessOrEquals, 30);
            d.Add(TokenTypes.MoreOrEquals, 30);
            d.Add(TokenTypes.EqualsEquals, 40);
            d.Add(TokenTypes.NotEquals, 40);
            d.Add(TokenTypes.And, 50);
            d.Add(TokenTypes.Or, 60);
            d.Add(TokenTypes.Equals, 70);
            return d;
        }

        private T ConvertTo<T>(TokenModel token, AstNodeTypes astType) where T : BaseAst, new()
        {
            T n = new T
            {
                IsError = token.IsError,
                ErrorMessage = token.ErrorMessage,
                Token = token.Token,
                TokenText = token.TokenText,
                TokenStartLine = token.TokenStartLine,
                TokenStartColumn = token.TokenStartColumn,
                AstType = astType,
            };

            return n;
        }

        private bool TryParseNumber(string str, out int number)
        {
            number = 0;
            if (str.StartsWith("0x"))
            {
                try
                {
                    number = Convert.ToInt32(str.Substring(2), 16);
                }
                catch
                {
                    return false;
                }
                return true;
            }
            return int.TryParse(str, out number);
        }

        private void SetNumber(NumberAst token)
        {
            // to-do
        }

        private void SetOperatorPriority(OperatorAst token)
        {
            int priority;
            if (!OP.TryGetValue(token.Token, out priority))
                priority = 0;
            token.Priority = priority;
        }

        private BaseAst GetAstNode(TokenModel token)
        {
            switch (token.Token)
            {
                case TokenTypes.FunctionRW:
                    return ConvertTo<FunctionAst>(token, AstNodeTypes.Function);

                case TokenTypes.IfRW:
                    return ConvertTo<IfAst>(token, AstNodeTypes.If);

                case TokenTypes.ForRW:
                    return ConvertTo<ForAst>(token, AstNodeTypes.For);

                case TokenTypes.WhileRW:
                    return ConvertTo<WhileAst>(token, AstNodeTypes.While);

                case TokenTypes.GotoRW:
                    return ConvertTo<GotoAst>(token, AstNodeTypes.Goto);

                case TokenTypes.ReturnRW:
                    return ConvertTo<BaseAst>(token, AstNodeTypes.Return);

                case TokenTypes.Number:
                    NumberAst number = ConvertTo<NumberAst>(token, AstNodeTypes.Number);
                    int num;
                    if (!TryParseNumber(number.TokenText, out num))
                        return BaseAst.GetErrorAstNode(string.Format("Nespravny format cisla, radek {0}, sloupec {1}", number.TokenStartLine, number.TokenStartColumn));
                    number.Number = num;
                    return number;

                case TokenTypes.BraceLeft:
                    return ConvertTo<BlockAst>(token, AstNodeTypes.Block);

                case TokenTypes.Equals:
                case TokenTypes.Plus:
                case TokenTypes.Minus:
                case TokenTypes.Multi:
                case TokenTypes.PlusPlus:
                case TokenTypes.MinusMinus:
                case TokenTypes.EqualsEquals:
                case TokenTypes.Less:
                case TokenTypes.More:
                case TokenTypes.LessOrEquals:
                case TokenTypes.MoreOrEquals:
                case TokenTypes.NotEquals:
                case TokenTypes.Or:
                case TokenTypes.And:
                case TokenTypes.Neg:
                    OperatorAst op = ConvertTo<OperatorAst>(token, AstNodeTypes.Operator);
                    SetOperatorPriority(op);
                    return op;

                case TokenTypes.Error:
                    return ConvertTo<BaseAst>(token, AstNodeTypes.None);

                default:
                    return ConvertTo<BaseAst>(token, AstNodeTypes.Variable);
            }
        }

        private void ReadNextAst()
        {
            la.ReadNextToken();
            actualNode = GetAstNode(la.ActualToken);
            nextNode = GetAstNode(la.NextToken);
        }

        #region SA - global variables declaration

        private BaseAst GetVariables()
        {
            ReadNextAst();
            while ((actualNode.Token != TokenTypes.Semicolon) && !(actualNode.IsError))
            {
                if (actualNode.Token != TokenTypes.Identifier)
                    return BaseAst.GetErrorAstNode(string.Format("Je ocekavan identifikator promenne, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

                if (program.VarsDecl.Keys.Contains(actualNode.TokenText))
                    return BaseAst.GetErrorAstNode(string.Format("Promenna '{0}' jiz byla deklarovana, radek {1}, sloupec {2}", actualNode.TokenText, actualNode.TokenStartLine, actualNode.TokenStartColumn));

                BaseAst var = actualNode;

                BaseAst expr = null;
                if (nextNode.Token == TokenTypes.Equals)
                {
                    ReadNextAst();
                    BaseAst node = GetExprAST(out expr);
                    if (node.IsError)
                        return node;
                }

                program.Vars.Add(var.TokenText);
                program.VarsDecl.Add(var.TokenText, expr);

                ReadNextAst();
                switch (actualNode.Token)
                {
                    case TokenTypes.Comma:
                        ReadNextAst(); // precist oddelovac a pripravit se na dalsi promennou
                        break;

                    case TokenTypes.Semicolon:
                        break;

                    default:
                        return BaseAst.GetErrorAstNode(string.Format("Je ocekavan znak oddeleni ',' nebo ';' nebo znak prirazeni '=', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
                }
            }

            return actualNode;
        }

        #endregion SA - global variables declaration

        #region SA - function

        private BaseAst GetFunctionAST(FunctionAst fnc)
        {
            if (fnc == null)
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetFunctionNode(FunctionAst fnc)', parametr 'fnc' je null");

            ReadNextAst();
            if (actualNode.Token != TokenTypes.Identifier)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan identifikator funkce, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
            if (program.OrigFncs.Keys.Contains(actualNode.TokenText))
                return BaseAst.GetErrorAstNode(string.Format("Funkce '{0}' jiz byla deklarovana, radek {1}, sloupec {2}", actualNode.TokenText, actualNode.TokenStartLine, actualNode.TokenStartColumn));

            program.OrigFncs.Add(actualNode.TokenText, fnc);

            ReadNextAst();
            if (actualNode.Token != TokenTypes.ParenthesisLeft)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavana leva zavorka '(', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            ReadNextAst();
            if (actualNode.Token != TokenTypes.ParenthesisRight)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavana prava zavorka ')', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            ReadNextAst();
            if ((actualNode.Token != TokenTypes.BraceLeft) && !(actualNode is BlockAst))
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavana leva slozena zavorka '{{', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            BaseAst node = GetFncBodyAST(actualNode as BlockAst);
            if (node.IsError)
                return node;
            fnc.Body = node as BlockAst;

            return fnc;
        }

        private BaseAst GetFncBodyAST(BlockAst body)
        {
            if (body == null)
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetFncBodyNode(BlockAst body)', parametr 'body' je null");

            BaseAst node = BaseAst.GetInitLoopAstNode();
            while ((node.Token != TokenTypes.BraceRight) && !(node.IsError))
            {
                node = GetStatementAST();

                switch (node.Token)
                {
                    case TokenTypes.End:
                        return BaseAst.GetErrorAstNode(string.Format("Konec programu, blok neni korektne ukoncen, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));

                    case TokenTypes.BraceLeft:
                        node = GetBlockAST(node as BlockAst);
                        break;
                }

                if (node.IsError)
                    return node;

                if (node.Token == TokenTypes.BraceRight)
                    break;

                body.Statements.Add(node);
            }

            return body;
        }

        #endregion SA - function

        #region SA - block

        private BaseAst GetBlockAST(BlockAst block)
        {
            if (block == null)
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetBlockAST(BlockAst block)', parametr 'block' je null");

            BaseAst node = BaseAst.GetInitLoopAstNode();
            while ((node.Token != TokenTypes.BraceRight) && !(node.IsError))
            {
                node = GetStatementAST();

                switch (node.Token)
                {
                    case TokenTypes.End:
                        return BaseAst.GetErrorAstNode(string.Format("Konec programu, blok neni korektne ukoncen, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));

                    case TokenTypes.BraceLeft:
                        node = GetBlockAST(node as BlockAst);
                        break;
                }

                if (node.IsError)
                    return node;

                if (node.Token == TokenTypes.BraceRight)
                    break;

                block.Statements.Add(node);
            }

            return block;
        }

        #endregion SA - block

        #region SA - statement

        private BaseAst GetStatementAST()
        {
            bool semicolon = false;

            ReadNextAst();

            BaseAst node = null;
            switch (actualNode.Token)
            {
                case TokenTypes.End:
                    return BaseAst.GetErrorAstNode(string.Format("Konec programu, blok neni korektne ukoncen, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

                case TokenTypes.IfRW:
                    node = GetIfAST(actualNode as IfAst);
                    break;

                case TokenTypes.ForRW:
                    node = GetForAST(actualNode as ForAst);
                    break;

                case TokenTypes.WhileRW:
                    node = GetWhileAST(actualNode as WhileAst);
                    break;

                case TokenTypes.GotoRW:
                    node = GetGotoAST(actualNode as GotoAst);
                    semicolon = true;
                    break;

                case TokenTypes.ReturnRW:
                    node = GetReturnAST(actualNode);
                    semicolon = true;
                    break;

                case TokenTypes.BraceLeft: // to je zacatek vnitrniho blocku -> volajici metoda si rozhodne co s tim
                case TokenTypes.BraceRight: // to je konec blocku -> volajici metoda rozhodne co s tim
                    node = actualNode;
                    break;

                case TokenTypes.Identifier:
                    switch (nextNode.Token)
                    {
                        case TokenTypes.ParenthesisLeft:
                            node = GetFunctionCallAST(actualNode);
                            semicolon = true;
                            break;

                        case TokenTypes.Equals:
                            node = GetAssignmentAST(actualNode);
                            semicolon = true;
                            break;

                        case TokenTypes.PlusPlus:
                        case TokenTypes.MinusMinus:
                            node = GetUnaryOpAST(actualNode);
                            semicolon = true;
                            break;

                        case TokenTypes.Colon:
                            node = GetLabelAST(actualNode);
                            break;

                        default:
                            return BaseAst.GetErrorAstNode(string.Format("Je ocekavan znak prirazeni '=', radek {0}, sloupec {1}", nextNode.TokenStartLine, nextNode.TokenStartColumn));
                    }
                    break;

                case TokenTypes.PlusPlus:
                case TokenTypes.MinusMinus:
                    node = GetUnaryOpAST(actualNode as OperatorAst);
                    semicolon = true;
                    break;

                case TokenTypes.VarRW:
                    return BaseAst.GetErrorAstNode(string.Format("Lokalni promenne nejsou povoleny, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

                case TokenTypes.FunctionRW:
                    return BaseAst.GetErrorAstNode(string.Format("Vnorene funkce nejsou povoleny, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

                default:
                    return BaseAst.GetErrorAstNode(string.Format("Je ocekavan prikaz, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
            }

            if (node == null)
                return BaseAst.GetErrorAstNode("Vnitrni chyba funkce 'GetStatementAST'");

            if (node.IsError)
                return node;

            if (semicolon)
            {
                ReadNextAst();
                if (actualNode.Token != TokenTypes.Semicolon)
                    return BaseAst.GetErrorAstNode(string.Format("Je ocekavan ';', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
            }

            return node;
        }

        #endregion SA - statement

        #region SA - statement or block

        private BaseAst StatementOrBlock()
        {
            if (nextNode.Token == TokenTypes.BraceLeft)
            {
                ReadNextAst();
                BaseAst st = actualNode;
                BaseAst tmp = GetBlockAST(st as BlockAst);
                if (tmp.IsError)
                    return tmp;
                return st;
            }

            return GetStatementAST();
        }

        #endregion SA - statement or block

        #region SA - if statement

        private BaseAst GetIfAST(IfAst cmd)
        {
            if (cmd == null)
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetIfAST(IfAst cmd)', parametr 'cmd' je null");

            BaseAst cond;
            BaseAst tmp = GetCondAST(out cond);
            if (tmp.IsError)
                return tmp;
            cmd.Condition = cond;

            BaseAst block = StatementOrBlock();
            if (block.IsError)
                return block;
            cmd.IfBody = block;

            if (nextNode.Token == TokenTypes.ElseRW)
            {
                ReadNextAst();
                block = StatementOrBlock();
                if (block.IsError)
                    return block;
                cmd.ElseBody = block;
            }

            return cmd;
        }

        #endregion SA - if statement

        #region SA - for statement

        private BaseAst AssignmentOrUnaryOrFnc()
        {
            ReadNextAst();

            BaseAst node = actualNode;
            switch (actualNode.Token)
            {
                case TokenTypes.End:
                    return BaseAst.GetErrorAstNode(string.Format("Konec programu, blok neni korektne ukoncen, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

                case TokenTypes.Identifier:
                    switch (nextNode.Token)
                    {
                        case TokenTypes.ParenthesisLeft:
                            node = GetFunctionCallAST(node);
                            break;

                        case TokenTypes.Equals:
                            node = GetAssignmentAST(node);
                            break;

                        case TokenTypes.PlusPlus:
                        case TokenTypes.MinusMinus:
                            node = GetUnaryOpAST(node);
                            break;

                        default:
                            return BaseAst.GetErrorAstNode(string.Format("Je ocekavan znak prirazeni '=', radek {0}, sloupec {1}", nextNode.TokenStartLine, nextNode.TokenStartColumn));
                    }
                    break;

                case TokenTypes.PlusPlus:
                case TokenTypes.MinusMinus:
                    node = GetUnaryOpAST(node as OperatorAst);
                    break;

                default:
                    return BaseAst.GetErrorAstNode(string.Format("Je ocekavan prikaz, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
            }

            return node;
        }

        private BaseAst GetForAST(ForAst cmd)
        {
            if (cmd == null)
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetForAST(ForAst cmd)', parametr 'cmd' je null");

            // '('
            ReadNextAst();
            if (actualNode.Token != TokenTypes.ParenthesisLeft)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan '(', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            // init command
            if (nextNode.Token != TokenTypes.Semicolon)
            {
                BaseAst init = AssignmentOrUnaryOrFnc();
                if (init.IsError)
                    return init;
                cmd.Init = init;
            }

            // ';'
            ReadNextAst();
            if (actualNode.Token != TokenTypes.Semicolon)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan ';', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            // condition
            BaseAst cond;
            BaseAst tmp = GetCondAST(out cond);
            if (tmp.IsError)
                return tmp;
            cmd.Condition = cond;

            // ';'
            ReadNextAst();
            if (actualNode.Token != TokenTypes.Semicolon)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan ';', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            // close command
            if (nextNode.Token != TokenTypes.ParenthesisRight)
            {
                BaseAst close = AssignmentOrUnaryOrFnc();
                if (close.IsError)
                    return close;
                cmd.Close = close;
            }

            // ')'
            ReadNextAst();
            if (actualNode.Token != TokenTypes.ParenthesisRight)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan ')', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            // body
            BaseAst st = StatementOrBlock();
            if (st.IsError)
                return st;
            cmd.ForBody = st;

            return cmd;
        }

        #endregion SA - for statement

        #region SA - while command

        private BaseAst GetWhileAST(WhileAst cmd)
        {
            if (cmd == null)
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetWhileAST(WhileAst cmd)', parametr 'cmd' je null");

            BaseAst cond;
            BaseAst tmp = GetCondAST(out cond);
            if (tmp.IsError)
                return tmp;
            cmd.Condition = cond;

            BaseAst st = StatementOrBlock();
            if (st.IsError)
                return st;
            cmd.WhileBody = st;

            return cmd;
        }

        #endregion SA - while command

        #region SA - goto command

        private BaseAst GetGotoAST(GotoAst cmd)
        {
            if (cmd == null)
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetGotoAST(GotoAst cmd)', parametr 'cmd' je null");

            ReadNextAst();
            if (actualNode.Token != TokenTypes.Identifier)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavano navesti, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            cmd.Label = actualNode.TokenText;

            return cmd;
        }

        #endregion SA - goto command

        #region SA - label

        private BaseAst GetLabelAST(BaseAst label)
        {
            if ((label == null) || (label.AstType != AstNodeTypes.Variable))
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetLabelAST(BaseAst label)', parametr 'label' je null");

            label.AstType = AstNodeTypes.Label;

            ReadNextAst();
            if (actualNode.Token != TokenTypes.Colon)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavana ':', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            return label;
        }

        #endregion SA - label

        #region SA - return command

        private BaseAst GetReturnAST(BaseAst cmd)
        {
            if ((cmd == null) || (cmd.AstType != AstNodeTypes.Return))
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetReturnAST(BaseAst cmd)', parametr 'cmd' je null");

            cmd.AstType = AstNodeTypes.Return;

            // nas return nepodporuje navratove hodnoty (jelikoz to by znamenalo pripustit lokalni promenne)

            //BaseAst expr;
            //BaseAst tmp = GetExprAST(out expr);
            //if (tmp.IsError)
            //    return tmp;
            //cmd.Return = expr;

            return cmd;
        }

        #endregion SA - return command

        #region SA - function call

        private BaseAst GetFunctionCallAST(BaseAst cmd)
        {
            if ((cmd == null) || (cmd.AstType != AstNodeTypes.Variable))
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetFunctionCallAST(BaseAst cmd)', parametr 'cmd' je null");

            cmd.AstType = AstNodeTypes.FunctionCall;

            //if (!program.OrigFncs.Keys.Contains(cmd.TokenText))
            //    return GetErrorAstNode(string.Format("Funkce '{0}' doposud nebyla deklarovana, radek {1}, sloupec {2}", cmd.TokenText, cmd.TokenStartLine, cmd.TokenStartColumn));

            ReadNextAst();
            if (actualNode.Token != TokenTypes.ParenthesisLeft)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan '(', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            ReadNextAst();
            if (actualNode.Token != TokenTypes.ParenthesisRight)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan ')', radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            return cmd;
        }

        #endregion SA - function call

        #region SA - assignemt

        private BaseAst GetAssignmentAST(BaseAst var)
        {
            if ((var == null) || (var.AstType != AstNodeTypes.Variable))
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetAssignmentAST(BaseAst var)', parametr 'var' je null");
            if (!program.VarsDecl.Keys.Contains(var.TokenText))
                return BaseAst.GetErrorAstNode(string.Format("Promenna '{0}' doposud nebyla deklarovana, radek {1}, sloupec {2}", var.TokenText, var.TokenStartLine, var.TokenStartColumn));

            ReadNextAst();
            OperatorAst cmd = actualNode as OperatorAst;
            if ((cmd == null) || (cmd.Token != TokenTypes.Equals))
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan operator '=', radek {0}, sloupec {1}", cmd.TokenStartLine, cmd.TokenStartColumn));

            cmd.Left = var;

            BaseAst expr;
            BaseAst tmp = GetExprAST(out expr);
            if (tmp.IsError)
                return tmp;
            cmd.Right = expr;

            return cmd;
        }

        #endregion SA - assignment

        #region SA - unary operation ++ --

        private BaseAst GetUnaryOpAST(BaseAst var)
        {
            if ((var == null) || (var.AstType != AstNodeTypes.Variable))
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetUnaryOpAST(BaseAst var)', parametr 'var' je null");

            ReadNextAst();
            OperatorAst cmd = actualNode as OperatorAst;
            if ((cmd == null) || ((cmd.Token != TokenTypes.PlusPlus) && (cmd.Token != TokenTypes.MinusMinus)))
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavan operator '++' nebo '--', radek {0}, sloupec {1}", cmd.TokenStartLine, cmd.TokenStartColumn));

            cmd.Left = var;

            return cmd;
        }

        private BaseAst GetUnaryOpAST(OperatorAst cmd)
        {
            if (cmd == null)
                return BaseAst.GetErrorAstNode("Chybne volana funkce 'GetUnaryOpAST(OperatorAst cmd)', parametr 'cmd' je null");

            ReadNextAst();
            if (actualNode.Token != TokenTypes.Identifier)
                return BaseAst.GetErrorAstNode(string.Format("Je ocekavano navesti, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));

            cmd.Right = actualNode;

            return cmd;
        }

        #endregion SA - unary operation ++ --

        #region SA - expression

        private BaseAst GetExprAST(out BaseAst expr)
        {
            return GetSubExprAST(out expr, 0, false);
        }

        private BaseAst GetCondAST(out BaseAst expr)
        {
            return GetSubExprAST(out expr, 0, true);
        }

        private bool GetOperandNode(out BaseAst node)
        {
            OperatorAst nodeO;
            ReadNextAst();
            switch (actualNode.Token)
            {
                case TokenTypes.End:
                    node = BaseAst.GetErrorAstNode(string.Format("Konec programu, vyraz neni korektne ukoncen, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
                    return false;

                case TokenTypes.Identifier:
                    if (!program.VarsDecl.Keys.Contains(actualNode.TokenText))
                    {
                        node = BaseAst.GetErrorAstNode(string.Format("Promenna '{0}' doposud nebyla deklarovana, radek {1}, sloupec {2}", actualNode.TokenText, actualNode.TokenStartLine, actualNode.TokenStartColumn));
                        return false;
                    }
                    node = actualNode;
                    if ((nextNode.Token == TokenTypes.PlusPlus) || (nextNode.Token == TokenTypes.MinusMinus))
                    {
                        ReadNextAst();
                        nodeO = actualNode as OperatorAst;
                        if (nodeO == null)
                        {
                            node = BaseAst.GetErrorAstNode(string.Format("Nespravny AST datovy typ operatoru, radek {0}, sloupec {1}", nodeO.TokenStartLine, nodeO.TokenStartColumn));
                            return false;
                        }
                        nodeO.Left = node;
                        node = nodeO;
                    }
                    return true;

                case TokenTypes.Number:
                    node = actualNode;
                    return true;

                case TokenTypes.Plus:
                case TokenTypes.Minus:
                    BaseAst sgn = actualNode;
                    int sign = 1;
                    if (sgn.Token == TokenTypes.Minus)
                        sign = -1;

                    ReadNextAst();
                    switch (actualNode.Token)
                    {
                        case TokenTypes.VarRW:
                            // to-do
                            node = BaseAst.GetErrorAstNode("Momentalne nepodporujeme '- variable'");
                            return false;

                        case TokenTypes.Number:
                            node = ConvertTo<NumberAst>(sgn, AstNodeTypes.Number);
                            node.TokenText = sgn.TokenText + actualNode.TokenText;
                            ((NumberAst)node).Number = sign * ((NumberAst)actualNode).Number;
                            return true;

                        default:
                            node = BaseAst.GetErrorAstNode(string.Format("Je ocekavan identifikator promenne, radek {0}, sloupec {1}'", actualNode.TokenStartLine, actualNode.TokenStartColumn));
                            return false;
                    }

                case TokenTypes.PlusPlus:
                case TokenTypes.MinusMinus:
                    nodeO = (OperatorAst)actualNode;
                    if (nodeO == null)
                    {
                        node = BaseAst.GetErrorAstNode(string.Format("Nespravny AST datovy typ operatoru, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
                        return false;
                    }
                    ReadNextAst();
                    if (actualNode.Token != TokenTypes.Identifier)
                    {
                        node = BaseAst.GetErrorAstNode(string.Format("Je ocekavan identifikator promenne, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
                        return false;
                    }
                    if (!program.VarsDecl.Keys.Contains(actualNode.TokenText))
                    {
                        node = BaseAst.GetErrorAstNode(string.Format("Promenna '{0}' doposud nebyla deklarovana, radek {1}, sloupec {2}", actualNode.TokenText, actualNode.TokenStartLine, actualNode.TokenStartColumn));
                        return false;
                    }
                    nodeO.Right = actualNode;
                    node = nodeO;
                    return true;

                default:
                    node = actualNode;
                    return false;
            }
        }

        private bool GetExprBinaryOperationNode(out BaseAst node, bool isCond)
        {
            ReadNextAst();
            node = actualNode;
            switch (node.Token)
            {
                case TokenTypes.End:
                    node = BaseAst.GetErrorAstNode(string.Format("Konec programu, vyraz neni korektne ukoncen, radek {0}, sloupec {1}", actualNode.TokenStartLine, actualNode.TokenStartColumn));
                    return false;

                case TokenTypes.Plus:
                case TokenTypes.Minus:
                case TokenTypes.Multi:
                    return true;

                case TokenTypes.EqualsEquals:
                case TokenTypes.Less:
                case TokenTypes.More:
                case TokenTypes.LessOrEquals:
                case TokenTypes.MoreOrEquals:
                case TokenTypes.NotEquals:

                case TokenTypes.Or:
                case TokenTypes.And:
                    return isCond;

                default:
                    return false;
            }
        }

        private bool IsExprToken(TokenTypes token)
        {
            switch (token)
            {
                case TokenTypes.Identifier:
                case TokenTypes.Number:

                case TokenTypes.ParenthesisLeft:
                case TokenTypes.ParenthesisRight:

                case TokenTypes.Plus:
                case TokenTypes.Minus:
                case TokenTypes.Multi:
                case TokenTypes.PlusPlus:
                case TokenTypes.MinusMinus:
                    return true;

                default:
                    return false;
            }
        }

        private bool IsCondToken(TokenTypes token)
        {
            switch (token)
            {
                case TokenTypes.Identifier:
                case TokenTypes.Number:

                case TokenTypes.ParenthesisLeft:
                case TokenTypes.ParenthesisRight:

                case TokenTypes.Plus:
                case TokenTypes.Minus:
                case TokenTypes.Multi:
                case TokenTypes.PlusPlus:
                case TokenTypes.MinusMinus:

                case TokenTypes.EqualsEquals:
                case TokenTypes.Less:
                case TokenTypes.More:
                case TokenTypes.LessOrEquals:
                case TokenTypes.MoreOrEquals:
                case TokenTypes.NotEquals:

                case TokenTypes.Or:
                case TokenTypes.And:
                    return true;

                default:
                    return false;
            }
        }

        private bool IsWantedToken(TokenTypes token, bool isCond)
        {
            if (isCond)
                return IsCondToken(token);
            return IsExprToken(token);
        }

        private BaseAst GetSubExprAST(out BaseAst expr, int level, bool isCond)
        {
            expr = null;

            BaseAst node = null;
            List<BaseAst> nodes = new List<BaseAst>();

            // number or identifier or left parenthesis
            if (!GetOperandNode(out node))
            {
                switch (node.Token)
                {
                    case TokenTypes.Neg:
                        if (isCond)
                        {
                            OperatorAst nodeN = (OperatorAst)node;
                            GetOperandNode(out node); // must be '('
                            if (node.Token != TokenTypes.ParenthesisLeft)
                                return BaseAst.GetErrorAstNode(string.Format("Po operaci negace je ocekavana leva zavorka, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                            BaseAst nodePRn = GetSubExprAST(out node, level + 1, isCond);
                            if (nodePRn.IsError)
                                return nodePRn;
                            if (nodePRn.Token != TokenTypes.ParenthesisRight)
                                return BaseAst.GetErrorAstNode(string.Format("Vyraz neni korektne ukoncen pravou zavorkou, radek {0}, sloupec {1}", nodePRn.TokenStartLine, nodePRn.TokenStartColumn));
                            nodeN.Right = node;
                            node = nodeN;
                        }
                        else
                            return BaseAst.GetErrorAstNode(string.Format("Chybna operace negace, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                        break;

                    case TokenTypes.ParenthesisLeft:
                        BaseAst nodePR = GetSubExprAST(out node, level + 1, isCond);
                        if (nodePR.IsError)
                            return nodePR;
                        if (nodePR.Token != TokenTypes.ParenthesisRight)
                            return BaseAst.GetErrorAstNode(string.Format("Vyraz neni korektne ukoncen pravou zavorkou, radek {0}, sloupec {1}", nodePR.TokenStartLine, nodePR.TokenStartColumn));
                        break;

                    case TokenTypes.ParenthesisRight:
                        return BaseAst.GetErrorAstNode(string.Format("Chybna prava zavorka, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));

                    case TokenTypes.Error:
                        return node;

                    default:
                        return BaseAst.GetErrorAstNode(string.Format("Prazdny vyraz, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                }
            }

            nodes.Add(node);

            while (IsWantedToken(nextNode.Token, isCond))
            {
                if (!GetExprBinaryOperationNode(out node, isCond))
                {
                    switch (node.Token)
                    {
                        case TokenTypes.Identifier:
                        case TokenTypes.Number:
                        case TokenTypes.ParenthesisLeft:
                            return BaseAst.GetErrorAstNode(string.Format("Nespravne formatovany vyraz, je ocekavan operator, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                    }
                    if (node.Token == TokenTypes.ParenthesisRight)
                    {
                        if (level == 0)
                            return BaseAst.GetErrorAstNode(string.Format("Chybna prava zavorka, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                        break;
                    }
                    continue;
                }
                if (!isCond && ((node.Token == TokenTypes.Multi) && (nodes[nodes.Count - 1].Token != TokenTypes.Number)))
                    return BaseAst.GetErrorAstNode(string.Format("Pred nasobenim muze byt pouze cislo, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));

                nodes.Add(node);

                if (!GetOperandNode(out node))
                {
                    switch (node.Token)
                    {
                        case TokenTypes.Neg:
                            if (isCond)
                            {
                                OperatorAst nodeN = (OperatorAst)node;
                                GetOperandNode(out node); // must be '('
                                if (node.Token != TokenTypes.ParenthesisLeft)
                                    return BaseAst.GetErrorAstNode(string.Format("Po operaci negace je ocekavana leva zavorka, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                                BaseAst nodePRn = GetSubExprAST(out node, level + 1, isCond);
                                if (nodePRn.IsError)
                                    return nodePRn;
                                if (nodePRn.Token != TokenTypes.ParenthesisRight)
                                    return BaseAst.GetErrorAstNode(string.Format("Vyraz neni korektne ukoncen pravou zavorkou, radek {0}, sloupec {1}", nodePRn.TokenStartLine, nodePRn.TokenStartColumn));
                                nodeN.Right = node;
                                node = nodeN;
                            }
                            else
                                return BaseAst.GetErrorAstNode(string.Format("Chybna operace negace, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                            break;

                        case TokenTypes.ParenthesisLeft:
                            BaseAst nodePR = GetSubExprAST(out node, level + 1, isCond);
                            if (nodePR.IsError)
                                return nodePR;
                            if (nodePR.Token != TokenTypes.ParenthesisRight)
                                return BaseAst.GetErrorAstNode(string.Format("Vyraz neni korektne ukoncen pravou zavorkou, radek {0}, sloupec {1}", nodePR.TokenStartLine, nodePR.TokenStartColumn));
                            break;

                        case TokenTypes.Error:
                            return node;

                        default:
                            return BaseAst.GetErrorAstNode(string.Format("Nespravne formatovany vyraz, je ocekavan cislo, promenna nebo leva zavorka, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                    }
                }
                nodes.Add(node);
            }

            if (nodes.Count == 0)
                return BaseAst.GetErrorAstNode(string.Format("Nespravne formatovany vyraz, je ocekavan cislo, promenna nebo leva zavorka, radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));

            int op = 10;
            while ((op < opMax) && (nodes.Count > 1))
            {
                int i = 1;
                while (i < nodes.Count)
                {
                    if (i >= (nodes.Count - 1))
                        return BaseAst.GetErrorAstNode("Nespravne formatovany vyraz... chybny pocet operandu");

                    OperatorAst oper = nodes[i] as OperatorAst;
                    if (oper == null)
                        return BaseAst.GetErrorAstNode("Nespravne formatovany vyraz... uzel neni operace");

                    if (oper.Priority == op)
                    {
                        oper.Left = nodes[i - 1];
                        oper.Right = nodes[i + 1];
                        nodes.RemoveAt(i + 1);
                        nodes.RemoveAt(i - 1);
                    }
                    else
                        i += 2;
                }

                op += 10; // increase operation priority
            }
            if (nodes.Count != 1)
                return BaseAst.GetErrorAstNode("Nespravne formatovany vyraz... nedobre utvoreny AST");

            expr = nodes[0];

            return node;
        }

        #endregion SA - expression

        public bool GetAST(out ProgramAst prg)
        {
            program = new ProgramAst();
            prg = program;

            BaseAst node = BaseAst.GetInitLoopAstNode();
            while ((node.Token != TokenTypes.End) && (!node.IsError))
            {
                ReadNextAst();
                node = actualNode;

                switch (actualNode.Token)
                {
                    case TokenTypes.VarRW: // deklarace globalni promenne
                        node = GetVariables();
                        break;


                    case TokenTypes.FunctionRW: // deklarace funkce
                        node = GetFunctionAST(node as FunctionAst);
                        break;


                    case TokenTypes.End: // konec programu
                        break;


                    default:
                        node = BaseAst.GetErrorAstNode(string.Format("Je ocekavano klicove slovo 'var' nebo 'function', radek {0}, sloupec {1}", node.TokenStartLine, node.TokenStartColumn));
                        break;
                }
            }

            if (node.IsError)
            {
                Console.WriteLine("Error: '{0}'", node.ErrorMessage);
                return false;
            }
            return true;
        }
    }
}
