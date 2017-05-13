using System.Collections.Generic;

namespace SharpNeat.Domains.IPD.Players.Generated
{
    struct IPDPlayerGeneratedTreeIterator
    {
        public const int INVOKE_Q = -2;
        public const int SUCCESS = -1;

        public IPDGame.Choices[] Result { get; private set; }
        public int Alpha { get; private set; }
        public int Beta { get; private set; }
        
        private Dictionary<int, IPDPlayerGeneratedNode> _tree;
        private IPDPlayerGenerated.QFunction _q;

        private IPDPlayerGeneratedNode _next;

        public IPDPlayerGeneratedTreeIterator(Dictionary<int, IPDPlayerGeneratedNode> tree, IPDPlayerGenerated.QFunction q)
        {
            _tree = tree;
            _next = tree[0];

            _q = q;
            Alpha = 0;
            Beta = 0;
        }

        public IPDGame.Choices[] result Run(IPDGame game, out IPDGame.Choices[] result)
        {
            int i = _next.Evaluate(game, Alpha, Beta);
        }
    }

    struct IPDPlayerGeneratedNodeResult
    {
        public enum Outcome { Failed, Success, Next }

        private IPDGame.Choices[] _result = null;

        public IPDPlayerGeneratedNodeResult(Outcome o, int next)
        {

        }

        public IPDPlayerGeneratedNodeResult(Outcome o, IPDGame.Choices[] result)
        {

        }
    }
}
