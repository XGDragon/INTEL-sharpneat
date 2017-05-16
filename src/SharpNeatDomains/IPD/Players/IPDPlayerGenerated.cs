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
        public override string Name => _name;

        private string _name;
        private IPDGame.Choices[] _initQ;
        private Queue<IPDGame.Choices> _q = new Queue<IPDGame.Choices>();
        private DecisionTree _tree;
        private Object _choiceLock = new Object();

        public IPDPlayerGenerated(string name, DecisionTree tree, params IPDGame.Choices[] initQ)
        {
            _name = name;

            if (initQ.Length == 0)
                throw new Exception("Initialiation Q was empty!");

            _initQ = initQ;
            _tree = tree;
        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            IPDGame.Choices r;
            lock (_choiceLock)
            {
                if (game.T == 0)
                    FillQueue(_initQ);
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
