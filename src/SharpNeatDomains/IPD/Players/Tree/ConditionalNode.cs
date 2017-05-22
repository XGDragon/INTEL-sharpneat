using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players.Tree
{
    abstract class ConditionalNode : Node
    {
        public override bool HasResult => false;
        public override QFunction Result => throw new Exception("This node has not implemented a QFunction.");

        protected int _alphaAdd;
        protected int _betaAdd;

        protected int _left;
        protected int _right;

        public ConditionalNode(int left, int right)
        {
            _left = left;
            _right = right;
        }

        public ConditionalNode(int left, int right, int a = 0, int b = 0) : this(left, right)
        {
            _alphaAdd = a;
            _betaAdd = b;
        }
    }

    class PayoffConditionalNode : ConditionalNode
    {
        private int _k;
        private IPDGame.Past[] _x;

        public PayoffConditionalNode(int leftNode, int rightNode, int k, params IPDGame.Past[] x) : base(leftNode, rightNode)
        {
            _k = 0 - Math.Abs(k);
            _x = x;
        }

        public PayoffConditionalNode(int leftNode, int rightNode, int k, int a = 0, int b = 0, params IPDGame.Past[] x) : base(leftNode, rightNode, a, b)
        {
            _k = 0 - Math.Abs(k);
            _x = x;
        }

        public override int Evaluate(DecisionTree.Iterator iterator, IPDGame game)
        {
            int t = game.T - _k - iterator.K;

            if (t < 0)
                return DecisionTree.INVOKE_Q;

            if (_x.Contains(game.GetPast(iterator.Player, t)))
            {
                iterator.Alpha += _alphaAdd;
                return _left;
            }
            else
            {
                iterator.Beta += _betaAdd;
                return _right;
            }
        }
    }

    class ValueConditionalNode : ConditionalNode
    {
        QFunctionConditional _cond;

        public ValueConditionalNode(int left, int right, QFunctionConditional cond) : base(left, right)
        {
            _cond = cond;
        }

        public ValueConditionalNode(int left, int right, QFunctionConditional cond, int a = 0, int b = 0) : base(left, right, a, b)
        {
            _cond = cond;
        }

        public override int Evaluate(DecisionTree.Iterator iterator, IPDGame game)
        {
            if (_cond.Evaluate(iterator.Alpha, iterator.Beta))
            {
                iterator.Alpha += _alphaAdd;
                return _left;
            }
            else
            {
                iterator.Beta += _betaAdd;
                return _right;
            }
        }
    }
}
