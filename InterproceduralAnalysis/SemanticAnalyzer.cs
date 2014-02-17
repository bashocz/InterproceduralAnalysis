using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class SemanticAnalyzer
    {
        private BaseAst CheckFunctionCallsInIfAST(ProgramAst program, IfAst cmd)
        {
            BaseAst err = CheckFunctionCallsInStatementAST(program, cmd.IfBody);
            if ((err != null) && (err.Token != TokenTypes.End) && (err.IsError))
                return err;
            return CheckFunctionCallsInStatementAST(program, cmd.ElseBody);
        }

        private BaseAst CheckFunctionCallsInWhileAST(ProgramAst program, WhileAst cmd)
        {
            return CheckFunctionCallsInStatementAST(program, cmd.WhileBody);
        }

        private BaseAst CheckFunctionCallsInForAST(ProgramAst program, ForAst cmd)
        {
            BaseAst err = CheckFunctionCallsInStatementAST(program, cmd.Init);
            if ((err != null) && (err.Token != TokenTypes.End) && (err.IsError))
                return err;
            err = CheckFunctionCallsInStatementAST(program, cmd.Close);
            if ((err != null) && (err.Token != TokenTypes.End) && (err.IsError))
                return err;
            return CheckFunctionCallsInStatementAST(program, cmd.ForBody);

        }

        private BaseAst CheckFunctionCallsInBlockAST(ProgramAst program, BlockAst block)
        {
            foreach (BaseAst node in block.Statements)
            {
                BaseAst err = CheckFunctionCallsInStatementAST(program, node);
                if ((err != null) && (err.Token != TokenTypes.End) && (err.IsError))
                    return err;
            }
            return BaseAst.GetEndAstNode();
        }

        private BaseAst CheckFunctionCallsInStatementAST(ProgramAst program, BaseAst st)
        {
            if ((st != null) && (st.AstType == AstNodeTypes.FunctionCall))
            {
                if (!program.OrigFncs.Keys.Contains(st.TokenText))
                    return BaseAst.GetErrorAstNode(string.Format("Funkce '{0}' doposud nebyla deklarovana, radek {1}, sloupec {2}", st.TokenText, st.TokenStartLine, st.TokenStartColumn));
            }

            BaseAst inner = null;
            if (st is BlockAst)
                inner = CheckFunctionCallsInBlockAST(program, st as BlockAst);
            if (st is IfAst)
                inner = CheckFunctionCallsInIfAST(program, st as IfAst);
            if (st is WhileAst)
                inner = CheckFunctionCallsInWhileAST(program, st as WhileAst);
            if (st is ForAst)
                inner = CheckFunctionCallsInForAST(program, st as ForAst);

            if ((inner != null) && (inner.Token != TokenTypes.End) && (inner.IsError))
                return inner;

            return BaseAst.GetEndAstNode();
        }

        private BaseAst CheckFunctionAST(ProgramAst program, FunctionAst fnc)
        {
            BaseAst err = CheckFunctionCallsInBlockAST(program, fnc.Body);
            return err;
        }

        public bool CheckAST(ProgramAst program)
        {
            foreach (string fncName in program.OrigFncs.Keys)
            {
                FunctionAst fnc = program.OrigFncs[fncName];
                BaseAst err = CheckFunctionAST(program, fnc);
                if ((err.Token != TokenTypes.End) && (err.IsError))
                {
                    Console.WriteLine("Error: '{0}'", err.ErrorMessage);
                    return false;
                }
            }
            return true;
        }
    }
}
