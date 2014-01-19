using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class GraphGenerator
    {
        public bool CreateGraph(ProgramAst program)
        {
            foreach (string fncName in program.ConvFncs.Keys)
            {
                FunctionAst fnc = program.ConvFncs[fncName] as FunctionAst;
                if (fnc != null)
                {
                    if ((fnc.Body != null) && (fnc.Body.Statements != null) && (fnc.Body.Statements.Count > 0))
                    {
                        bool isAnyCmd = false;
                        foreach (BaseAst st in fnc.Body.Statements)
                            if ((st is OperatorAst) || (st is ConditionAst) || (st.AstType == AstNodeTypes.FunctionCall) || (st.AstType == AstNodeTypes.Return))
                            {
                                isAnyCmd = true;
                                break;
                            }
                        if (isAnyCmd)
                        {
                            Dictionary<string, IaNode> lbls = new Dictionary<string, IaNode>();
                            List<Tuple<string, IaNode, string>> gts = new List<Tuple<string, IaNode, string>>();

                            IaNode node = new IaNode();
                            program.Graph.Add(fncName, node);
                            int i = 0;
                            while (i < fnc.Body.Statements.Count)
                            {
                                BaseAst st = fnc.Body.Statements[i];
                                if ((st is OperatorAst) || (st.AstType == AstNodeTypes.FunctionCall))
                                {
                                    IaNode next = new IaNode();
                                    node.Next = new IaEdge { Ast = st, From = node, To = next };
                                    node = next;
                                    i++;
                                    continue;
                                }
                                if (st is ConditionAst)
                                {
                                    IaNode next = new IaNode();
                                    node.Next = new IaEdge { Ast = st, From = node, To = next };
                                    node = next;
                                    i++;

                                    BaseAst gt = fnc.Body.Statements[i];
                                    gts.Add(new Tuple<string, IaNode, string>("IsTrue", node, (gt as GotoAst).Label));
                                    next = new IaNode();
                                    node.IsFalse = new IaEdge { From = node, To = next };
                                    node = next;
                                    i++;
                                    continue;
                                }
                                if (st.AstType == AstNodeTypes.Label)
                                {
                                    lbls.Add(st.TokenText, node);
                                    i++;
                                    continue;
                                }
                                if (st is GotoAst)
                                {
                                    gts.Add(new Tuple<string, IaNode, string>("Next", node, (st as GotoAst).Label));
                                    node = new IaNode(); // po goto musi byt novy node (nova vetev)
                                    i++;
                                    continue;
                                }
                                if (st.AstType== AstNodeTypes.Return)
                                {
                                    node = new IaNode(); // aktualni node je konec funkce, novy node pro pokracovani, jinak se zahodi
                                    i++;
                                    continue;
                                }
                            }

                            foreach (Tuple<string, IaNode, string> gt in gts)
                            {
                                IaNode to = lbls[gt.Item3];
                                if (to == null)
                                {
                                    Console.WriteLine("Neznamy label {0} pri vytvareni grafu.", gt.Item3);
                                    return false;
                                }
                                if (gt.Item1 == "Next")
                                    gt.Item2.Next = new IaEdge { From = gt.Item2, To = to };
                                else if (gt.Item1 == "IsTrue")
                                    gt.Item2.IsTrue = new IaEdge { From = gt.Item2, To = to };
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
