using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class ComputeMatrix
    {
        protected readonly int w, n;
        protected readonly long m;
        private readonly int p;
        private readonly int[] a;

        public ComputeMatrix(int w, int n)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            this.w = w;
            this.m = (long)Math.Pow(2, w);
            this.n = n + 1; // velikost matice G... +1 pro konstanty

            p = GetPrime(w);
            a = GetRArray(w, p);
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

        public int Reduction(long nr, out int d)
        {
            if ((nr % 2) != 0)
            {
                d = (int)nr;
                return 0;
            }
            int r = a[(nr & (-nr)) % p];
            d = (int)(nr >> r);
            return r;
        }

        public long[][] GetEmpty()
        {
            return GetMArray(n, n);
        }

        public long[][] GetIdentity()
        {
            long[][] mx = GetMArray(n, n);
            for (int i = 0; i < n; i++)
                mx[i][i] = 1;
            return mx;
        }

        public long[] Multiplication(long[][] mx, long[] vr)
        {
            int z = mx.Length;
            if (z != vr.Length)
                throw new ApplicationException();

            int l = mx[0].Length;
            long[] wr = new long[l];
            for (int j = 0; j < l; j++)
                for (int a = 0; a < z; a++)
                    wr[j] += mx[a][j] * vr[a];

            return wr;
        }

        public long[][] Multiplication(long[][] mx, long[][] nx)
        {
            int z = mx.Length;
            if (z != nx[0].Length)
                throw new ApplicationException();

            int k = nx.Length;
            int l = mx[0].Length;
            long[][] r = GetMArray(k, l);

            for (int i = 0; i < k; i++)
            {
                for (int j = 0; j < l; j++)
                {
                    long sum = 0;
                    for (int a = 0; a < k; a++)
                        sum += mx[a][j] * nx[i][a];
                    r[i][j] = sum;
                }
            }

            return r;
        }

        private long[][] GetMArray(int k, int l)
        {
            long[][] mx = new long[k][];
            for (int i = 0; i < l; i++)
                mx[i] = new long[l];
            return mx;
        }
    }

    class TempVector
    {
        private readonly long[] vr;
        private readonly int li;

        public TempVector(long[] vr)
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

        public long Litem
        {
            get
            {
                if ((li >= 0) && (li < vr.Length))
                    return vr[li];
                return 0;
            }
        }
    }

    class RMatrix : ComputeMatrix
    {
        private readonly TempVector[] tmx;
        private readonly long[][] mx;

        public RMatrix(int w, int n)
            : base(w, n)
        {
            mx = GetEmpty();
            tmx = new TempVector[mx.Length];
        }

        public long[][] Mx
        {
            get { return GetMx(); }
        }

        private long[][] GetMx()
        {
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    mx[i][j] = tmx[i].Vr[j];
            return mx;
        }

        public bool AddVector(TempVector tvr)
        {
            TempVector tmpv = tvr; // potrebujeme uchovat informaci o hodnotach puvodne vkladaneho vektoru
            bool change = false;  // potrebujeme sledovat, jestli doslo k vlozeni nejakeho vektoru
            int i = 0;
            while (tmx[i] != null)
            {
                if (tmx[i].Lidx == tvr.Lidx)
                {
                    int dg, rg;
                    rg = Reduction(tmx[i].Litem, out dg);
                    int dv, rv;
                    rv = Reduction(tvr.Litem, out dv);

                    if (rg > rv)
                    {
                        TempVector tmpx = tmx[i];
                        tmx[i] = tvr; 
                        change = true; // byla provedena zmena
                        tvr = tmpx;
                        
                        int tmp = dg;
                        dg = dv;
                        dv = tmp;

                        tmp = rg;
                        rg = rv;
                        rv = tmp;
                        
                    }

                    // univerzalni vzorec pro pripad rg <= rv (proto ta zmena znaceni)
                    int x = (int)Math.Pow(2, rv - rg) * dv;

                    int l = tvr.Vr.Length;
                    long[] wr = new long[l];
                    for (int j = 0; j < l; j++)
                        wr[j] = (((dg * tvr.Vr[j]) - (x * tmx[i].Vr[j])) % m + m) % m;

                    TempVector twr = new TempVector(wr);
                    if (twr.Lidx >= 0)
                        AddVector(twr);

                    return true;
                }
                else if (tmx[i].Lidx > tvr.Lidx) // toto nevim, zda muze nastat
                {
                    return false;
                }
                i++;
            }
            tmx[i] = tvr;
            change = true;
            if ((tvr.Litem != 0) && ((tvr.Litem % 2) == 0))
                AddEven(tvr);

            if (change == true)
            {
                // toto znamena, ze puvodni vektor tvr (ulozeny do promenne tmpv) zpusobil v danem uzlu zmenu v mnozine vektoru
                // proto => queueW.Add(tmpv)
            }

            return true;
        }

        private void AddEven(TempVector tvr)
        {
            int d, r;
            r = Reduction(tvr.Litem, out d);

            int x = (int)Math.Pow(2, w - r);

            int l = tvr.Vr.Length;
            long[] wr = new long[l];
            for (int i = 0; i < l; i++)
                wr[i] = (x * tvr.Vr[i]) % m;

            TempVector twr = new TempVector(wr);
            if (twr.Lidx >= 0)
                AddVector(twr);
        }
    }
}
