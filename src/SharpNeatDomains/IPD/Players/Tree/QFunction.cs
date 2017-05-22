using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players.Tree
{
    /// <summary>
    /// QFunction is built from a conditional and sequences
    /// </summary>
    class QFunction
    {
        public enum Values { Alpha, Beta, Number }

        public static QFunction Default => new QFunction(new QFunctionSequence(IPDGame.Choices.R));

        private bool _hasCond = false;
        private QFunctionConditional _cond;
        private QFunctionSequence _true;
        private QFunctionSequence _false;


        public QFunction(QFunctionSequence q)
        {
            _true = q;
        }

        public QFunction(params IPDGame.Choices[] choices) : this(new QFunctionSequence(choices)) { }

        public QFunction(QFunctionConditional cond, QFunctionSequence t, QFunctionSequence f) : this(t)
        {
            _cond = cond;
            _false = f;
            _hasCond = true;
        }

        public IPDGame.Choices[] Evaluate(int alpha, int beta)
        {
            if (_hasCond)
                return _cond.Evaluate(alpha, beta) ? _true.Evaluate(alpha, beta) : _false.Evaluate(alpha, beta);
            else return _true.Evaluate(alpha, beta);
        }        
    }

    /// <summary>
    /// Left [leq] Right
    /// </summary>
    struct QFunctionConditional
    {
        private (QFunction.Values, int) _left;
        private (QFunction.Values, int) _right;

        public QFunctionConditional(QFunction.Values left, QFunction.Values right)
        {
            _left = (left, -1);
            _right = (right, -1);
        }

        public QFunctionConditional(QFunction.Values left, int right)
        {
            _left = (left, -1);
            _right = (QFunction.Values.Number, right);
        }

        public QFunctionConditional(int left, QFunction.Values right)
        {
            _left = (QFunction.Values.Number, left);
            _right = (right, -1);
        }

        public QFunctionConditional(int left, int right)
        {
            _left = (QFunction.Values.Number, left);
            _right = (QFunction.Values.Number, right);
        }

        public bool Evaluate(int alpha, int beta)
        {
            int GetValue((QFunction.Values, int) v)
            {
                if (v.Item1 == QFunction.Values.Alpha)
                    return alpha;
                if (v.Item1 == QFunction.Values.Beta)
                    return beta;
                return v.Item2;
            }

            return (GetValue(_left) <= GetValue(_right));
        }
    }

    /// <summary>
    /// Sequence is built from SequencePieces, that is, { x^y, a^b } are two SequencePieces forming a Sequence
    /// </summary>
    struct QFunctionSequence
    {
        private Piece[] _pieces;
        private IPDGame.Choices[] _choices;

        public QFunctionSequence(params IPDGame.Choices[] choices)
        {
            _pieces = null;
            _choices = choices;
        }

        public QFunctionSequence(params Piece[] pieces)
        {
            _pieces = pieces;
            _choices = null;
        }

        public IPDGame.Choices[] Evaluate(int alpha, int beta)
        {
            if (_choices == null)
            {
                List<IPDGame.Choices> c = new List<IPDGame.Choices>();
                for (int i = 0; i < _pieces.Length; i++)
                {
                    int r = _pieces[i].Repeats(alpha, beta);
                    for (int j = 0; j < r; j++)
                        c.Add(_pieces[i].Choice);
                }
                if (c.Count > 0)
                    return c.ToArray();
                else
                    return new IPDGame.Choices[] { _pieces[0].Choice };
            }
            else return _choices;
        }

        public struct Piece
        {
            private QFunction.Values _value;
            public IPDGame.Choices Choice { get; private set; }
            private int _repeats;

            public Piece(IPDGame.Choices choice, int repeats)
            {
                _value = QFunction.Values.Number;
                Choice = choice;
                _repeats = repeats;
            }

            public Piece(IPDGame.Choices choice, QFunction.Values alphaBeta, int repeats = 0)
            {
                _value = alphaBeta;
                Choice = choice;
                _repeats = repeats;
            }

            public int Repeats(int alpha, int beta)
            {
                if (_value == QFunction.Values.Number)
                    return _repeats;
                return (_value == QFunction.Values.Alpha) ? alpha : beta;
            }
        }
    }
}
