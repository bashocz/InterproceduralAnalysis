using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterproceduralAnalysis
{
    class LeadVector
    {
        private readonly long[] vr;
        private readonly int li;

        public LeadVector(long[] vr)
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

        public long Lentry   
        {
            get
            {
                if ((li >= 0) && (li < vr.Length))
                    return vr[li];
                return 0;
            }
        }
    }
}
