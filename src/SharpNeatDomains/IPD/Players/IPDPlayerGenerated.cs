using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerGenerated : IPDPlayer
    {
        private Queue<IPDGame.Choices> _q;
        private Generated.IPDPlayerGeneratedTreeIterator _tree;

        public delegate IPDGame.Choices[] QFunction(int alpha, int beta);

        public IPDPlayerGenerated(Queue<IPDGame.Choices> initQ, Generated.IPDPlayerGeneratedTreeIterator tree)
        {
            _q = initQ;
            _tree = tree;
        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            if (_q.Count == 0)
            {
                IPDGame.Choices[] result;
                if (!_e.Evaluate(game, out result))
                    result = new IPDGame.Choices[] { IPDGame.Choices.C };
                for (int i = 0; i < r.Length; i++)
                    _q.Enqueue(r[i]);
            }
            if (_q.Count > 0)
                return _q.Dequeue();
            else
                throw new Exception("Q is still empty after evaluating the tree");
        }
    }
}
