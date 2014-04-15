using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace InterproceduralAnalysis
{
    class InterproceduralAnalyzer
    {
        private readonly BaseFunctions bfm;
        private readonly BaseFunctions bg;

        private readonly bool printM;
        private readonly bool printG;
        private readonly bool printLE;

        public InterproceduralAnalyzer(int w, int n, bool printGM, bool printGG, bool printLE)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            this.printM = printGM;
            this.printG = printGG;
            this.printLE = printLE;

            bg = new BaseFunctions(w, n);
            bfm = new BaseFunctions(w, (bg.var_n * bg.var_n) - 1);
        }

        #region Creating Transition Matrixes

        private void CreateTransitionMatrixes(ProgramAst prg)
        {
            Queue<IaNode> q = new Queue<IaNode>();
            foreach (string name in prg.OrigFncs.Keys)
                q.Enqueue(prg.Graph[name]);

            while (q.Count > 0)
            {
                IaNode n = q.Dequeue();
                if (n != null)
                {
                    foreach (IaEdge edge in n.Edges)
                    {
                        if (edge.MatrixSet == null)
                        {
                            var mtx = new TransitionMatrixSet(edge, bg);
                            mtx.GetMatrix(prg.Vars);
                            edge.MatrixSet = mtx;

                            if (printM)
                                mtx.Print();

                            q.Enqueue(edge.To);
                        }
                    }
                }
            }
        }

        #endregion Creating Transition Matrixes

        #region Creating Function Call Matrixes

        private void CreateEmptyFunctionG(ProgramAst prg, List<IaEdge> fncCallEdges)
        {
            foreach (IaNode node in prg.Graph.Values)
            {
                Queue<IaNode> q = new Queue<IaNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    IaNode n = q.Dequeue();
                    if (n.FunctionGSet == null)
                    {
                        n.FunctionGSet = new GeneratorSet(n, bfm);
                        foreach (IaEdge edge in n.Edges)
                        {
                            if ((edge.Ast != null) && (edge.Ast.AstType == AstNodeTypes.FunctionCall))
                                fncCallEdges.Add(edge);

                            if (edge.To.FunctionGSet == null)
                                q.Enqueue(edge.To);
                        }
                    }
                }
            }
        }

        private void CreateFunctionMatrixes(ProgramAst prg)
        {
            List<IaEdge> fncCallEdges = new List<IaEdge>();
            CreateEmptyFunctionG(prg, fncCallEdges);

            Queue<NodeMatrix> w_queue = new Queue<NodeMatrix>();
            foreach (string name in prg.OrigFncs.Keys)
                w_queue.Enqueue(new NodeMatrix { Node = prg.Graph[name], Matrix = bfm.GetIdentity(bg.var_n) });

            while (w_queue.Count > 0)
            {
                NodeMatrix pair = w_queue.Dequeue();

                IaNode from = pair.Node;
                if ((from.Next != null) || (from.IsTrue != null) || (from.IsFalse != null))
                {
                    foreach (IaEdge edge in from.Edges)
                    {
                        if ((edge.Ast == null) || (edge.Ast.AstType != AstNodeTypes.FunctionCall))
                        {
                            IaNode to = edge.To;

                            // algoritmus 2
                            foreach (long[][] a_mtx in edge.MatrixSet.TMatrixes)
                            {
                                long[][] mtx = bg.MatrixMultiMatrix(a_mtx, pair.Matrix, bfm.var_m);
                                long[] xi = bg.MatrixToVector(mtx);
                                LeadVector x = new LeadVector(xi);
                                if (x.Lidx >= 0) // neni to nulovy vektor
                                {
                                    if (to.FunctionGSet.AddVector(x))
                                    {
                                        if (printG)
                                            to.FunctionGSet.Print();

                                        w_queue.Enqueue(new NodeMatrix { Node = to, Matrix = mtx });
                                    }
                                }
                            }
                        }
                        else
                        {
                            // algoritmus 3
                        }
                    }
                }
                else
                {
                    // algoritmus 4

                    foreach (IaEdge edge in fncCallEdges)
                    {
                        if ((edge.Ast == null) || (edge.Ast.AstType != AstNodeTypes.FunctionCall))
                            throw new ApplicationException();

                        if (edge.Ast.TokenText == from.FncName)
                        {
                            IaNode to = edge.To;

                            int i = 0;
                            while (edge.From.FunctionGSet.GArr[i] != null)
                            {
                                LeadVector vector = edge.From.FunctionGSet.GArr[i];
                                long[][] matrix = bfm.VectorToMatrix(vector.Vr, bg.var_n);

                                long[][] mtx = bg.MatrixMultiMatrix(pair.Matrix, matrix, bfm.var_m);
                                long[] xi = bg.MatrixToVector(mtx);
                                LeadVector x = new LeadVector(xi);
                                if (x.Lidx >= 0) // neni to nulovy vektor
                                {
                                    if (to.FunctionGSet.AddVector(x))
                                    {
                                        if (printG)
                                            to.FunctionGSet.Print();

                                        w_queue.Enqueue(new NodeMatrix { Node = to, Matrix = mtx });
                                    }
                                }
                                i++;
                            }
                        }
                    }
                }
            }
            // algoritmus 5
            foreach (string fncName in prg.LastNode.Keys)
            {
                IaNode last = prg.LastNode[fncName];

                List<long[][]> mtxs = new List<long[][]>();
                int i = 0;
                while (last.FunctionGSet.GArr[i] != null)
                {
                    mtxs.Add(bfm.VectorToMatrix(last.FunctionGSet.GArr[i].Vr, bg.var_n));
                    i++;
                }
                if (mtxs.Count == 0)
                    throw new ApplicationException();

                foreach (IaEdge edge in fncCallEdges)
                {
                    if ((edge.Ast == null) || (edge.Ast.AstType != AstNodeTypes.FunctionCall))
                        throw new ApplicationException();

                    if (edge.Ast.TokenText == last.FncName)
                    {
                        edge.MatrixSet.TMatrixes.Clear();
                        edge.MatrixSet.TMatrixes.AddRange(mtxs);
                    }
                }
            }
        }

        #endregion Creating Function Call Matrixes

        #region Creating Generator Sets

        private void AddIdentityVectors(Queue<NodeVector> w_queue, IaNode node)
        {
            long[][] id = bg.GetIdentity(bg.var_n);
            for (int i = 0; i < bg.var_n; i++)
                w_queue.Enqueue(new NodeVector { Node = node, Vector = new LeadVector(id[i]) });
        }

        private void CreateEmptyG(ProgramAst prg)
        {
            foreach (IaNode node in prg.Graph.Values)
            {
                Queue<IaNode> q = new Queue<IaNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    IaNode n = q.Dequeue();
                    if (n.GeneratorSet == null)
                    {
                        n.GeneratorSet = new GeneratorSet(n, bg);
                        foreach (IaEdge edge in n.Edges)
                            if (edge.To.GeneratorSet == null)
                                q.Enqueue(edge.To);
                    }
                }
            }
        }

        private void CreateGeneratorSets(ProgramAst prg)
        {
            CreateEmptyG(prg);

            Queue<NodeVector> w_queue = new Queue<NodeVector>();
            AddIdentityVectors(w_queue, prg.Graph["main"]);

            while (w_queue.Count > 0)
            {
                NodeVector pair = w_queue.Dequeue();

                IaNode from = pair.Node;
                foreach (IaEdge edge in from.Edges)
                {
                    IaNode to = edge.To;

                    if ((edge.Ast != null) && (edge.Ast.AstType == AstNodeTypes.FunctionCall))
                    {
                        IaNode fncBegin = prg.Graph[edge.Ast.TokenText];
                        if (fncBegin.GeneratorSet.AddVector(pair.Vector))
                        {
                            if (printG)
                                fncBegin.GeneratorSet.Print();

                            w_queue.Enqueue(new NodeVector { Node = fncBegin, Vector = pair.Vector });
                        }
                    }

                    foreach (long[][] a_mtx in edge.MatrixSet.TMatrixes)
                    {
                        long[] xi = bg.MatrixMultiVector(a_mtx, pair.Vector.Vr, bg.var_m);
                        LeadVector x = new LeadVector(xi);
                        if (x.Lidx >= 0) // neni to nulovy vektor
                        {
                            if (to.GeneratorSet.AddVector(x))
                            {
                                if (printG)
                                    to.GeneratorSet.Print();

                                w_queue.Enqueue(new NodeVector { Node = to, Vector = x });
                            }
                        }
                    }
                }
            }
        }

        #endregion Creating Generator Sets

        #region Creating Linear Equations

        private void CreateLinearEquations(ProgramAst prg)
        {
            foreach (IaNode node in prg.Graph.Values)
            {
                Queue<IaNode> q = new Queue<IaNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    IaNode n = q.Dequeue();
                    if (n.LinearEquations == null)
                    {
                        n.LinearEquations = new LinearEquations(n, bg, printLE);
                        n.LinearEquations.CalculateLE();

                        foreach (IaEdge edge in n.Edges)
                            if (edge.To.LinearEquations == null)
                                q.Enqueue(edge.To);
                    }
                }
            }
        }

        #endregion Creating Linear Equations

        public void Analyze(ProgramAst prg)
        {
            CreateTransitionMatrixes(prg);
            CreateFunctionMatrixes(prg);
            CreateGeneratorSets(prg);
            CreateLinearEquations(prg);
        }
    }

    class BaseFunctions
    {
        public readonly int var_w, var_n;
        public readonly long var_m;
        private readonly int prime;
        private readonly int[] r_arr;

        public BaseFunctions(int w, int n)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            var_w = w;
            var_m = 1L << w;
            var_n = n + 1; // velikost matice G... +1 pro konstanty

            prime = GetPrime(var_w);
            r_arr = GetRArray(var_w, prime);
        }

        private int GetPrime(int w)
        {
            int p = w;
            while (!IsPrime(p))
                p++;
            return p;
        }

        private bool IsPrime(int n)
        {
            if (n == 1) return false;
            if (n == 2 || n == 3) return true;
            if ((n & 1) == 0) return false;
            if ((((n + 1) % 6) == 0) && (((n - 1) % 6) == 0)) return false;
            int q = (int)Math.Sqrt(n) + 1;
            for (int v = 3; v < q; v += 2)
                if (n % v == 0)
                    return false;
            return true;
        }

        private int[] GetRArray(int w, int p)
        {
            int[] a = new int[p];
            for (int i = 0; i < w; i++)
            {
                long idx = (long)(1L << i) % p;
                a[idx] = i;
            }
            return a;
        }

        public int Reduction(long nr, out long d)
        {
            if ((nr % 2) != 0) // odd number
            {
                d = nr;
                return 0;
            }

            int r = r_arr[(nr & (-nr)) % prime];
            d = (nr >> r);
            return r;
        }

        private long[][] GetMArray(int k, int l)
        {
            long[][] mx = new long[k][];

            for (int i = 0; i < l; i++)
                mx[i] = new long[l];
            return mx;
        }

        public long[][] GetEmpty(int n)
        {
            return GetMArray(n, n);
        }

        public long[][] GetIdentity(int n)
        {
            long[][] mx = GetMArray(n, n);
            for (int i = 0; i < n; i++)
                mx[i][i] = 1;
            return mx;
        }

        public long[] MatrixMultiVector(long[][] matrix, long[] vector, long mod)
        {
            int z = matrix.Length;
            if (z != vector.Length)
                throw new ApplicationException();

            int l = matrix[0].Length;
            long[] result = new long[l];
            for (int j = 0; j < l; j++)
                for (int a = 0; a < z; a++)
                    result[j] = (result[j] + matrix[a][j] * vector[a]) % mod;

            return result;
        }

        public long[][] MatrixMultiMatrix(long[][] left, long[][] right, long mod)
        {
            int z = left.Length;
            if (z != right[0].Length)
                throw new ApplicationException();

            int k = right.Length;
            int l = left[0].Length;
            long[][] result = GetMArray(k, l);

            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    long sum = 0;
                    for (int a = 0; a < k; a++)
                        sum = (sum + left[a][j] * right[i][a]) % mod;
                    result[i][j] = sum;
                }
            }

            return result;
        }

        public long[] MatrixToVector(long[][] matrix)
        {
            int k = matrix.Length;
            int l = matrix[0].Length;
            long[] vector = new long[k * l];

            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    vector[j + i * l] = matrix[i][j];
                }
            }

            return vector;
        }

        public long[][] VectorToMatrix(long[] vector, int n)
        {
            if (vector.Length < n)
                throw new ApplicationException();

            long[][] matrix = GetMArray(n, n);

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matrix[i][j] = vector[j + i * n];
                }
            }

            return matrix;
        }
    }

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
                    if (vu != vi) // pouze pokud jsou promenne rozdilne
                    {
                        if (((c == 1) && (s == 1)) || ((c > 1) && (s > 1)))
                            s = 1;
                        else
                            s = var_m - 1;
                        mtx[0][vi] = (mtx[0][vi] + s) % var_m; // ++x => zvysi se o 1 ihned => ma vliv na promennou prirazeni
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
            // neznamy vyraz
            long[][] mtx = null;
            mtx = b.GetIdentity(var_n);
            mtx[vi][vi] = 0;
            TMatrixes.Add(mtx); // vyraz.. x_? = 0
            mtx = b.GetIdentity(var_n);
            mtx[vi][vi] = 0;
            mtx[0][vi] = 1;
            TMatrixes.Add(mtx); // vyraz.. x_? = 1
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
                        // tady tedy vubec nevim, jak obecny AST prevest na tu matici :-(...

                        // umi to pouze vyraz typu x_? = c_0 + c_1 * x_1 + .. + c_n * x_n (pripadne scitance nejak zprehazene)
                        long[][] mtx = b.GetIdentity(var_n);

                        mtx[vi][vi] = 0; // vynulovat 1 na diagonale pro cilovou promennou
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
            else
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

    class LeadVector
    {
        private readonly long[] vr;
        private readonly int li;

        public LeadVector(long[] vr)
        {
            this.vr = vr;
            li = GetLeadIndex(vr);
        }

        private int GetLeadIndex(long[] vr)
        {
            int k = vr.Length, li = -1;
            for (int i = 0; i < k; i++)
                if (vr[i] != 0)
                {
                    li = i;
                    break;
                }
            return li;
        }

        public long[] Vr
        {
            get { return vr; }
        }

        public int Lidx
        {
            get { return li; }
        }

        public long Lentry   // jen jsem to prejmenovala dle terminologie toho clanku
        {
            get
            {
                if ((li >= 0) && (li < vr.Length))
                    return vr[li];
                return 0;
            }
        }
    }

    class GeneratorSet
    {
        protected readonly string name;
        protected readonly IaNode parent;
        protected readonly BaseFunctions b;

        protected readonly int var_w, var_n;
        protected readonly long var_m;

        public GeneratorSet(IaNode parent, BaseFunctions b)
            : this("G", parent, b) { }

        public GeneratorSet(string name, IaNode parent, BaseFunctions b)
        {
            this.name = name;

            this.parent = parent;
            this.b = b;

            var_w = b.var_w;
            var_n = b.var_n;
            var_m = b.var_m;

            GArr = new LeadVector[var_n];
        }

        public LeadVector[] GArr { get; private set; }

        private void RemoveVector(int ri)
        {
            int i = ri;
            while (i < (var_n - 1) && (GArr[i + 1] != null)) // pri mazani vektoru, je treba posunout vsechny vektory zprava o 1 pozici
            {
                GArr[i] = GArr[i + 1];
                i++;
            }
            GArr[i] = null; // posledni vektor je null -> konec G
        }

        private void InsertVector(int ii, LeadVector vector)
        {
            if (GArr[ii] != null)
            {
                if (GArr[ii].Lidx > vector.Lidx)
                {
                    int i = var_n - 2;
                    while (i >= ii)
                    {
                        GArr[i + 1] = GArr[i];
                        i--;
                    }
                    GArr[ii] = vector;
                    return;
                }
                else
                {
                    // tady to chce asi vyjimku, podle me to nemuze nastat!!!
                    throw new ApplicationException();
                }
            }
            GArr[ii] = vector; // jednoduse pridani vektoru na konec :-)
        }

        private void AddEven(LeadVector tvr)
        {
            int r;
            long d;
            r = b.Reduction(tvr.Lentry, out d);

            long x = 1L << (var_w - r);

            int l = tvr.Vr.Length;
            long[] wr = new long[l];
            for (int i = 0; i < l; i++)
                wr[i] = (x * tvr.Vr[i]) % var_m;

            LeadVector twr = new LeadVector(wr);
            if (twr.Lidx >= 0)
                AddVector(twr);
        }

        public bool AddVector(LeadVector tvr)
        {
            if (tvr.Lidx < 0) // nulovy vektor
                return false;

            int i = 0;

            while (GArr[i] != null)
            {
                if (GArr[i].Lidx >= tvr.Lidx)
                    break;
                i++;
            }

            if (GArr[i] == null) // pridat vektor na konec G
            {
                if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                    AddEven(tvr);

                GArr[i] = tvr;

                return true;
            }
            else if (GArr[i].Lidx == tvr.Lidx)
            {
                bool change = false;  // potrebujeme sledovat, jestli doslo k vlozeni nejakeho vektoru

                int rv, rg;
                long dv, dg;
                rg = b.Reduction(GArr[i].Lentry, out dg);
                rv = b.Reduction(tvr.Lentry, out dv);

                if (rg > rv)
                {
                    LeadVector tmpx = GArr[i];
                    RemoveVector(i);

                    change = true; // byla provedena zmena
                    if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                        AddEven(tvr);

                    InsertVector(i, tvr);

                    tvr = tmpx;

                    long td = dg;
                    dg = dv;
                    dv = td;

                    int tr = rg;
                    rg = rv;
                    rv = tr;

                }

                // univerzalni vzorec pro pripad rg <= rv (proto ta zmena znaceni)
                long x = (long)(1L << rv - rg) * dv;

                int l = tvr.Vr.Length;
                long[] wr = new long[l];
                for (int j = 0; j < l; j++)
                    wr[j] = (((dg * tvr.Vr[j]) - (x * GArr[i].Vr[j])) % var_m + var_m) % var_m; // 2x modulo pro odstraneni zapornych cisel pod odecitani

                LeadVector twr = new LeadVector(wr);
                if (twr.Lidx >= 0)
                    change |= AddVector(twr);

                return change;
            }
            else if (GArr[i].Lidx > tvr.Lidx)
            {
                InsertVector(i, tvr);
                return true;
            }

            return false;
        }

        public void Print()
        {
            Console.WriteLine("{0} v uzlu '{1}':", name, parent.Name);

            if ((GArr == null) || (GArr[0] == null))
            {
                Console.WriteLine("prazdna mnozina");
                return;
            }

            int k = GArr.Length;
            int l = GArr[0].Vr.Length;

            for (int j = 0; j < l; j++)
            {
                int i = 0;
                while ((i < k) && (GArr[i] != null))
                {
                    Console.Write("{0} ", GArr[i].Vr[j]);
                    i++;
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }

    class LinearEquations : GeneratorSet
    {
        private bool print;

        private long[][] A;
        private long[][] T;

        public LinearEquations(IaNode parent, BaseFunctions b, bool print)
            : base("GLa", parent, b)
        {
            this.print = print;
        }

        private void Preset()
        {
            A = b.GetEmpty(var_n);

            GeneratorSet g = parent.GeneratorSet;
            int gi = 0;
            for (int i = 0; i < var_n; i++)
            {
                if ((g.GArr[gi] != null) && (g.GArr[gi].Lidx == i))
                {
                    for (int j = 0; j < var_n; j++)
                        A[i][j] = g.GArr[gi].Vr[j];
                    gi++;
                }
                // else -> ponechej nulovy vektor
            }

            T = b.GetIdentity(var_n);
        }

        private void GetPivot(long[][] A, int di, out int pi, out int pj, out long pd, out int pr)
        {
            long d;
            int r;
            pr = int.MaxValue;
            pd = 0;
            pi = -1;
            pj = -1;
            for (int j = di; j < var_n; j++)
                for (int i = di; i < var_n; i++)
                    if (A[i][j] > 0)
                    {
                        r = b.Reduction(A[i][j], out d);
                        if (r < pr)
                        {
                            pi = i;
                            pj = j;
                            pd = d;
                            pr = r;
                        }
                    }
        }

        private void ClearColumn(long[][] A, int pi, int pj, long pd, int pr)
        {
            for (int j = 0; j < var_n; j++)
                if ((j != pj) && (A[pi][j] > 0))
                {
                    long alpha = A[pi][j] >> pr;

                    for (int i = 0; i < var_n; i++)
                    {
                        long y = ((A[i][j] * pd - A[i][pj] * alpha) % var_m + var_m) % var_m;
                        A[i][j] = y;
                    }
                }
        }

        private void ClearRow(long[][] A, long[][] T, int pi, int pj, long pd, int pr)
        {
            for (int i = 0; i < var_n; i++)
                if ((i != pi) && (A[i][pj] > 0))
                {
                    long alpha = A[i][pj] >> pr;

                    for (int j = 0; j < var_n; j++)
                    {
                        long y = ((A[i][j] * pd - A[pi][j] * alpha) % var_m + var_m) % var_m;
                        A[i][j] = y;
                        long z = ((T[i][j] * pd - T[pi][j] * alpha) % var_m + var_m) % var_m;
                        T[i][j] = z;
                    }
                }
        }

        private void ExchangeColumns(long[][] A, long[][] T, int di, int pi)
        {
            if (di == pi)
                return;

            long t;
            for (int j = 0; j < var_n; j++)
            {
                t = A[pi][j];
                A[pi][j] = A[di][j];
                A[di][j] = t;

                t = T[pi][j];
                T[pi][j] = T[di][j];
                T[di][j] = t;
            }
        }

        private void ExchangeRows(long[][] A, long[][] T, int di, int pj)
        {
            if (di == pj)
                return;

            long t;
            for (int i = 0; i < var_n; i++)
            {
                t = A[i][pj];
                A[i][pj] = A[i][di];
                A[i][di] = t;
            }
        }

        private void GetAT()
        {
            if (print)
            {
                PrintMatrix("A", A);
                PrintMatrix("T", T);
            }

            for (int di = 0; di < (var_n - 1); di++) // indexy diagonaly
            {
                int pi, pj; // indexy pivotu
                long pd; // d pivotu
                int pr; // r pivotu

                GetPivot(A, di, out pi, out pj, out pd, out pr);

                if ((pi < 0) || (pj < 0)) // ve zbyvajici matici jiz neni nenulovy prvek
                    break;

                ClearColumn(A, pi, pj, pd, pr);

                ClearRow(A, T, pi, pj, pd, pr);

                ExchangeColumns(A, T, di, pi);

                ExchangeRows(A, T, di, pj);

                if (print)
                {
                    PrintMatrix("A", A);
                    PrintMatrix("T", T);
                }
            }
        }

        private void GetG()
        {
            for (int di = 0; di < var_n; di++) // indexy diagonaly
            {
                long d;
                int r;
                r = b.Reduction(A[di][di], out d);

                long[] li = new long[var_n];
                li[di] = (1L << r);

                long[] xi = b.MatrixMultiVector(T, li, var_m);

                AddVector(new LeadVector(xi));
            }

            if (print)
                Print();
        }

        public void CalculateLE()
        {
            Preset();

            if (print)
                Console.WriteLine("Zacatek vypoctu linearnich rovnic v uzlu '{0}':", parent.Name);

            GetAT();
            GetG();

            if (print)
            {
                Console.WriteLine("Konec vypoctu linearnich rovnic v uzlu '{0}':", parent.Name);
                Console.WriteLine();
            }
        }

        private void PrintMatrix(string name, long[][] mtx)
        {
            Console.WriteLine("{0}:", name);

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
        }
    }

}
