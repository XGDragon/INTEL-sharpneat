using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Phenomes;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerAdaptive : IPDPlayer
    {
        public override string Name => "Adaptive";

        private Queue<IPDGame.Choices> _initialChoices;

        private int _totalDefectScore;
        private int _totalCooperateScore;

        private Object _choiceLock = new Object();

        public IPDPlayerAdaptive()
        {

        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            lock (_choiceLock)
            {
                IPDGame.Choices pr;
                int prt = game.T - 1;

                if (_initialChoices.Count > 0)
                    return _initialChoices.Dequeue();
                else
                {
                    pr = game.GetChoice(this, prt);

                    if (pr == IPDGame.Choices.C)
                        _totalCooperateScore += (int)game.GetPast(this, prt);
                    else
                        _totalDefectScore += (int)game.GetPast(this, prt);

                    if (_initialChoices.Count > 0)
                        return _initialChoices.Dequeue();
                    else
                        return (_totalDefectScore > _totalCooperateScore) ? IPDGame.Choices.D : IPDGame.Choices.C;
                }
            }
        }

        public override void Reset()
        {
            _initialChoices = new Queue<IPDGame.Choices>();
            for (int i = 0; i < 6; i++)
                _initialChoices.Enqueue(IPDGame.Choices.C);
            for (int i = 0; i < 5; i++)
                _initialChoices.Enqueue(IPDGame.Choices.D);

            _totalDefectScore = 0;
            _totalCooperateScore = 0;
        }
    }
}
