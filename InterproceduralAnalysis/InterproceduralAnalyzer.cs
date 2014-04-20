using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace InterproceduralAnalysis
{
    class InterproceduralAnalyzer
    {
        private readonly BaseFunctions bfm;
        private readonly BaseFunctions bg;

        private readonly bool printM;
        private readonly bool printG;
        private readonly bool printLE;

        public InterproceduralAnalyzer(int w, int n, bool printGM, bool printGG, bool printLE)
        {
            if ((w <= 0) || (n < 0))
                throw new ApplicationException();

            this.printM = printGM;
            this.printG = printGG;
            this.printLE = printLE;

            bg = new BaseFunctions(w, n);
            bfm = new BaseFunctions(w, (bg.var_n * bg.var_n) - 1);
        }

        #region Creating Transition Matrixes

        private void CreateTransitionMatrixes(ProgramAst prg)
        {
            Queue<IaNode> q = new Queue<IaNode>();
            foreach (string name in prg.OrigFncs.Keys)
                q.Enqueue(prg.Graph[name]);

            while (q.Count > 0)
            {
                IaNode n = q.Dequeue();
                if (n != null)
                {
                    foreach (IaEdge edge in n.Edges)
                    {
                        if (edge.MatrixSet == null)
                        {
                            var mtx = new TransitionMatrixSet(edge, bg);
                            mtx.GetMatrix(prg.Vars);
                            edge.MatrixSet = mtx;

                            if (printM)
                                mtx.Print();

                            q.Enqueue(edge.To);
                        }
                    }
                }
            }
        }

        #endregion Creating Transition Matrixes

        #region Creating Function Call Matrixes

        private void CreateEmptyFunctionG(ProgramAst prg, List<IaEdge> fncCallEdges)
        {
            foreach (IaNode node in prg.Graph.Values)
            {
                Queue<IaNode> q = new Queue<IaNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    IaNode n = q.Dequeue();
                    if (n.FunctionGSet == null)
                    {
                        n.FunctionGSet = new GeneratorSet(n, bfm);
                        foreach (IaEdge edge in n.Edges)
                        {
                            if ((edge.Ast != null) && (edge.Ast.AstType == AstNodeTypes.FunctionCall))
                                fncCallEdges.Add(edge);

                            if (edge.To.FunctionGSet == null)
                                q.Enqueue(edge.To);
                        }
                    }
                }
            }
        }

        private void CreateFunctionMatrixes(ProgramAst prg)
        {
            List<IaEdge> fncCallEdges = new List<IaEdge>();
            CreateEmptyFunctionG(prg, fncCallEdges);

            Queue<NodeMatrix> w_queue = new Queue<NodeMatrix>();
            foreach (string name in prg.OrigFncs.Keys)
                w_queue.Enqueue(new NodeMatrix { Node = prg.Graph[name], Matrix = bfm.GetIdentity(bg.var_n) });

            while (w_queue.Count > 0)
            {
                NodeMatrix pair = w_queue.Dequeue();

                IaNode from = pair.Node;
                if ((from.Next != null) || (from.IsTrue != null) || (from.IsFalse != null))
                {
                    foreach (IaEdge edge in from.Edges)
                    {
                        if ((edge.Ast == null) || (edge.Ast.AstType != AstNodeTypes.FunctionCall))
                        {
                            IaNode to = edge.To;

                            // algoritmus 2
                            foreach (long[][] a_mtx in edge.MatrixSet.TMatrixes)
                            {
                                long[][] mtx = bg.MatrixMultiMatrix(a_mtx, pair.Matrix, bfm.var_m);
                                long[] xi = bg.MatrixToVector(mtx);
                                LeadVector x = new LeadVector(xi);
                                if (x.Lidx >= 0) // neni to nulovy vektor
                                {
                                    if (to.FunctionGSet.AddVector(x))
                                    {
                                        if (printG)
                                            to.FunctionGSet.Print();

                                        w_queue.Enqueue(new NodeMatrix { Node = to, Matrix = mtx });
                                    }
                                }
                            }
                        }
                        else
                        {
                            // algoritmus 3
                        }
                    }
                }
                else
                {
                    // algoritmus 4

                    foreach (IaEdge edge in fncCallEdges)
                    {
                        if ((edge.Ast == null) || (edge.Ast.AstType != AstNodeTypes.FunctionCall))
                            throw new ApplicationException();

                        if (edge.Ast.TokenText == from.FncName)
                        {
                            IaNode to = edge.To;

                            int i = 0;
                            while (edge.From.FunctionGSet.GArr[i] != null)
                            {
                                LeadVector vector = edge.From.FunctionGSet.GArr[i];
                                long[][] matrix = bfm.VectorToMatrix(vector.Vr, bg.var_n);

                                long[][] mtx = bg.MatrixMultiMatrix(pair.Matrix, matrix, bfm.var_m);
                                long[] xi = bg.MatrixToVector(mtx);
                                LeadVector x = new LeadVector(xi);
                                if (x.Lidx >= 0) // neni to nulovy vektor
                                {
                                    if (to.FunctionGSet.AddVector(x))
                                    {
                                        if (printG)
                                            to.FunctionGSet.Print();

                                        w_queue.Enqueue(new NodeMatrix { Node = to, Matrix = mtx });
                                    }
                                }
                                i++;
                            }
                        }
                    }
                }
            }
            // algoritmus 5
            foreach (string fncName in prg.LastNode.Keys)
            {
                IaNode last = prg.LastNode[fncName];

                List<long[][]> mtxs = new List<long[][]>();
                int i = 0;
                while (last.FunctionGSet.GArr[i] != null)
                {
                    mtxs.Add(bfm.VectorToMatrix(last.FunctionGSet.GArr[i].Vr, bg.var_n));
                    i++;
                }
                if (mtxs.Count > 0)
                {
                    foreach (IaEdge edge in fncCallEdges)
                    {
                        if ((edge.Ast == null) || (edge.Ast.AstType != AstNodeTypes.FunctionCall))
                            throw new ApplicationException();

                        if (edge.Ast.TokenText == last.FncName)
                        {
                            edge.MatrixSet.TMatrixes.Clear();
                            edge.MatrixSet.TMatrixes.AddRange(mtxs);
                        }
                    }
                }
            }
        }

        #endregion Creating Function Call Matrixes

        #region Creating Generator Sets

        private void AddIdentityVectors(Queue<NodeVector> w_queue, IaNode node)
        {
            long[][] id = bg.GetIdentity(bg.var_n);
            for (int i = 0; i < bg.var_n; i++)
                w_queue.Enqueue(new NodeVector { Node = node, Vector = new LeadVector(id[i]) });
        }

        private void CreateEmptyG(ProgramAst prg)
        {
            foreach (IaNode node in prg.Graph.Values)
            {
                Queue<IaNode> q = new Queue<IaNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    IaNode n = q.Dequeue();
                    if (n.GeneratorSet == null)
                    {
                        n.GeneratorSet = new GeneratorSet(n, bg);
                        foreach (IaEdge edge in n.Edges)
                            if (edge.To.GeneratorSet == null)
                                q.Enqueue(edge.To);
                    }
                }
            }
        }

        private void CreateGeneratorSets(ProgramAst prg)
        {
            CreateEmptyG(prg);

            Queue<NodeVector> w_queue = new Queue<NodeVector>();
            AddIdentityVectors(w_queue, prg.Graph["main"]);

            while (w_queue.Count > 0)
            {
                NodeVector pair = w_queue.Dequeue();

                IaNode from = pair.Node;
                foreach (IaEdge edge in from.Edges)
                {
                    IaNode to = edge.To;

                    if ((edge.Ast != null) && (edge.Ast.AstType == AstNodeTypes.FunctionCall))
                    {
                        IaNode fncBegin = prg.Graph[edge.Ast.TokenText];
                        if (fncBegin.GeneratorSet.AddVector(pair.Vector))
                        {
                            if (printG)
                                fncBegin.GeneratorSet.Print();

                            w_queue.Enqueue(new NodeVector { Node = fncBegin, Vector = pair.Vector });
                        }
                    }

                    foreach (long[][] a_mtx in edge.MatrixSet.TMatrixes)
                    {
                        long[] xi = bg.MatrixMultiVector(a_mtx, pair.Vector.Vr, bg.var_m);
                        LeadVector x = new LeadVector(xi);
                        if (x.Lidx >= 0) // neni to nulovy vektor
                        {
                            if (to.GeneratorSet.AddVector(x))
                            {
                                if (printG)
                                    to.GeneratorSet.Print();

                                w_queue.Enqueue(new NodeVector { Node = to, Vector = x });
                            }
                        }
                    }
                }
            }
        }

        #endregion Creating Generator Sets

        #region Creating Linear Equations

        private void CreateLinearEquations(ProgramAst prg)
        {
            foreach (IaNode node in prg.Graph.Values)
            {
                Queue<IaNode> q = new Queue<IaNode>();
                q.Enqueue(node);
                while (q.Count > 0)
                {
                    IaNode n = q.Dequeue();
                    if (n.LinearEquations == null)
                    {
                        n.LinearEquations = new LinearEquations(n, bg, printLE);
                        n.LinearEquations.CalculateLE();

                        foreach (IaEdge edge in n.Edges)
                            if (edge.To.LinearEquations == null)
                                q.Enqueue(edge.To);
                    }
                }
            }
        }

        #endregion Creating Linear Equations

        public void Analyze(ProgramAst prg)
        {
            CreateTransitionMatrixes(prg);
            CreateFunctionMatrixes(prg);
            CreateGeneratorSets(prg);
            CreateLinearEquations(prg);
        }
    }
    
    

}
