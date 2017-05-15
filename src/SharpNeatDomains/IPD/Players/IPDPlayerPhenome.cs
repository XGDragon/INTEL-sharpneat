using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Phenomes;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerPhenome : IPDPlayer
    {
        //list of opponents. per opponent, play n games (filling up inputs with results). output must be the counter strategy.
        //suggestion for output: output 1 - 4 is resp. TRPS results from last round. depending on last round, use mixed strategy of output x
        //each strat has a counterstrategy vector size 4. AllD counterstrategy is { 0, 0, 0, 0 }. AllC and TFT counterstrategy is { 1, 1, 1, 1 }. random is { .5, .5, .5, .5 }
        //each game fitness is calculated by comparing current output vector with counterstrategy vector. smaller difference > higher fitness, possibly with greater weight for earlier games.
        //SEE PAPER FOR IDEAS ON FITNESS FUNCTIONS ETC, PAGE 112

        private IBlackBox _phenome;
        private const IPDGame.Choices FIRST_CHOICE = IPDGame.Choices.C;

        public IPDPlayerPhenome(IBlackBox phenome)
        {
            _phenome = phenome;
        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            if (game.T == 0)
                return FIRST_CHOICE;

            for (int i = 0, k = 1; i < _phenome.InputSignalArray.Length; i++, k++)
                _phenome.InputSignalArray[i] = PastToInput(game.GetPast(this, game.T - k));

            _phenome.Activate();
            if (!_phenome.IsStateValid)
            {   // Any black box that gets itself into an invalid state is unlikely to be
                // any good, so lets just bail out here.
                return IPDGame.Choices.R;
            }
            
            //take the output index equal to the most recent past outcome
            double output = _phenome.OutputSignalArray[PastToOutput(game.GetPast(this, game.T - 1))];
            //0.0 = D, because _r < 0.0 is always : D
            return (_r.NextDouble() < output) ? IPDGame.Choices.C : IPDGame.Choices.D;
        }

        private double PastToInput(IPDGame.Past past)
        {
            switch (past)
            {
                case IPDGame.Past.T:
                    return 1.0;
                case IPDGame.Past.R:
                    return 2.0;
                case IPDGame.Past.P:
                    return 3.0;
                case IPDGame.Past.S:
                    return 4.0;
                default:
                    return 0.0;
            }
        }

        private int PastToOutput(IPDGame.Past past)
        {
            switch (past)
            {
                case IPDGame.Past.T:
                    return 0;
                case IPDGame.Past.R:
                    return 1;
                case IPDGame.Past.P:
                    return 2;
                case IPDGame.Past.S:
                default:
                    return 3;
            }
        }
    }
}
