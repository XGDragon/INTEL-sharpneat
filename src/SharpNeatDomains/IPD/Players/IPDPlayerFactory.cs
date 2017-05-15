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
        public static IPDPlayerGenerated TFT => _createXTFT(IPDGame.Choices.C);
        public static IPDPlayerGenerated STFT => _createXTFT(IPDGame.Choices.D);
        public static IPDPlayerGenerated AllC => _allX(IPDGame.Choices.C);
        public static IPDPlayerGenerated AllR => _allX(IPDGame.Choices.R);
        public static IPDPlayerGenerated AllD => _allX(IPDGame.Choices.D);

        private static IPDPlayerGenerated _createXTFT(IPDGame.Choices startingChoice)
        {
            DecisionTree tree = new DecisionTree(new Dictionary<int, Node>()
            {
                { 0, new PayoffConditionalNode(1, 2, 1, IPDGame.Past.P, IPDGame.Past.S) },
                { 1, new AssignResultNode(new QFunction(new QFunctionSequence(IPDGame.Choices.D)))},
                { 2, new AssignResultNode(new QFunction(new QFunctionSequence(IPDGame.Choices.C)))}
            });

            return new IPDPlayerGenerated(tree, startingChoice);
        }

        private static IPDPlayerGenerated _allX(IPDGame.Choices X)
        {
            DecisionTree tree = new DecisionTree(new Dictionary<int, Node>()
            {
                { 0, new AssignResultNode(new QFunction(new QFunctionSequence(X)))},
            }, QFunction.Default);

            return new IPDPlayerGenerated(tree, X);
        }
    }
}
