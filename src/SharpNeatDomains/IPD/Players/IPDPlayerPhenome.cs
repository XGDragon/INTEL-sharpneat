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

        private const IPDGame.Choices FIRST_CHOICE = IPDGame.Choices.C;
        //private readonly IPDGame.Past[] _pastToI = new IPDGame.Past[4] { IPDGame.Past.T, IPDGame.Past.R, IPDGame.Past.P, IPDGame.Past.S };

        public override string Name => "Phenome";

        private IBlackBox _phenome;

        public IPDPlayerPhenome(IBlackBox phenome)
        {
            _phenome = phenome;
        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            if (game.T == 0)
                return FIRST_CHOICE;

            int i = 0;
            for (int k = 1; k <= _phenome.InputSignalArray.Length / 2; k++)
            {
                IPDGame.Past p = game.GetPast(this, game.T - k);
                //My choice (0 == C, 1 == D)
                _phenome.InputSignalArray[i++] = (p == IPDGame.Past.R || p == IPDGame.Past.S) ? 0 : 1;
                //Opponent choice
                _phenome.InputSignalArray[i++] = (p == IPDGame.Past.T || p == IPDGame.Past.R) ? 0 : 1;
            }
            
            _phenome.Activate();
            if (!_phenome.IsStateValid)
            {   // Any black box that gets itself into an invalid state is unlikely to be
                // any good, so lets just bail out here.
                return IPDGame.Choices.R;
            }
            
            return (_phenome.OutputSignalArray[0] > _phenome.OutputSignalArray[1]) ? IPDGame.Choices.C : IPDGame.Choices.D;
        }

        public override void Reset()
        {
            _phenome.ResetState();
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
