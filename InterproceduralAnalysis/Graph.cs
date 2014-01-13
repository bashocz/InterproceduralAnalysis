using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class IaNode
    {
        //public int[] Vector { get; private set; }

        public IaNode()//(int size)
        {
            //Vector = new int[size];
        }

        public IaEdge Next { get; set; } // for all statements

        public IaEdge IsTrue { get; set; } // for if statement
        public IaEdge IsFalse { get; set; } // for if statement
    }

    class IaEdge
    {
        public BaseAstNode Ast { get; set; }

        public IaNode From { get; set; }
        public IaNode To { get; set; }
    }
}
