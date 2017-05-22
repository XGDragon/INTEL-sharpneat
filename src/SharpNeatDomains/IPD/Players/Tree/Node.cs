using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players.Tree
{
    abstract class Node
    {
        public abstract bool HasResult { get; }
        public abstract QFunction Result { get; }

        public virtual int Evaluate(ref DecisionTree.Iterator iterator, IPDGame game)
        {
            throw new Exception("This node has not implemented an Evaluate function.");
        }
    }
}
