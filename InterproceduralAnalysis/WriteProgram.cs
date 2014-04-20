﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class WriteProgram
    {
        private StreamWriter file;

        private void WriteLinearEquations(IaNode node, List<string> vars, string prefix)
        {
            string p = prefix + "  ";
            if (node != null)
            {
                if (node.LinearEquations == null)
                    file.WriteLine("{0}// Nedosazitelny stav...", p);
                else if (node.LinearEquations.GArr[0] == null)
                    file.WriteLine("{0}// Zadna omezeni...", p);
                else
                {
                    int i = 0;
                    while ((i < node.LinearEquations.GArr.Length) && (node.LinearEquations.GArr[i] != null))
                    {
                        string le = string.Empty;
                        long[] a = node.LinearEquations.GArr[i].Vr;
                        if (a[0] > 0)
                            le += string.Format("{0}", a[0]);
                        for (int j = 1; j < a.Length; j++)
                        {
                            if (a[j] > 0)
                            {
                                if (!string.IsNullOrEmpty(le))
                                    le += " + ";
                                le += string.Format("{0}*{1}", a[j], vars[j - 1]);
                            }
                        }
                        file.WriteLine("{0}// {1} = 0", p, le);
                        i++;
                    }
                }
            }
            else
            {
                file.WriteLine("{0}// Uzel neni ulozen...", p);
            }
        }

        private string GetExpr(BaseAst ast)
        {
            if (ast == null)
                throw new ApplicationException();

            if (ast.AstType == AstNodeTypes.Number)
                return ast.TokenText;
            if (ast.AstType == AstNodeTypes.Variable)
                return ast.TokenText;
            if (ast is OperatorAst)
            {
                OperatorAst op = ast as OperatorAst;
                if ((op.Token == TokenTypes.PlusPlus) || (op.Token == TokenTypes.MinusMinus))
                {
                    if (op.Left != null)
                        return GetExpr(op.Left) + op.TokenText;
                    return op.TokenText + GetExpr(op.Right);
                }

                if (op.Token == TokenTypes.Neg)
                {
                    return string.Format("!({0})", GetExpr(op.Right));
                }

                OperatorAst leftOp = op.Left as OperatorAst;
                string left = ((leftOp != null) && (leftOp.Priority > op.Priority)) ? "(" + GetExpr(op.Left) + ")" : GetExpr(op.Left);

                OperatorAst rightOp = op.Right as OperatorAst;
                string right = ((rightOp != null) && (rightOp.Priority > op.Priority)) ? "(" + GetExpr(op.Right) + ")" : GetExpr(op.Right);

                return string.Format("{0} {1} {2}", left, op.TokenText, right);
            }
            throw new ApplicationException();
        }

        private void WriteOperator(OperatorAst ast, List<string> vars, string p)
        {
            file.WriteLine("{0}{1};", p, GetExpr(ast));
            WriteLinearEquations(ast.Node, vars, p);
        }

        private void WriteIf(IfAst ast, List<string> vars, string p)
        {
            file.WriteLine("{0}if ({1})", p, GetExpr(ast.Condition));
            WriteLinearEquations(ast.ConvertCondition.Node, vars, p);

            WriteBody(ast.IfBody, vars, p);
            if (ast.ElseBody != null)
            {
                file.WriteLine("{0}else", p);
                WriteBody(ast.ElseBody, vars, p);
            }
        }

        private void WriteWhile(WhileAst ast, List<string> vars, string p)
        {
            file.WriteLine("{0}while ({1})", p, GetExpr(ast.Condition));
            WriteLinearEquations(ast.ConvertCondition.Node, vars, p);

            WriteBody(ast.WhileBody, vars, p);
        }

        private void WriteFor(ForAst ast, List<string> vars, string p)
        {
            file.WriteLine("{0}for ({1}; {2}; {3})", p, GetExpr(ast.Init), GetExpr(ast.Condition), GetExpr(ast.Close));
            file.WriteLine("{0}// #1", p);
            WriteLinearEquations(ast.ConvertInit.Node, vars, p); // init
            file.WriteLine("{0}// #2", p);
            WriteLinearEquations(ast.ConvertCondition.Node, vars, p); // condition
            file.WriteLine("{0}// #3", p);
            WriteLinearEquations(ast.ConvertClose.Node, vars, p); // close
            WriteBody(ast.ForBody, vars, p);
        }

        private void WriteBlock(BlockAst block, List<string> vars, string prefix)
        {
            foreach (BaseAst ast in block.Statements)
            {
                WriteStatement(ast, vars, prefix);
            }
        }

        private void WriteGoto(GotoAst ast, List<string> vars, string p)
        {
            file.WriteLine("{0}goto {1};", p, ast.Label);
            WriteLinearEquations(ast.Node, vars, p);
        }

        private void WriteFunctionCall(BaseAst ast, List<string> vars, string p)
        {
            file.WriteLine("{0}{1}();", p, ast.TokenText);
            WriteLinearEquations(ast.Node, vars, p);
        }

        private void WriteLabel(BaseAst ast)
        {
            file.WriteLine("{0}:", ast.TokenText);
        }

        private void WriteReturn(BaseAst ast, List<string> vars, string p)
        {
            file.WriteLine("{0}return;", p);
            WriteLinearEquations(ast.Node, vars, p);
        }

        private void WriteStatement(BaseAst ast, List<string> vars, string p)
        {
            if (ast is OperatorAst)
                WriteOperator(ast as OperatorAst, vars, p);
            else if (ast is IfAst)
                WriteIf(ast as IfAst, vars, p);
            else if (ast is WhileAst)
                WriteWhile(ast as WhileAst, vars, p);
            else if (ast is ForAst)
                WriteFor(ast as ForAst, vars, p);
            else if (ast is BlockAst)
                WriteBlock(ast as BlockAst, vars, p);
            else if (ast is GotoAst)
                WriteGoto(ast as GotoAst, vars, p);
            else
            {
                if (ast.AstType == AstNodeTypes.FunctionCall)
                    WriteFunctionCall(ast, vars, p);
                else if (ast.AstType == AstNodeTypes.Label)
                    WriteLabel(ast);
                else if (ast.AstType == AstNodeTypes.Return)
                    WriteReturn(ast, vars, p);
                else
                    throw new ApplicationException();
            }
        }

        private void WriteBody(BaseAst ast, List<string> vars, string prefix)
        {
            file.WriteLine("{0}{{", prefix);

            WriteStatement(ast, vars, prefix + "  ");

            file.WriteLine("{0}}}", prefix);
        }

        private void WriteFunction(string name, FunctionAst fnc, List<string> vars)
        {
            file.WriteLine();
            file.WriteLine("function {0}()", name);
            WriteBody(fnc.Body, vars, "");
        }

        private void WriteFunctions(ProgramAst prg)
        {
            foreach (string name in prg.OrigFncs.Keys)
            {
                WriteFunction(name, prg.OrigFncs[name], prg.Vars);
            }
        }

        private void WriteVariables(ProgramAst prg)
        {
            string vars = string.Empty;

            foreach (string var in prg.Vars)
            {
                if (prg.VarsDecl[var] == null)
                {
                    if (string.IsNullOrEmpty(vars))
                        vars = "var " + var;
                    else
                        vars += ", " + var;
                }
                else
                {
                    if (!string.IsNullOrEmpty(vars))
                        file.WriteLine(vars + ";");
                    vars = string.Empty;

                    file.WriteLine("var {0} = {1};", var, GetExpr(prg.VarsDecl[var]));
                }
            }

            if (!string.IsNullOrEmpty(vars))
                file.WriteLine(vars + ";");
        }

        private string GetProgramFile(string originProgramFile)
        {
            return Path.ChangeExtension(originProgramFile, ".ia." + Path.GetExtension(originProgramFile));
        }

        public void Write(ProgramAst prg, string originProgramFile)
        {
            string programFile = GetProgramFile(originProgramFile);

            using (file = new StreamWriter(programFile, false))
            {
                WriteVariables(prg);

                WriteFunctions(prg);

                file.Flush();
                file.Close();
            }
        }
    }
}
