using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players.Generated
{
    abstract class IPDPlayerGeneratedConditionalNode : IPDPlayerGeneratedNode
    {
        protected int _alpha;
        protected int _beta;

        protected int _label;   //unimplemented as of now

        protected IPDPlayerGeneratedNode _left;
        protected IPDPlayerGeneratedNode _right;

        public IPDPlayerGeneratedConditionalNode(IPDPlayerGeneratedNode left, IPDPlayerGeneratedNode right, int a, int b)
        {
            _left = left;
            _right = right;
            _alpha = a;
            _beta = b;
        }
    }

    class IPDPlayerGeneratedPayoffConditionalNode : IPDPlayerGeneratedConditionalNode
    {
        private int _k;
        private IPDGame.Past[] _x;

        public IPDPlayerGeneratedPayoffConditionalNode(IPDPlayerGeneratedNode left, IPDPlayerGeneratedNode right, int a, int b, int k, params IPDGame.Past[] x) : base(left, right, a, b)
        {            
            _k = k;
            _x = x;
        }

        public override IPDPlayerGeneratedTreeIterator Evaluate(IPDGame game, int Alpha, int Beta)
        {
            if (game.T - _k < 0)
                //return Q..

            if (_x.Contains(game[game.T - _k]))
                return _left.Evaluate(game, Alpha + _alpha, Beta);
            else
                return _right.Evaluate(game, Alpha, Beta + _beta);
        }
    }

    class IPDPlayerGeneratedValueConditionalNode : IPDPlayerGeneratedConditionalNode
    {
        private int _u;
        private int _v;

        public IPDPlayerGeneratedValueConditionalNode(IPDPlayerGeneratedNode left, IPDPlayerGeneratedNode right, int a, int b, int u, int v) : base(left, right, a, b)
        {
            _u = u;
            _v = v;
        }

        public override IPDPlayerGeneratedTreeIterator Evaluate(IPDGame game, int Alpha, int Beta)
        {
            if (_u <= _v)
                return _left.Evaluate(game, Alpha + _alpha, Beta);
            else
                return _right.Evaluate(game, Alpha, Beta + _beta);
        }
    }
}
