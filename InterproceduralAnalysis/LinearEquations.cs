using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
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
            while ((gi < var_n) && (g.GArr[gi] != null))
            {
                for (int i = 0; i < var_n; i++)
                    A[i][gi] = g.GArr[gi].Vr[i];
                gi++;
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

        private void ExchangeRows(long[][] A, int di, int pj)
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

            for (int di = 0; di < (var_n - 1); di++)
            {
                int pi, pj; // indexy pivotu
                long pd; // d pivotu
                int pr; // r pivotu

                GetPivot(A, di, out pi, out pj, out pd, out pr);

                if ((pi < 0) || (pj < 0))
                    break;

                ClearColumn(A, pi, pj, pd, pr);

                ClearRow(A, T, pi, pj, pd, pr);

                ExchangeColumns(A, T, di, pi);

                ExchangeRows(A, di, pj);

                if (print)
                {
                    PrintMatrix("A", A);
                    PrintMatrix("T", T);
                }
            }
        }

        private void GetG()
        {
            for (int di = 0; di < var_n; di++)
            {
                long d;
                int r;
                r = b.Reduction(A[di][di], out d);

                long[] li = new long[var_n];
                li[di] = (1L << var_w - r);

                long[] xi = b.MatrixMultiVector(T, li, var_m);

                AddVector(new LeadVector(xi));
            }

            if (print)
                Print();
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

        public void CalculateLE()
        {
            //if (parent.GeneratorSet == null)
            //    return;

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

        public string PrintLE(int i, List<string> vars)
        {
            string le = string.Empty;

            long[] a = GArr[i].Vr;
            for (int j = 1; j < a.Length; j++)
            {
                if (a[j] > 0)
                {
                    if (!string.IsNullOrEmpty(le))
                        le += " + ";
                    le += string.Format("{0}*{1}", a[j], vars[j - 1]);
                }
            }
            long c = 0;
            if (a[0] > 0)
                c = var_m - a[0];

            return string.Format("{0} = {1}", le, c);
        }
    }
}
