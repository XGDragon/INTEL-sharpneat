using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Domains.IPD.Players.Tree;

namespace SharpNeat.Domains.IPD.Players
{
    static class IPDPlayerFactory
    {
        public static IPDPlayerGenerated TFT => _createXTFT("TFT", IPDGame.Choices.C);
        public static IPDPlayerGenerated STFT => _createXTFT("STFT", IPDGame.Choices.D);
        public static IPDPlayerGenerated AllC => _allX("AllC", IPDGame.Choices.C);
        public static IPDPlayerGenerated AllR => _allX("AllR", IPDGame.Choices.R);
        public static IPDPlayerGenerated AllD => _allX("AllD", IPDGame.Choices.D);
        
        public static IPDPlayerGenerated Random()
        {
            Random r = new System.Random();
            Dictionary<int, Node> treeDict = new Dictionary<int, Node>();

            IPDGame.Choices randomChoice()
            {
                double n = r.NextDouble();
                if (n < 0.1)
                    return IPDGame.Choices.R;
                else if (n < 0.55)
                    return IPDGame.Choices.C;
                else return IPDGame.Choices.D;
            }

            int loopCount()
            {
                int l = 0;
                double n = 1.0d;
                while (r.NextDouble() < n)
                {
                    n /= 2;
                    l++;
                }
                return l;

            QFunction createQFunction()
            {
                double n = r.NextDouble();
                if (n < 0.5)
                    return new QFunction()
            }

            QFunctionConditional createQFunctionConditional()
            {
                    return new QFunctionConditional()//make conditional
            }

            QFunctionSequence createQFunctionSequence()
            {
                QFunctionSequence.Piece[] p = new QFunctionSequence.Piece[loopCount()];
                for (int i = 0; i < p.Length; i++)
                    p[i] = createQFunctionPiece();
                return new QFunctionSequence(p);
            }

            QFunctionSequence.Piece createQFunctionPiece()
            {
                QFunctionSequence.Piece.Values v;
                int reps = 0;
                double n = r.NextDouble();
                if (n < 0.33)
                    v = QFunctionSequence.Piece.Values.Alpha;
                else if (n < 0.66)
                    v = QFunctionSequence.Piece.Values.Beta;
                else
                {
                    v = QFunctionSequence.Piece.Values.Number;
                    reps = r.Next(10);
                }
                return new QFunctionSequence.Piece(randomChoice(), v, (reps > 3) ? reps / 2 : reps);
            }

            Node createNode()
            {
                double n = r.NextDouble();
                if (n < 0.33)
                    return createAssignNode();
                else if (n < 0.66)
                    return createPayoffNode();
                else
                    return createValueNode();
            }

            AssignResultNode createAssignNode()
            {
                return new AssignResultNode()
            }

            PayoffConditionalNode createPayoffNode()
            {

            }

            ValueConditionalNode createValueNode()
            {

            }


            QFunction Q;
            new Dictionary<int, Node>()
            {
                {

                }
            }







            return null;
        }

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
    }
}
