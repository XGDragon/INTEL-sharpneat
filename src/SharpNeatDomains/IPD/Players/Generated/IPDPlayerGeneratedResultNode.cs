using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players.Generated
{
    abstract class IPDPlayerGeneratedResultNode : IPDPlayerGeneratedNode
    {
        
    }

    class IPDPlayerGeneratedAssignNode : IPDPlayerGeneratedResultNode
    {
        public override IPDPlayerGeneratedTreeIterator Evaluate(IPDGame game, int Alpha, int Beta)
        {
            throw new NotImplementedException();
        }
    }

    class IPDPlayerGeneratedJumpNode : IPDPlayerGeneratedResultNode
    {
        int _jumpTo;

        public override IPDPlayerGeneratedTreeIterator Evaluate(IPDGame game, int Alpha, int Beta)
        {

        }
    }
}
