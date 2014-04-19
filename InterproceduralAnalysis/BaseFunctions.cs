using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
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
            var_n = n + 1; 

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
            a[0] = var_w;
            return a;
        }

        public int Reduction(long nr, out long d)
        {
            if ((nr % 2) != 0) 
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
}
