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

        private readonly LeadVector[] g_act;

        private Queue<WItem> w_queue;

        public InterproceduralAnalyzer(int w, int n)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            this.var_w = w;
            this.var_m = (long)Math.Pow(2, w);
            this.var_n = n + 1; // velikost matice G... +1 pro konstanty

            prime = GetPrime(var_w);
            r_arr = GetRArray(var_w, prime);

            g_act = new LeadVector[var_n];

            w_queue = new Queue<WItem>();
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
                    result[j] += matrix[a][j] * vector[a];

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
                        sum += left[a][j] * right[i][a];
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

        private void AddEven(LeadVector tvr)
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
                AddVector(twr);
        }

        private bool AddVector(LeadVector tvr)
        {
            int i = 0;

            while (g_act[i] != null)
            {
                if (g_act[i].Lidx == tvr.Lidx)
                {
                    bool change = false;  // potrebujeme sledovat, jestli doslo k vlozeni nejakeho vektoru

                    int rv, rg;
                    long dv, dg;
                    rg = Reduction(g_act[i].Lentry, out dg);
                    rv = Reduction(tvr.Lentry, out dv);

                    if (rg > rv)
                    {
                        LeadVector tmpx = g_act[i];
                        g_act[i] = tvr;

                        change = true; // byla provedena zmena
                        if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                            AddEven(tvr);

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
                        wr[j] = (((dg * tvr.Vr[j]) - (x * g_act[i].Vr[j])) % var_m + var_m) % var_m;

                    LeadVector twr = new LeadVector(wr);
                    if (twr.Lidx >= 0)
                        change |= AddVector(twr);

                    return change;
                }
                else if (g_act[i].Lidx > tvr.Lidx) // toto nevim, zda muze nastat
                {
                    return false;
                }
                i++;
            }

            // pridani vektoru na konec G

            if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                AddEven(tvr);

            g_act[i] = tvr;

            return true;
        }

        private void AddIdentityVectors(Queue<WItem> w_queue, IaNode node)
        {
            for (int i = 0; i < var_n; i++)
                w_queue.Enqueue(new WItem { Node = node, Vector = new LeadVector(node.GeneratorSet[i]) });
        }

        public void Analyze(ProgramAst prg)
        {
            IaNode first = prg.Graph["main"]; // pro pokusy to ted staci :-)... pak se to musi upravit
            first.GeneratorSet = GetIdentity();
            AddIdentityVectors(w_queue, first);

            while (w_queue.Count > 0)
            {
                WItem pair = w_queue.Dequeue();

                // zde by se mela vzit matice G ze vstupniho uzlu!!!

                // tak ty hrany budu muset taky upravit... prozatim vim, ze je tam pouze hrana Next s jednou matici prechodu A
                // tady musi byt smycka pro pruchod vsemi hranami vystupujicimi ze vstupniho uzlu a vsemi maticemi na hrane
                long[][] a_mtx = pair.Node.Next.MatrixSet[0];

                long[] xi = MatrixMultiVector(a_mtx, pair.Vector.Vr);
                LeadVector x = new LeadVector(xi);
                if (AddVector(x))
                {
                    w_queue.Enqueue(new WItem { Node = pair.Node, Vector = x });
                }

                // zde by se mela ulozit matice G do vystupniho uzlu

                // zde smycky budou koncit
            }
        }

        // pro debugovaci tisk na obrazovku... bude smazano

        public long[][] Mx
        {
            get { return GetMx(); }
        }

        private long[][] GetMx()
        {
            long[][] mx = GetMArray(var_n, var_n);
            for (int i = 0; i < var_n; i++)
                for (int j = 0; j < var_n; j++)
                    mx[i][j] = g_act[i].Vr[j];
            return mx;
        }
    }

    class WItem
    {
        public IaNode Node { get; set; }
        public LeadVector Vector { get; set; }
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
}
