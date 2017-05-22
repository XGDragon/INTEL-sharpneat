using System.Collections.Generic;
using SharpNeat.Domains.IPD.Players.Tree;

namespace SharpNeat.Domains.IPD.Players.Tree
{
    class DecisionTree
    {
        public const int INVOKE_Q = -2;
        
        private Dictionary<int, Node> _tree;
        private QFunction _q;
        private IPDPlayer _player;
        private Iterator _iterator;

        public DecisionTree(Dictionary<int, Node> tree)
        {
            if (!IsValidTree(tree))
                throw new System.Exception("Tree contains errors");
            _tree = tree;
            _q = QFunction.Default;
        }

        public DecisionTree(Dictionary<int, Node> tree, QFunction q) : this(tree)
        {
            _q = q;
        }

        public IPDGame.Choices[] Run(IPDGame game)
        {
            int node = 0;
            IPDGame.Choices[] result = null;
            while (_tree.ContainsKey(node))
            {
                //Encountered a Assign Result node
                if (_tree[node].HasResult)
                {
                    result = _tree[node].Result.Evaluate(_iterator.Alpha, _iterator.Beta);
                    break;
                }

                node = _tree[node].Evaluate(ref _iterator, game);    //Alpha, Beta are not changed if INVOKE_Q

                //k - K < 0
                if (node == INVOKE_Q)
                {
                    result = _q.Evaluate(_iterator.Alpha, _iterator.Beta);
                    break;
                }
            }

            if (result == null)
                throw new System.Exception("Result returned a null value");
            else if (result.Length == 0)
                throw new System.Exception("Result returned an empty value");
            else return result;
        }

        public void Reset(IPDPlayer owner)
        {
            _player = owner;
            _iterator = new Iterator(_player);
        }

        private bool IsValidTree(Dictionary<int, Node> tree)
        {
            bool hasRoot = tree.ContainsKey(0);

            return hasRoot;
        }

        public struct Iterator
        {
            public IPDPlayer Player { get; private set; }
            public int Alpha { get; set; }
            public int Beta { get; set; }
            public int K { get; set; }

            public Iterator(IPDPlayer player)
            {
                Player = player;
                Alpha = 0;
                Beta = 0;
                K = 0;
            }
        }
    }
}
