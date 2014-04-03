using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class InterproceduralAnalyzer
    {
        protected readonly int var_w, var_n;
        protected readonly long var_m;
        private readonly int prime;
        private readonly int[] r_arr;

        private Queue<QueueItem> w_queue;

        public InterproceduralAnalyzer(int w, int n)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            this.var_w = w;
            this.var_m = (long)Math.Pow(2, w);
            this.var_n = n + 1; // velikost matice G... +1 pro konstanty

            prime = GetPrime(var_w);
            r_arr = GetRArray(var_w, prime);

            w_queue = new Queue<QueueItem>();
        }

        #region Creating Transition Matrixes

        private bool GetConst(BaseAst node, List<string> vars, out int vii, out long c)
        {
            vii = 0;
            c = 0;

            if ((node.AstType == AstNodeTypes.Number) && (node is NumberAst))
            {
                c = (node as NumberAst).Number;
            }
            else if (node.AstType == AstNodeTypes.Variable)
            {
                vii = vars.IndexOf(node.TokenText) + 1;
                c = 1;
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
                    c = ((num as NumberAst).Number + var_m) % var_m;
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

        private bool ProceedExpr(BaseAst top, long[][] mtx, int vi, List<string> vars)
        {
            BaseAst node = top;
            int vii;
            long c;
            bool isError = false;

            while ((node != null) && (!isError))
            {
                if ((node.AstType == AstNodeTypes.Number) ||
                    (node.AstType == AstNodeTypes.Variable) ||
                    ((node.AstType == AstNodeTypes.Operator) && (node.Token == TokenTypes.Multi)))
                {
                    if (isError = !GetConst(node, vars, out vii, out c))
                        break;
                    mtx[vii][vi] = (mtx[vii][vi] + c) % var_m;
                    node = null;
                }
                else if ((node.AstType == AstNodeTypes.Operator) && (node is OperatorAst) && ((node.Token == TokenTypes.Plus) || (node.Token == TokenTypes.Minus)))
                {
                    BaseAst left = (node as OperatorAst).Left;
                    BaseAst right = (node as OperatorAst).Right;

                    if ((left.AstType == AstNodeTypes.Operator) && ((left.Token == TokenTypes.Plus) || (left.Token == TokenTypes.Minus)))
                    {
                        BaseAst tmp = left;
                        left = right;
                        right = tmp;
                    }

                    if ((left.AstType == AstNodeTypes.Number) ||
                        (left.AstType == AstNodeTypes.Variable) ||
                        ((left.AstType == AstNodeTypes.Operator) && (left.Token == TokenTypes.Multi)))
                    {
                        if (isError = !GetConst(left, vars, out vii, out c))
                            break;
                        mtx[vii][vi] = (mtx[vii][vi] + c) % var_m;
                    }
                    else
                    {
                        isError = true;
                        break;
                    }

                    node = right;
                }
                else
                {
                    isError = true;
                    break;
                }
            }
            return !isError;
        }

        private void GetMatrix(List<long[][]> ml, OperatorAst expr, List<string> vars)
        {
            long[][] mtx = GetIdentity();

            if ((expr != null) && (expr.Token == TokenTypes.Equals))
            {
                int vi = vars.IndexOf(expr.Left.TokenText) + 1;
                if ((vi > 0) && (vi <= vars.Count))
                {
                    // tady tedy vubec nevim, jak obecny AST prevest na tu matici :-(...

                    // umi to pouze vyraz typu x_? = c_0 + c_1 * x_1 + .. + c_n * x_n (pripadne scitance nejak zprehazene)

                    mtx[vi][vi] = 0; // vynulovat 1 na diagonale pro cilovou promennou
                    if (ProceedExpr(expr.Right, mtx, vi, vars))
                        ml.Add(mtx);
                    else
                    {
                        mtx = GetIdentity();
                        mtx[vi][vi] = 0;
                        ml.Add(mtx); // vyraz.. x_? = 0
                        mtx = GetIdentity();
                        mtx[0][vi] = 1;
                        ml.Add(mtx); // vyraz.. x_? = 1
                    }
                }
            }
        }

        #endregion Creating Transition Matrixes

        #region Creating Generator Sets

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
                long idx = (long)(Math.Pow(2, i)) % p;
                a[idx] = i;
            }
            return a;
        }

        private long[][] GetMArray(int k, int l)
        {
            long[][] mx = new long[k][];

            for (int i = 0; i < l; i++)
                mx[i] = new long[l];
            return mx;
        }

        private int Reduction(long nr, out long d)
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

        private long[][] GetIdentity()
        {
            long[][] mx = GetMArray(var_n, var_n);
            for (int i = 0; i < var_n; i++)
                mx[i][i] = 1;
            return mx;
        }

        private long[] MatrixMultiVector(long[][] matrix, long[] vector)
        {
            int z = matrix.Length;
            if (z != vector.Length)
                throw new ApplicationException();

            int l = matrix[0].Length;
            long[] result = new long[l];
            for (int j = 0; j < l; j++)
                for (int a = 0; a < z; a++)
                    result[j] = (result[j] + matrix[a][j] * vector[a]) % var_m;

            return result;
        }

        private long[][] MatrixMultiMatrix(long[][] left, long[][] right)
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
                        sum = (sum + left[a][j] * right[i][a]) % var_m;
                    result[i][j] = sum;
                }
            }

            return result;
        }

        //public long[] ConvertMatrixToVector(long[][] matrix)
        //{
        //    int k = matrix[0].Length;
        //    int l = matrix[1].Length;
        //    long[] vector = new long[k*l];

        //    for (int i = 0; i < k; i++)
        //    {
        //        for (int j = 0; j < l; j++)
        //        {
        //            vector[j + i * l] = matrix[i][j];
        //        }
        //    }

        //    return vector;
        //}

        //public long[][] ConvertVectorToMatrix(long[] vector)
        //{
        //    long[][] matrix = new long[var_n][];

        //    for (int i = 0; i < var_n; i++)
        //    {
        //        for (int j = 0; j < var_n; j++)
        //        {
        //            matrix[i][j] = vector[j + i * var_n];
        //        }
        //    }

        //    return matrix;
        //}

        //public RMatrix(int w, int n)
        //    : base(w, n)
        //{
        //    mx = GetEmpty();
        //    tmx = new TempVector[mx.Length];
        //}

        private void RemoveVector(LeadVector[] g_act, int ri)
        {
            int i = ri;
            while (i < (var_n - 1) && (g_act[i + 1] != null)) // pri mazani vektoru, je treba posunout vsechny vektory zprava o 1 pozici
            {
                g_act[i] = g_act[i + 1];
                i++;
            }
            g_act[i] = null; // posledni vektor je null -> konec G
        }

        private void InsertVector(LeadVector[] g_act, int ii, LeadVector vector)
        {
            if (g_act[ii] != null)
            {
                if (g_act[ii].Lidx > vector.Lidx)
                {
                    int i = var_n - 2;
                    while (i >= ii)
                    {
                        g_act[i + 1] = g_act[i];
                        i--;
                    }
                    g_act[ii] = vector;
                    return;
                }
                else
                {
                    // tady to chce asi vyjimku, podle me to nemuze nastat!!!
                    throw new ApplicationException();
                }
            }
            g_act[ii] = vector; // jednoduse pridani vektoru na konec :-)
        }

        private void AddEven(LeadVector[] g_act, LeadVector tvr)
        {
            int r;
            long d;
            r = Reduction(tvr.Lentry, out d);

            int x = (int)Math.Pow(2, var_w - r);

            int l = tvr.Vr.Length;
            long[] wr = new long[l];
            for (int i = 0; i < l; i++)
                wr[i] = (x * tvr.Vr[i]) % var_m;

            LeadVector twr = new LeadVector(wr);
            if (twr.Lidx >= 0)
                AddVector(g_act, twr);
        }

        private bool AddVector(LeadVector[] g_act, LeadVector tvr)
        {
            int i = 0;

            while (g_act[i] != null)
            {
                if (g_act[i].Lidx >= tvr.Lidx)
                    break;
                i++;
            }

            if (g_act[i] == null) // pridat vektor na konec G
            {
                if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                    AddEven(g_act, tvr);

                g_act[i] = tvr;

                return true;
            }
            else if (g_act[i].Lidx == tvr.Lidx)
            {
                bool change = false;  // potrebujeme sledovat, jestli doslo k vlozeni nejakeho vektoru

                int rv, rg;
                long dv, dg;
                rg = Reduction(g_act[i].Lentry, out dg);
                rv = Reduction(tvr.Lentry, out dv);

                if (rg > rv)
                {
                    LeadVector tmpx = g_act[i];
                    RemoveVector(g_act, i);

                    change = true; // byla provedena zmena
                    if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                        AddEven(g_act, tvr);

                    InsertVector(g_act, i, tvr);

                    tvr = tmpx;

                    long td = dg;
                    dg = dv;
                    dv = td;

                    int tr = rg;
                    rg = rv;
                    rv = tr;

                }

                // univerzalni vzorec pro pripad rg <= rv (proto ta zmena znaceni)
                int x = (int)Math.Pow(2, rv - rg) * (int)dv;

                int l = tvr.Vr.Length;
                long[] wr = new long[l];
                for (int j = 0; j < l; j++)
                    wr[j] = (((dg * tvr.Vr[j]) - (x * g_act[i].Vr[j])) % var_m + var_m) % var_m; // 2x modulo pro odstraneni zapornych cisel pod odecitani

                LeadVector twr = new LeadVector(wr);
                if (twr.Lidx >= 0)
                    change |= AddVector(g_act, twr);

                return change;
            }
            else if (g_act[i].Lidx > tvr.Lidx)
            {
                InsertVector(g_act, i, tvr);
                return true;
            }

            return false;
        }

        private void AddIdentityVectors(Queue<QueueItem> w_queue, IaNode node)
        {
            long[][] id = GetIdentity();
            for (int i = 0; i < var_n; i++)
                w_queue.Enqueue(new QueueItem { Node = node, Vector = new LeadVector(id[i]) });
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
                        n.GeneratorSet = new LeadVector[var_n];
                        foreach (IaEdge edge in n.Edges)
                            q.Enqueue(edge.To);
                    }
                }
            }
        }

        #endregion Creating Generator Sets

        public void CreateTransitionMatrixes(ProgramAst prg)
        {
            IaNode node = prg.Graph["main"]; // pro pokusy to ted staci :-)... pak se to musi rozsirit i na inicializovane VAR

            while (node != null)
            {
                IaEdge edge = node.Next; // zatim jde o linearni funkci

                if (edge != null)
                {
                    if (edge.Ast is OperatorAst)
                    {
                        GetMatrix(edge.MatrixSet, edge.Ast as OperatorAst, prg.Vars);
                    }
                    else
                    {
                        edge.MatrixSet.Add(GetIdentity());
                    }

                    node = edge.To;
                }
                else
                    node = null;
            }
        }

        public void CreateGeneratorSets(ProgramAst prg)
        {
            CreateEmptyG(prg);
            IaNode first = prg.Graph["main"]; // pro pokusy to ted staci :-)... pak se to musi rozsirit i na inicializovane VAR
            AddIdentityVectors(w_queue, first);

            while (w_queue.Count > 0)
            {
                QueueItem pair = w_queue.Dequeue();

                IaNode from = pair.Node;
                // zde musi byt kontrola, zda se nejedna o volani funkce... pokud ano, je treba pridat hranu do W
                foreach (IaEdge edge in from.Edges)
                {
                    IaNode to = edge.To;

                    foreach (long[][] a_mtx in edge.MatrixSet)
                    {
                        long[] xi = MatrixMultiVector(a_mtx, pair.Vector.Vr);
                        LeadVector x = new LeadVector(xi);
                        if (x.Lidx >= 0) // neni to nulovy vektor
                        {
                            if (AddVector(to.GeneratorSet, x))
                            {
                                w_queue.Enqueue(new QueueItem { Node = to, Vector = x });
                            }
                        }
                    }
                }
            }
        }

        public void PrintLastG(ProgramAst prg)
        {
            IaNode node = prg.Graph["main"]; // pro pokusy to ted staci :-)... pak se to musi rozsirit i na inicializovane VAR

            while (node != null)
            {
                IaEdge edge = node.Next; // zatim jde o linearni funkci

                if (edge != null)
                {
                    node = edge.To;
                }
                else
                {
                    PrintMatrix(node.GeneratorSet);
                    node = null;
                }
            }

        }

        private void PrintMatrix(LeadVector[] m)
        {
            if (m[0] == null)
            {
                Console.WriteLine("null");
                return;
            }

            int k = m.Length;
            int l = m[0].Vr.Length;

            for (int j = 0; j < l; j++)
            {
                int i = 0;
                while ((i < k) && (m[i] != null))
                {
                    Console.Write("{0} ", m[i].Vr[j]);
                    i++;
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

    }
}
