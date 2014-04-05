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
                            List<Tuple<string, IaNode, string, int>> gts = new List<Tuple<string, IaNode, string, int>>();

                            IaNode node = new IaNode { Name = fncName + "_fnc_begin" };
                            program.Graph.Add(fncName, node);
                            int i = 0;
                            while (i < fnc.Body.Statements.Count)
                            {
                                BaseAst st = fnc.Body.Statements[i];
                                if (st is OperatorAst)
                                {
                                    IaNode next = new IaNode { Name = fncName + "_after_expr_line#" + st.TokenStartLine };
                                    node.Next = new IaEdge { Name = fncName + "_expr_line#" + st.TokenStartLine, Ast = st, From = node, To = next };
                                    node = next;
                                    i++;
                                    continue;
                                }
                                if (st.AstType == AstNodeTypes.FunctionCall)
                                {
                                    IaNode next = new IaNode { Name = fncName + "_after_fnccall_line#" + st.TokenStartLine };
                                    node.Next = new IaEdge { Name = fncName + "_fnccall_line#" + st.TokenStartLine, Ast = st, From = node, To = next };
                                    node = next;
                                    i++;
                                    continue;
                                }
                                if (st is ConditionAst)
                                {
                                    IaNode next = new IaNode { Name = fncName + "_after_condition_line#" + st.TokenStartLine };
                                    node.Next = new IaEdge { Name = fncName + "_condition_line#" + st.TokenStartLine, Ast = st, From = node, To = next };
                                    node = next;
                                    i++;

                                    BaseAst gt = fnc.Body.Statements[i];
                                    gts.Add(new Tuple<string, IaNode, string, int>("IsTrue", node, (gt as GotoAst).Label, st.TokenStartLine));
                                    next = new IaNode { Name = fncName + "_condition_false_line#" + st.TokenStartLine };
                                    node.IsFalse = new IaEdge { Name = fncName + "_condition_false_line#" + st.TokenStartLine, From = node, To = next };
                                    node = next;
                                    i++;
                                    continue;
                                }
                                if (st.AstType == AstNodeTypes.Label)
                                {
                                    node.Name = fncName + "_label_" + st.TokenText;
                                    lbls.Add(st.TokenText, node);
                                    i++;
                                    continue;
                                }
                                if (st is GotoAst)
                                {
                                    gts.Add(new Tuple<string, IaNode, string, int>("Next", node, (st as GotoAst).Label, st.TokenStartLine));
                                    node = new IaNode { Name = fncName + "_after_goto_line#" + (st as GotoAst).Label }; // po goto musi byt novy node (nova vetev)
                                    i++;
                                    continue;
                                }
                                if (st.AstType == AstNodeTypes.Return)
                                {
                                    node = new IaNode { Name = fncName + "_return_line#" + st.TokenStartLine }; // aktualni node je konec funkce, novy node pro pokracovani, jinak se zahodi
                                    i++;
                                    continue;
                                }
                            }

                            foreach (Tuple<string, IaNode, string, int> gt in gts)
                            {
                                IaNode to = lbls[gt.Item3];
                                if (to == null)
                                {
                                    Console.WriteLine("Neznamy label {0} pri vytvareni grafu.", gt.Item3);
                                    return false;
                                }
                                if (gt.Item1 == "Next")
                                    gt.Item2.Next = new IaEdge { Name = fncName + "_goto_line#" + gt.Item4, From = gt.Item2, To = to };
                                else if (gt.Item1 == "IsTrue")
                                    gt.Item2.IsTrue = new IaEdge { Name = fncName + "_condition_true_line#" + gt.Item4, From = gt.Item2, To = to };
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
