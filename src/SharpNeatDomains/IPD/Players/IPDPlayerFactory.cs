﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Domains.IPD.Players.Tree;
using RandomNameGeneratorLibrary;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerFactory
    {
        public static IPDPlayerGenerated TFT => _createXTFT("TFT", IPDGame.Choices.C);
        public static IPDPlayerGenerated STFT => _createXTFT("STFT", IPDGame.Choices.D);
        public static IPDPlayerGenerated AllC => _allX("AllC", IPDGame.Choices.C);
        public static IPDPlayerGenerated AllR => _allX("AllR", IPDGame.Choices.R);
        public static IPDPlayerGenerated AllD => _allX("AllD", IPDGame.Choices.D);

        private static IPDPlayerGenerated _createXTFT(string name, IPDGame.Choices startingChoice)
        {
            DecisionTree tree = new DecisionTree(new Dictionary<int, Node>()
            {
                { 0, new PayoffConditionalNode(1, 2, 1, IPDGame.Past.P, IPDGame.Past.S) },
                { 1, new AssignResultNode(new QFunction(new QFunctionSequence(IPDGame.Choices.D)))},
                { 2, new AssignResultNode(new QFunction(new QFunctionSequence(IPDGame.Choices.C)))}
            });

            return new IPDPlayerGenerated(name, tree, startingChoice);
        }

        private static IPDPlayerGenerated _allX(string name, IPDGame.Choices X)
        {
            DecisionTree tree = new DecisionTree(new Dictionary<int, Node>()
            {
                { 0, new AssignResultNode(new QFunction(new QFunctionSequence(X)))},
            }, QFunction.Default);

            return new IPDPlayerGenerated(name, tree, X);
        }

        private Random _r;
        private int _condId;
        private int _resultId;
        private Func<(int, int)>[,] _table;

        private IPDGame.Past[] _past = new IPDGame.Past[4] { IPDGame.Past.T, IPDGame.Past.R, IPDGame.Past.P, IPDGame.Past.S };

        public IPDPlayerFactory(int seed = 0)
        {
            _r = (seed <= 0) ? new System.Random() : new System.Random(seed);

            Func<(int, int)> RR = () => { return (_resultId++, _resultId++); };
            Func<(int, int)> CC = () => { return (_condId++, _condId++); };
            Func<(int, int)> CR = () => { return (_condId++, _resultId++); };
            Func<(int, int)> RC = () => { return (_resultId++, _condId++); };
            Func<(int, int)> T  = () => { return (_r.NextDouble() < 0.5) ? (_resultId++, _condId++) : (_condId++, _resultId++); };
            _table = new Func<(int, int)>[3, 4]
            {
                { RR, RR, RR, RR },
                { T , CR, RC, T  },
                { T , CR, RC, CC }
            };
        }

        public IPDPlayerGenerated Random()
        {
            Dictionary<int, Node> treeDict = new Dictionary<int, Node>();
            
            (int left, int right)[] condNodes = new (int, int)[RandomAmount(7)];
            _condId = 1;
            _resultId = condNodes.Length;

            for (int i = 0; i < condNodes.Length; i++)
            {
                int a = (_r.NextDouble() < 0.5d) ? 0 : 1;
                int b = (_r.NextDouble() < 0.5d) ? 0 : 2;

                int x = Math.Min(condNodes.Length - _condId, 2);
                int y = a + b;

                condNodes[i] = _table[x, y]();
            }

            for (int i = 0; i < condNodes.Length; i++)
                treeDict.Add(i, CreateConditionalNode(condNodes[i].left, condNodes[i].right));
            for (int i = condNodes.Length; i < _resultId; i++)
                treeDict.Add(i, CreateResultNode());

            string name = _r.GenerateRandomFirstAndLastName() + " " + _r.GenerateRandomPlaceName();
            IPDGame.Choices[] c = new IPDGame.Choices[RandomAmount()];
            for (int i = 0; i < c.Length; i++)
                c[i] = RandomChoice();

            return new IPDPlayerGenerated(name, new DecisionTree(treeDict, CreateQFunction()), c);
        }

        private IPDGame.Choices RandomChoice()
        {
            double n = _r.NextDouble();
            if (n < 0.1)
                return IPDGame.Choices.R;
            else if (n < 0.55)
                return IPDGame.Choices.C;
            else return IPDGame.Choices.D;
        }

        /// <summary>
        /// Minimum is always 1.
        /// </summary>
        private int RandomAmount(int max = 3)
        {
            int a = 0;
            double n = max;
            while (a <= max && _r.NextDouble() < n-- / max)
                a++;
            return a;
        }

        #region QFunction
        private QFunction CreateQFunction()
        {
            double n = _r.NextDouble();
            if (n < 0.75)
                return new QFunction(CreateQFunctionSequence());
            else
                return new QFunction(CreateQFunctionConditional(), CreateQFunctionSequence(), CreateQFunctionSequence());
        }


        private QFunction.Values RandomQValue()
        {
            double n = _r.NextDouble();
            if (n < 0.25)
                return QFunction.Values.Alpha;
            if (n < 0.50)
                return QFunction.Values.Beta;
            else
                return QFunction.Values.Number;
        }

        private QFunctionConditional CreateQFunctionConditional()
        {
            QFunction.Values[] v = new QFunction.Values[] { RandomQValue(), RandomQValue() };
            if (v[0] == QFunction.Values.Number && v[1] == QFunction.Values.Number)
                return new QFunctionConditional(RandomAmount(), RandomAmount());
            if (v[0] != QFunction.Values.Number && v[1] != QFunction.Values.Number)
                return new QFunctionConditional(v[0], v[1]);
            if (v[0] == QFunction.Values.Number)
                return new QFunctionConditional(v[0], RandomAmount());
            else //(v[1] == QFunction.Values.Number)
                return new QFunctionConditional(RandomAmount(), v[1]);
        }

        private QFunctionSequence CreateQFunctionSequence()
        {
            QFunctionSequence.Piece[] p = new QFunctionSequence.Piece[RandomAmount()];
            for (int i = 0; i < p.Length; i++)
                p[i] = new QFunctionSequence.Piece(RandomChoice(), RandomQValue(), RandomAmount());
            return new QFunctionSequence(p);
        }
        #endregion
        
        private ResultNode CreateResultNode()
        {
            return new AssignResultNode(CreateQFunction());
        }

        private ConditionalNode CreateConditionalNode(int leftNode, int rightNode)
        {
            if (_r.NextDouble() < 0.5)
                return CreatePayoffNode(leftNode, rightNode);
            else return CreateValueNode(leftNode, rightNode);
        }

        private PayoffConditionalNode CreatePayoffNode(int leftNode, int rightNode)
        {
            int[] r = MathNet.Numerics.Combinatorics.GeneratePermutation(4, _r);            
            IPDGame.Past[] p = new IPDGame.Past[RandomAmount(3)];
            for (int i = 0; i < p.Length; i++)
                p[i] = _past[r[i]];
            return new PayoffConditionalNode(leftNode, rightNode, RandomAmount(), RandomAmount(4) - 1, RandomAmount(4) - 1, p);
        }

        private ValueConditionalNode CreateValueNode(int leftNode, int rightNode)
        {
            return new ValueConditionalNode(leftNode, rightNode, CreateQFunctionConditional(), RandomAmount(4) - 1, RandomAmount(4) - 1);
        }
    }
}
