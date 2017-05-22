using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players.Tree
{
    abstract class ResultNode : Node
    {
        public override QFunction Result => throw new Exception("This node has not implemented a QFunction.");
    }

    class AssignResultNode : ResultNode
    {
        public override bool HasResult => true;
        public override QFunction Result => _q;

        private QFunction _q;

        public AssignResultNode(params IPDGame.Choices[] choices)
        {
            _q = new QFunction(choices);
        }

        public AssignResultNode(QFunction qfunction)
        {
            _q = qfunction;
        }
    }

    class JumpResultNode : ResultNode
    {
        public override bool HasResult => false;

        int _jumpTo;

        public JumpResultNode(int jumpTo)
        {
            _jumpTo = jumpTo;
        }

        public override int Evaluate(DecisionTree.Iterator iterator, IPDGame game)
        {
            iterator.K++;
            return _jumpTo;
        }
    }
}
