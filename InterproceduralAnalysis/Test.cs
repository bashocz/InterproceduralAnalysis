using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class Test
    {
        public void ComputeTest()
        {
            int w = 3;
            int n = 2;
            long[][] a = { new long[] { 1, 5, 0 }, new long[] { 0, 4, 0 }, new long[] { 0, 3, 1 } }; // matice zmeny
            long[][] ru = { new long[] { 1, 5, 0 }, new long[] { 0, 3, 1 }, new long[] { 0, 0, 4 } };

            ComputeMatrix cm = new ComputeMatrix(w, n);

            long[][] rv = cm.GetIdentity(); // toto je W v uzlu V
            this.PrintMatrix(rv);

            // tady je zaklad algoritmu

            RMatrix rm = new RMatrix(w, n);
            for (int i = 0; i <= n; i++)
            {
                long[] x = cm.Multiplication(a, rv[i]);
                rm.AddVector(new TempVector(x));
            }

            // tady je konec algoritmu

            PrintMatrix(rm.Mx);

            Console.ReadKey();
        }

        public void Print(int[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Console.WriteLine("{0} - {1}", i, a[i]);
            }
            Console.WriteLine();
        }

        private void PrintMatrix(long[][] m)
        {
            for (int j = 0; j < m[0].GetLength(0); j++)
            {
                for (int i = 0; i < m.GetLength(0); i++)
                {
                    Console.Write("{0} ", m[i][j]);
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private void Print(long[] x)
        {
            for (int i = 0; i < x.Length; i++)
            {
                Console.WriteLine(x[i]);
            }
            Console.WriteLine();
        }
    }
}
