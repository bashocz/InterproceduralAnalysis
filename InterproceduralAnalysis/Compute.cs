﻿using System;
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
                if (g_act[ii].Lidx < vector.Lidx)
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
                    wr[j] = (((dg * tvr.Vr[j]) - (x * g_act[i].Vr[j])) % var_m + var_m) % var_m;

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

        public void CreateEmptyG(ProgramAst prg)
        {
            foreach (IaNode node in prg.Graph.Values)
            {
                Queue<IaNode> q = new Queue<IaNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    IaNode n = q.Dequeue();
                    n.GeneratorSet = new LeadVector[var_n];
                    foreach (IaEdge edge in n.Edges)
                        q.Enqueue(edge.To);
                }
            }
        }

        public void Analyze(ProgramAst prg)
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
                        if (AddVector(to.GeneratorSet, x))
                        {
                            w_queue.Enqueue(new QueueItem { Node = pair.Node, Vector = x });
                        }
                    }
                }
            }
        }
    }
}
