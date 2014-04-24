using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
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
            while (i < (var_n - 1) && (GArr[i + 1] != null)) 
            {
                GArr[i] = GArr[i + 1];
                i++;
            }
            GArr[i] = null; 
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
                    throw new ApplicationException();
                }
            }
            GArr[ii] = vector; 
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
            if (tvr.Lidx < 0) 
                return false;

            int i = 0;

            while (GArr[i] != null)
            {
                if (GArr[i].Lidx >= tvr.Lidx)
                    break;
                i++;
            }

            if (GArr[i] == null) 
            {
                if ((tvr.Lentry != 0) && ((tvr.Lentry % 2) == 0))
                    AddEven(tvr);

                InsertVector(i, tvr);

                return true;
            }
            else if (GArr[i].Lidx == tvr.Lidx)
            {
                bool change = false;  

                int rv, rg;
                long dv, dg;
                rg = b.Reduction(GArr[i].Lentry, out dg);
                rv = b.Reduction(tvr.Lentry, out dv);

                if (rg > rv)
                {
                    LeadVector tmpx = GArr[i];
                    RemoveVector(i);

                    change = true; 
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
}
