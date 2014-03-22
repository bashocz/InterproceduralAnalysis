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
            int w = 8;
            int n = 3;
            long[] v = { 2, 4, 3 ,7};
            long[,] a = { { 1, 167, 56, 4 }, { 0, 92, 116, 246 }, { 0, 133, 215, 15 }, { 0, 114, 38, 21 } };
            long[,] b = { { 1, 4, 8, 0 }, { 0, 3, 0, 0 }, { 0, 249, 2, 0 }, { 0, 5, 252, 1 } };

            ComputeReduction cr = new ComputeReduction(w);
            ComputeMatrix cm = new ComputeMatrix(w, n);
            
            long[,] m = cm.GetIdentity();
            this.PrintMatrix(m);

            long[] x = cm.Multiplication(m, v);
            this.Print(x);

            this.PrintMatrix(a);
            x = cm.Multiplication(a, v);
            this.Print(x);

            this.PrintMatrix(b);
            m = cm.Multiplication(a, b);
            this.PrintMatrix(m);

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

        private void PrintMatrix(long[,] m)
        {
            for (int j = 0; j < m.GetLength(1); j++)
            {
                for (int i = 0; i < m.GetLength(0); i++)
                {
                    Console.Write("{0} ", m[i, j]);
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
