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
        private const IPDGame.Choices FIRST_CHOICE = IPDGame.Choices.C;

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
    }
}
