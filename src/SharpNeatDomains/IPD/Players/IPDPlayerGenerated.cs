using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Domains.IPD.Players.Tree;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerGenerated : IPDPlayer
    {
        private Queue<IPDGame.Choices> _q = new Queue<IPDGame.Choices>();
        private DecisionTree _tree;
        private Object _choiceLock = new Object();

        public IPDPlayerGenerated(DecisionTree tree, params IPDGame.Choices[] initQ)
        {
            if (initQ.Length == 0)
                throw new Exception("Initialiation Q was empty!");

            FillQueue(initQ);
            _tree = tree;
        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            IPDGame.Choices r;
            lock (_choiceLock)
            {
                if (_q.Count == 0)
                    FillQueue(_tree.Run(this, game));
                if (_q.Count == 0)
                    throw new Exception("Q is still empty?");
                r = _q.Dequeue();
            }

            return r;
        }

        private void FillQueue(IPDGame.Choices[] choices)
        {
            for (int i = 0; i < choices.Length; i++)
                _q.Enqueue(choices[i]);
        }
    }
}
