using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class ComputeReduction
    {
        private readonly int w, p;
        private readonly int[] a;

        public ComputeReduction(int w)
        {
            if (w <= 0)
                throw new ApplicationException("");

            this.w = w;

            p = GetPrime(w);
            a = GetRArray(w, p);

            /*Test*/
            Test test = new Test();
            test.Print(a);
            /*test*/
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

        public int Reduction(int n, ref int d)
        {
            int r = a[(n & (-n)) % p];
            d = n >> r;
            return r;
        }
    }

    class ComputeMatrix
    {
        private readonly int w, n;

        public ComputeMatrix(int w, int n)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            this.w = w;
            this.n = n + 1; // velikost matice G... +1 pro konstanty
        }

        public long[,] GetIdentity()
        {
            long[,] m = new long[n, n];
            for (int i = 0; i < n; i++)
                m[i, i] = 1;
            return m;
        }

        public long[] Multiplication(long[,] m, long[] v)
        {
            int z = m.GetLength(0);
            if (z != v.Length)
                throw new ApplicationException();

            int l = m.GetLength(1);
            long[] w = new long[l];
            for (int j = 0; j < l; j++)
                for (int a = 0; a < z; a++)
                    w[j] += m[a, j] * v[a];

            return w;
        }

        public long[,] Multiplication(long[,] m, long[,] n)
        {
            int z = m.GetLength(0);
            if (z != n.GetLength(1))
                throw new ApplicationException();

            int k = n.GetLength(0);
            int l = m.GetLength(1);
            long[,] r = new long[k, l];

            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    long sum = 0;
                    for (int a = 0; a < k; a++)
                        sum += m[a, j] * n[i, a];
                    r[i, j] = sum;
                }
            }

            return r;
        }
    }
}
