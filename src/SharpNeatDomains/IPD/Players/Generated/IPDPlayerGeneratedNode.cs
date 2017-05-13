using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players.Generated
{
    abstract class IPDPlayerGeneratedNode
    {
        public abstract int Evaluate(IPDGame game, int Alpha, int Beta);

        protected int _depth;
    }
}
