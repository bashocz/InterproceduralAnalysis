using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace InterproceduralAnalysis
{
    class Test
    {
        private int w = 3;
        private int n = 2;
        public void ComputeTest(ProgramAst program)
        {
            long[][] a = { new long[] { 1, 5, 0 }, new long[] { 0, 4, 0 }, new long[] { 0, 3, 1 } }; // matice zmeny
            long[][] rv = { new long[] { 1, 0, 0 }, new long[] { 0, 1, 0 }, new long[] { 0, 0, 1 } }; // matice vstupniho uzlu
            long[][] ru = { new long[] { 1, 5, 0 }, new long[] { 0, 3, 1 }, new long[] { 0, 0, 4 } }; // matice vystupniho uzlu (pro kontrolu vypoctu)

            IaNode first = new IaNode();
            IaNode second = new IaNode();
            IaEdge edge = new IaEdge();
            edge.MatrixSet.Add(a);
            edge.From = first;
            edge.To = second;
            first.Next = edge;

            ProgramAst prg = new ProgramAst();
            prg.Graph.Add("main", first);



            InterproceduralAnalyzer rm = new InterproceduralAnalyzer(w, n);
            rm.Analyze(prg);



            PrintMatrix(second.GeneratorSet);

            Console.ReadKey();


            // pokus o sestaveni zmenovych matic funkci

            //long[] vector = cm.ConvertMatrixToVector(rv);  // prevod matice na vektor
            //foreach (KeyValuePair<string, IaNode> pair in program.Graph)
            //{
            //    queueW.Enqueue(new QItem(pair.Value, vector));  //vlozeni vstupnich uzlu vsech funkci do fronty s prirazenou jednotkovou matici prevedenou na vektor
            //}

        }

        //private List<FncItem> fncList = new List<FncItem>();
        //public void ComputeMatrixes(ProgramAst program)
        //{
        //    List<long[][]> transformation = new List<long[][]>();
        //    List<long[][]> changed = new List<long[][]>();
        //    long[][] matrix;
        //    long[][] nMatrix;
        //    RMatrix rm = new RMatrix(w, n);
        //    ComputeMatrix cm = new ComputeMatrix (w,n);

        //    while (queueW.Count != 0)
        //    {
        //        QItem item = queueW.Dequeue();
        //        matrix = cm.ConvertVectorToMatrix(item.Vector);
                
        //        IaEdge test = null; //pomocna hrana pro prvni podminku -> jak poznat, ze je v hrane volani funkce?

        //        if ((item.Node.Next == test) || (item.Node.IsTrue == test) || (item.Node.IsFalse == test)) // pokud jde o volani funkce
        //        {
        //            /* 
        //             * do fncList ulozit identifikaci funkce, ktera je v hrane volana
        //             * a identifikaci uzlu, ze ktereho hrana vychazi
        //            */
        //        }
        //            //pokud v hrane neni volani funkce
        //        else if (item.Node.Next != null)
        //        {
        //            transformation = item.Node.Next.MatrixSet;
        //            foreach (long[][] tMatrix in transformation)
        //            {
        //                changed.Add(cm.Multiplication(tMatrix, matrix));
        //            }

        //        }
        //        else if (item.Node.IsTrue != null)
        //        {
        //            transformation = item.Node.IsTrue.MatrixSet;
        //            foreach (long[][] tMatrix in transformation)
        //            {
        //                changed.Add(cm.Multiplication(tMatrix, matrix));
        //            }

        //        }
        //        else if (item.Node.IsFalse != null)
        //        {
        //            transformation = item.Node.IsFalse.MatrixSet;
        //            foreach (long[][] tMatrix in transformation)
        //            {
        //                changed.Add(cm.Multiplication(tMatrix, matrix));
        //            }

        //        }
        //        else if ((item.Node.Next == null) && (item.Node.IsTrue == null) && (item.Node.IsFalse != null))
        //        {
        //            /* vystupni uzel => distribuce na volajici funkce
        //             * nacist seznam uzlu z fncList, ktere byly ulozeny s identifikaci funkce, ve ktere je item.Node
        //             * a pro kazdy uzel:
        //            */
                    
        //            IaNode hlpNode = new IaNode();// pomocny uzel zastupujici uzly, ktere volaly funkci

        //            foreach (long[] vector in hlpNode.GeneratorSet)
        //            {
        //                nMatrix = cm.ConvertVectorToMatrix(vector);
        //                changed.Add(cm.Multiplication(matrix,nMatrix));
        //            }
        //        }

        //        foreach (long[][] chMatrix in changed)
        //        {
        //            TempVector vector = new TempVector(cm.ConvertMatrixToVector(chMatrix));
        //            rm.AddVector(vector);
        //        }
        //    }
        //}

        // konec - pokus o sestaveni matic

        public void Print(int[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Console.WriteLine("{0} - {1}", i, a[i]);
            }
            Console.WriteLine();
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

        private void PrintVector(long[] x)
        {
            for (int i = 0; i < x.Length; i++)
            {
                Console.WriteLine(x[i]);
            }
            Console.WriteLine();
        }
    }
}
