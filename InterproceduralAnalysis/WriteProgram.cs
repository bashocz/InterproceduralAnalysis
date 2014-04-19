using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class WriteProgram
    {
        private StreamWriter file;

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

        private void WriteOperator(OperatorAst ast, string p)
        {
            file.WriteLine("{0}{1};", p, GetExpr(ast));
        }

        private void WriteIf(IfAst ast, string p)
        {
            file.WriteLine("{0}if ({1})", p, GetExpr(ast.Condition));
            WriteBody(ast.IfBody, p);
            if (ast.ElseBody != null)
            {
                file.WriteLine("{0}else", p);
                WriteBody(ast.ElseBody, p);
            }
        }

        private void WriteWhile(WhileAst ast, string p)
        {
            file.WriteLine("{0}while ({1})", p, GetExpr(ast.Condition));
            WriteBody(ast.WhileBody, p);
        }

        private void WriteFor(ForAst ast, string p)
        {
            file.WriteLine("{0}for ({1}; {2}; {3})", p, GetExpr(ast.Init), GetExpr(ast.Condition), GetExpr(ast.Close));
            WriteBody(ast.ForBody, p);
        }

        private void WriteBlock(BlockAst block, string prefix)
        {
            foreach (BaseAst ast in block.Statements)
            {
                WriteStatement(ast, prefix);
            }
        }

        private void WriteGoto(GotoAst ast, string p)
        {
            file.WriteLine("{0}goto {1};", p, ast.Label);
        }

        private void WriteFunctionCall(BaseAst ast, string p)
        {
            file.WriteLine("{0}{1}();", p, ast.TokenText);
        }

        private void WriteLabel(BaseAst ast)
        {
            file.WriteLine("{0}:", ast.TokenText);
        }

        private void WriteReturn(BaseAst ast, string p)
        {
            file.WriteLine("{0}return;", p);
        }

        private void WriteStatement(BaseAst ast, string p)
        {
            if (ast is OperatorAst)
                WriteOperator(ast as OperatorAst, p);
            else if (ast is IfAst)
                WriteIf(ast as IfAst, p);
            else if (ast is WhileAst)
                WriteWhile(ast as WhileAst, p);
            else if (ast is ForAst)
                WriteFor(ast as ForAst, p);
            else if (ast is BlockAst)
                WriteBlock(ast as BlockAst, p);
            else if (ast is GotoAst)
                WriteGoto(ast as GotoAst, p);
            else
            {
                if (ast.AstType == AstNodeTypes.FunctionCall)
                    WriteFunctionCall(ast, p);
                else if (ast.AstType == AstNodeTypes.Label)
                    WriteLabel(ast);
                else if (ast.AstType == AstNodeTypes.Return)
                    WriteReturn(ast, p);
                else
                    throw new ApplicationException();
            }
        }

        private void WriteBody(BaseAst ast, string prefix)
        {
            file.WriteLine("{0}{{", prefix);

            WriteStatement(ast, prefix + "  ");

            file.WriteLine("{0}}}", prefix);
        }

        private void WriteFunctions(string name, FunctionAst fnc)
        {
            file.WriteLine();
            file.WriteLine("function {0}()", name);
            WriteBody(fnc.Body, "");
        }

        private void WriteFunctions(ProgramAst prg)
        {
            foreach (string name in prg.OrigFncs.Keys)
            {
                WriteFunctions(name, prg.OrigFncs[name]);
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
