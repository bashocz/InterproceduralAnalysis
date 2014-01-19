using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class StatementConverter
    {
        private bool isPrint;

        private int internalLabelIdx;

        public StatementConverter(bool isPrint)
        {
            this.isPrint = isPrint;
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

        private BaseAst GetErrorAstNode(string errorMsg)
        {
            return new BaseAst { IsError = true, ErrorMessage = errorMsg, Token = TokenTypes.Error };
        }

        private BlockAst ConvertFncBodyAST(BlockAst body)
        {
            BlockAst cb = ConvertTo<BlockAst>(body, AstNodeTypes.Block);
            foreach (BaseAst st in body.Statements)
            {
                cb.Statements.AddRange(ConvertStatement(st));
            }

            return cb;
        }

        private IEnumerable<BaseAst> ConvertStatement(BaseAst node)
        {
            if (node is BlockAst)
            {
                List<BaseAst> block = new List<BaseAst>();
                foreach (BaseAst st in (node as BlockAst).Statements)
                {
                    block.AddRange(ConvertStatement(st));
                }
                return block;
            }

            if (node is IfAst)
                return ConvertIf(node as IfAst);

            if (node is WhileAst)
                return ConvertWhile(node as WhileAst);

            if (node is ForAst)
                return ConvertFor(node as ForAst);

            return new List<BaseAst>() { node };
        }

        private BaseAst NegateCondition(BaseAst cond)
        {
            if ((cond is OperatorAst) && ((cond as OperatorAst).Token == TokenTypes.Neg))
            {
                return (cond as OperatorAst).Right;
            }
            return new OperatorAst { Token = TokenTypes.Neg, AstType = AstNodeTypes.Operator, TokenText = "!", Right = cond };
        }

        private IEnumerable<BaseAst> ConvertIf(IfAst node)
        {
            List<BaseAst> block = new List<BaseAst>();

            internalLabelIdx++;
            int idx = internalLabelIdx;

            // this label is not needed, but for better orientation in final code
            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$IfBegin{0}", idx) });

            ConditionAst cond = ConvertTo<ConditionAst>(node, AstNodeTypes.Condition);
            cond.Condition = NegateCondition(node.Condition);
            block.Add(cond);

            if (node.ElseBody != null)
                block.Add(new GotoAst { Token = TokenTypes.GotoRW, AstType = AstNodeTypes.Goto, TokenText = "goto", Label = string.Format("$IfFalse{0}", idx) });
            else
                block.Add(new GotoAst { Token = TokenTypes.GotoRW, AstType = AstNodeTypes.Goto, TokenText = "goto", Label = string.Format("$IfEnd{0}", idx) });

            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$IfTrue{0}", idx) });
            block.AddRange(ConvertStatement(node.IfBody));

            if (node.ElseBody != null)
            {
                block.Add(new GotoAst { Token = TokenTypes.GotoRW, AstType = AstNodeTypes.Goto, TokenText = "goto", Label = string.Format("$IfEnd{0}", idx) });

                // this label is not needed, but for better orientation in final code
                block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$IfFalse{0}", idx) });
                block.AddRange(ConvertStatement(node.ElseBody));
            }

            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$IfEnd{0}", idx) });

            return block;
        }

        private IEnumerable<BaseAst> ConvertWhile(WhileAst node)
        {
            List<BaseAst> block = new List<BaseAst>();

            internalLabelIdx++;
            int idx = internalLabelIdx;

            // this label is not needed, but for better orientation in final code
            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$WhileBegin{0}", idx) });

            ConditionAst cond = ConvertTo<ConditionAst>(node, AstNodeTypes.Condition);
            cond.Condition = NegateCondition(node.Condition);
            block.Add(cond);

            block.Add(new GotoAst { Token = TokenTypes.GotoRW, AstType = AstNodeTypes.Goto, TokenText = "goto", Label = string.Format("$WhileEnd{0}", idx) });

            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$WhileTrue{0}", idx) });
            block.AddRange(ConvertStatement(node.WhileBody));
            block.Add(new GotoAst { Token = TokenTypes.GotoRW, AstType = AstNodeTypes.Goto, TokenText = "goto", Label = string.Format("$WhileBegin{0}", idx) });

            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$WhileEnd{0}", idx) });

            return block;
        }

        private IEnumerable<BaseAst> ConvertFor(ForAst node)
        {
            List<BaseAst> block = new List<BaseAst>();

            internalLabelIdx++;
            int idx = internalLabelIdx;

            // this label is not needed, but for better orientation in final code
            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$ForBegin{0}", idx) });

            block.AddRange(ConvertStatement(node.Init));

            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$ForCondition{0}", idx) });

            ConditionAst cond = ConvertTo<ConditionAst>(node, AstNodeTypes.Condition);
            cond.Condition = NegateCondition(node.Condition);
            block.Add(cond);

            block.Add(new GotoAst { Token = TokenTypes.GotoRW, AstType = AstNodeTypes.Goto, TokenText = "goto", Label = string.Format("$ForEnd{0}", idx) });

            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$ForTrue{0}", idx) });
            block.AddRange(ConvertStatement(node.ForBody));
            block.AddRange(ConvertStatement(node.Close));
            block.Add(new GotoAst { Token = TokenTypes.GotoRW, AstType = AstNodeTypes.Goto, TokenText = "goto", Label = string.Format("$ForCondition{0}", idx) });

            block.Add(new BaseAst { Token = TokenTypes.Identifier, AstType = AstNodeTypes.Label, TokenText = string.Format("$ForEnd{0}", idx) });

            return block;
        }

        private string PrintSAExpr(BaseAst node)
        {
            if ((node is BaseAst) && (node.AstType == AstNodeTypes.Variable))
                return node.TokenText;
            if (node is NumberAst)
                return (node as NumberAst).Number.ToString();
            if (node is OperatorAst)
            {
                OperatorAst oper = node as OperatorAst;
                switch (oper.Token)
                {
                    case TokenTypes.PlusPlus:
                    case TokenTypes.MinusMinus:
                        if (oper.Right != null)
                            return string.Format("{0}{1}", oper.TokenText, PrintSAExpr(oper.Right));
                        return string.Format("{0}{1}", PrintSAExpr(oper.Left), oper.TokenText);

                    case TokenTypes.Neg:
                        return string.Format("{0}({1})", oper.TokenText, PrintSAExpr(oper.Right));

                    default:
                        return string.Format("({0} {1} {2})", PrintSAExpr(oper.Left), oper.TokenText, PrintSAExpr(oper.Right));
                }
            }

            return "...";
        }

        private void PrintSAOper(OperatorAst oper)
        {
            switch (oper.Token)
            {
                case TokenTypes.Equals:
                    Console.WriteLine("    {0} = {1};", oper.Left.TokenText, PrintSAExpr(oper.Right));
                    break;

                case TokenTypes.PlusPlus:
                case TokenTypes.MinusMinus:
                    Console.WriteLine("    {0};", PrintSAExpr(oper));
                    break;

                default:
                    Console.WriteLine("Toto neni znamy vyraz");
                    break;
            }
        }

        private void PrintSACond(ConditionAst cond)
        {
            Console.WriteLine("    if ({0})", PrintSAExpr(cond.Condition));
        }

        private void PrintSALabel(BaseAst label)
        {
            Console.WriteLine("{0}:", label.TokenText);
        }

        private void PrintSAGoto(GotoAst gotoc)
        {
            Console.WriteLine("    goto {0};", gotoc.Label);
        }

        private void PrintSAFncCall(BaseAst fnc)
        {
            Console.WriteLine("    {0}();", fnc.TokenText);
        }

        private void PrintSAReturn(BaseAst ret)
        {
            Console.WriteLine("    return;");
        }

        private void PrintSAVarDecl(string varName, BaseAst var)
        {
            if (var != null)
                Console.WriteLine("var {0} = {1};", varName, PrintSAExpr(var));
            else
                Console.WriteLine("var {0};", varName);
        }

        private void PrintSAFncDecl(string fncName, BlockAst body)
        {
            if (body != null)
            {
                Console.WriteLine();
                Console.WriteLine("function {0}()", fncName);
                Console.WriteLine("{");

                foreach (BaseAst st in body.Statements)
                {
                    if (st is OperatorAst)
                        PrintSAOper(st as OperatorAst);
                    else if (st is ConditionAst)
                        PrintSACond(st as ConditionAst);
                    else if ((st is BaseAst) && (st.AstType == AstNodeTypes.Label))
                        PrintSALabel(st as BaseAst);
                    else if (st is GotoAst)
                        PrintSAGoto(st as GotoAst);
                    else if ((st is BaseAst) && (st.AstType == AstNodeTypes.FunctionCall))
                        PrintSAFncCall(st as BaseAst);
                    else if ((st is BaseAst) && (st.AstType == AstNodeTypes.Return))
                        PrintSAReturn(st as BaseAst);
                    else
                    {
                        Console.WriteLine("Neco neni v poradku :-)...");
                        break;
                    }
                }

                Console.WriteLine("}");
            }
        }

        private void PrintVarsDecl(ProgramAst program)
        {
            foreach (string varName in program.VarsDecl.Keys)
            {
                PrintSAVarDecl(varName, program.VarsDecl[varName]);
            }
        }

        public bool ConvertToIfGoto(ProgramAst program)
        {
            if (isPrint)
                PrintVarsDecl(program);

            foreach (string fncName in program.OrigFncs.Keys)
            {
                FunctionAst fnc = program.OrigFncs[fncName];
                FunctionAst cf = ConvertTo<FunctionAst>(fnc, AstNodeTypes.Function);
                cf.Body = ConvertFncBodyAST(fnc.Body);

                if (isPrint)
                    PrintSAFncDecl(fncName, cf.Body);

                program.ConvFncs.Add(fncName, cf);
            }
            return true;
        }
    }
}
