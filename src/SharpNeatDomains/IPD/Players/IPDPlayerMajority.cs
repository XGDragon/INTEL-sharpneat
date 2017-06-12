using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Phenomes;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerMajority : IPDPlayer
    {
        public override string Name => "Majority";

        private Queue<IPDGame.Choices> _initialChoices;

        private int _totalPSScore;
        private int _totalTRScore;

        private Object _choiceLock = new Object();

        public IPDPlayerMajority()
        {

        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            lock (_choiceLock)
            {
                if (_initialChoices.Count > 0)
                    return _initialChoices.Dequeue();
                else
                {
                    if (game.GetPast(this, game.T - 1) == IPDGame.Past.S || game.GetPast(this, game.T - 1) == IPDGame.Past.P)
                        _totalPSScore++;
                    else
                        _totalTRScore++;

                    return (_totalPSScore >= _totalTRScore) ? IPDGame.Choices.D : IPDGame.Choices.C;
                }
            }
        }

        public override void Reset()
        {
            _initialChoices = new Queue<IPDGame.Choices>();
            _initialChoices.Enqueue(IPDGame.Choices.D);

            _totalPSScore = 0;
            _totalTRScore = 0;
        }
    }
}
