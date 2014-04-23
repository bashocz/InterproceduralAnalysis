using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class TransitionMatrixSet
    {
        private readonly IaEdge parent;
        private readonly BaseFunctions b;

        private readonly int var_n;
        private readonly long var_m;

        public TransitionMatrixSet(IaEdge parent, BaseFunctions b)
        {
            this.parent = parent;
            this.b = b;

            var_n = b.var_n;
            var_m = b.var_m;

            TMatrixes = new List<long[][]>();
        }

        public List<long[][]> TMatrixes { get; private set; }

        private bool GetConst(BaseAst node, int sign, List<string> vars, out int vii, out long c)
        {
            vii = 0;
            c = 0;

            if ((node.AstType == AstNodeTypes.Number) && (node is NumberAst))
            {
                c = ((sign * (node as NumberAst).Number + var_m) % var_m + var_m) % var_m;
            }
            else if (node.AstType == AstNodeTypes.Variable)
            {
                vii = vars.IndexOf(node.TokenText) + 1;
                c = 1;
                if (sign < 0)
                    c = var_m - 1;
            }
            else if ((node.AstType == AstNodeTypes.Operator) && (node is OperatorAst) && (node.Token == TokenTypes.Multi))
            {
                BaseAst num = (node as OperatorAst).Left;
                BaseAst var = (node as OperatorAst).Right;

                if (var is NumberAst)
                {
                    BaseAst tmp = num;
                    num = var;
                    var = tmp;
                }

                if ((num.AstType == AstNodeTypes.Number) && (num is NumberAst))
                {
                    c = ((sign * (num as NumberAst).Number + var_m) % var_m + var_m) % var_m;
                }
                else
                {
                    return false;
                }

                if (var.AstType == AstNodeTypes.Variable)
                {
                    vii = vars.IndexOf(var.TokenText) + 1;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool ProceedUnary(OperatorAst op, long[][] mtx, List<string> vars, out int vu, out long c)
        {
            vu = 0;
            c = 1L;

            if (op == null)
                return true;

            bool isError = false;

            if (op.Token == TokenTypes.MinusMinus)
                c = var_m - 1;

            if ((op.Left != null) && (op.Left.AstType == AstNodeTypes.Variable) && (op.Right == null))
            {
                vu = vars.IndexOf(op.Left.TokenText) + 1;
                mtx[0][vu] = (mtx[0][vu] + c) % var_m;
            }
            else if ((op.Right != null) && (op.Right.AstType == AstNodeTypes.Variable) && (op.Left == null))
            {
                vu = vars.IndexOf(op.Right.TokenText) + 1;
                mtx[0][vu] = (mtx[0][vu] + c) % var_m;
            }
            else
            {
                isError = true;
            }

            return !isError;
        }

        private bool ProceedUnary(OperatorAst op, int sign, long[][] mtx, int vi, List<string> vars)
        {
            int vu;
            long c;
            bool isError = false;

            if (!(isError = !ProceedUnary(op, mtx, vars, out vu, out c)))
            {
                long s = 1;
                if (sign < 0)
                    s = var_m - 1;
                mtx[vu][vi] = (mtx[vu][vi] + s) % var_m;
                if ((op.Right != null) && (op.Right.AstType == AstNodeTypes.Variable))
                {
                    if (vu != vi) 
                    {
                        if (((c == 1) && (s == 1)) || ((c > 1) && (s > 1)))
                            s = 1;
                        else
                            s = var_m - 1;
                        mtx[0][vi] = (mtx[0][vi] + s) % var_m; 
                    }
                }
            }

            return !isError;
        }

        private bool ProceedNode(BaseAst node, int sign, long[][] mtx, int vi, List<string> vars)
        {
            int vii;
            long c;
            bool isError = false;

            if ((node.AstType == AstNodeTypes.Number) ||
                (node.AstType == AstNodeTypes.Variable) ||
                ((node.AstType == AstNodeTypes.Operator) && (node.Token == TokenTypes.Multi)))
            {
                if (!(isError = !GetConst(node, sign, vars, out vii, out c)))
                {
                    mtx[vii][vi] = (mtx[vii][vi] + c) % var_m;
                }
            }
            else if ((node.AstType == AstNodeTypes.Operator) && ((node.Token == TokenTypes.PlusPlus) || (node.Token == TokenTypes.MinusMinus)))
            {
                isError = !ProceedUnary(node as OperatorAst, sign, mtx, vi, vars);
            }
            else
            {
                isError = true;
            }
            return !isError;
        }

        private bool ProceedExpr(BaseAst top, long[][] mtx, int vi, List<string> vars)
        {
            BaseAst node = top;
            int sign = 1;
            bool isError = false;

            while ((node != null) && (!isError))
            {
                if ((node.AstType == AstNodeTypes.Number) ||
                    (node.AstType == AstNodeTypes.Variable) ||
                    ((node.AstType == AstNodeTypes.Operator) && ((node.Token == TokenTypes.Multi) || (node.Token == TokenTypes.PlusPlus)) || (node.Token == TokenTypes.MinusMinus)))
                {
                    isError = !ProceedNode(node, sign, mtx, vi, vars);
                    node = null;
                }
                else if ((node.AstType == AstNodeTypes.Operator) && (node is OperatorAst) && ((node.Token == TokenTypes.Plus) || (node.Token == TokenTypes.Minus)))
                {
                    BaseAst left = (node as OperatorAst).Left;
                    BaseAst right = (node as OperatorAst).Right;

                    sign = 1;
                    if (node.Token == TokenTypes.Minus)
                        sign = -1;

                    if ((right.AstType == AstNodeTypes.Operator) && ((right.Token == TokenTypes.Plus) || (right.Token == TokenTypes.Minus)))
                    {
                        BaseAst tmp = left;
                        left = right;
                        right = tmp;
                    }

                    if ((right.AstType == AstNodeTypes.Number) ||
                        (right.AstType == AstNodeTypes.Variable) ||
                        ((right.AstType == AstNodeTypes.Operator) && ((right.Token == TokenTypes.Multi) || (right.Token == TokenTypes.PlusPlus)) || (right.Token == TokenTypes.MinusMinus)))
                    {
                        if (isError = !ProceedNode(right, sign, mtx, vi, vars))
                            break;
                    }
                    else
                    {
                        isError = true;
                        break;
                    }

                    sign = 1;
                    node = left;
                }
                else
                {
                    isError = true;
                    break;
                }
            }
            return !isError;
        }

        private bool ProceedPartCondition(OperatorAst op, long[][] mtx, List<string> vars)
        {
            bool isError = false;
            if (op != null)
            {
                if ((op.Token == TokenTypes.PlusPlus) || (op.Token == TokenTypes.MinusMinus))
                {
                    int vu;
                    long c;
                    isError = !ProceedUnary(op, mtx, vars, out vu, out c);
                }
                if (!isError && (op.Left is OperatorAst))
                    isError = !ProceedPartCondition(op.Left as OperatorAst, mtx, vars);
                if (!isError && (op.Right is OperatorAst))
                    isError = !ProceedPartCondition(op.Right as OperatorAst, mtx, vars);
            }

            return !isError;
        }

        private bool ProceedCondition(ConditionAst cond, long[][] mtx, List<string> vars)
        {
            bool isError = false;
            if (cond != null)
            {
                OperatorAst op = cond.Condition as OperatorAst;
                if (op != null)
                    isError = !ProceedPartCondition(op, mtx, vars);
            }

            return !isError;
        }

        private void AddMatrixesForUnknownExpr(int vi)
        {
            long[][] mtx = null;
            mtx = b.GetIdentity(var_n);
            mtx[vi][vi] = 0;
            TMatrixes.Add(mtx);
            
            mtx = b.GetIdentity(var_n);
            mtx[vi][vi] = 0;
            mtx[0][vi] = 1;
            TMatrixes.Add(mtx); 
        }

        public void GetMatrix(List<string> vars)
        {
            OperatorAst expr = parent.Ast as OperatorAst;

            if (expr != null)
            {
                if (expr.Token == TokenTypes.Equals)
                {
                    int vi = vars.IndexOf(expr.Left.TokenText) + 1;
                    if ((vi > 0) && (vi <= vars.Count))
                    {
                        long[][] mtx = b.GetIdentity(var_n);

                        mtx[vi][vi] = 0; 
                        if (ProceedExpr(expr.Right, mtx, vi, vars))
                            TMatrixes.Add(mtx);
                        else
                        {
                            AddMatrixesForUnknownExpr(vi);
                        }
                    }
                }
                else if ((expr.Token == TokenTypes.PlusPlus) || (expr.Token == TokenTypes.MinusMinus))
                {
                    long[][] mtx = b.GetIdentity(var_n);
                    int vu;
                    long c;
                    if (ProceedUnary(expr as OperatorAst, mtx, vars, out vu, out c))
                    {
                        TMatrixes.Add(mtx);
                    }
                    else
                    {
                        AddMatrixesForUnknownExpr(vu);
                    }
                }
            }
            else if (parent.Ast is ConditionAst)
            {
                long[][] mtx = b.GetIdentity(var_n);
                if (ProceedCondition(parent.Ast as ConditionAst, mtx, vars))
                {
                    TMatrixes.Add(mtx);
                }
                else
                {
                    TMatrixes.Add(b.GetIdentity(var_n));
                }
            }
            else if ((parent.Ast == null) || (parent.Ast.AstType != AstNodeTypes.FunctionCall))
            {
                TMatrixes.Add(b.GetIdentity(var_n));
            }
        }

        public void Print()
        {
            Console.WriteLine("M (pocet matic {1}) na hrane '{0}':", parent.Name, (TMatrixes != null) ? TMatrixes.Count : 0);

            if ((TMatrixes == null) || (TMatrixes.Count == 0))
            {
                Console.WriteLine("prazdna mnozina");
                return;
            }

            int m = 1;
            foreach (long[][] mtx in TMatrixes)
            {
                Console.WriteLine("M[{0}]:", m);

                int k = mtx.Length;
                int l = mtx[0].Length;

                for (int j = 0; j < l; j++)
                {
                    for (int i = 0; i < k; i++)
                    {
                        Console.Write("{0} ", mtx[i][j]);
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
                m++;
            }
        }
    }
}
