using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Phenomes;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerProber : IPDPlayer
    {
        public override string Name => "Prober" + _alternative.ToString();

        public enum Probes { One, Two, Three, Hard }

        private Queue<IPDGame.Choices> _initialChoices;
        private Probes _alternative;
        private int C_D_TFT;

        private Object _choiceLock = new Object();

        public IPDPlayerProber(Probes type = Probes.One)
        {
            _alternative = type;
        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            lock (_choiceLock)
            {
                if (_initialChoices.Count > 0)
                    return _initialChoices.Dequeue();
                else
                {
                    if (C_D_TFT == -1)
                    {
                        switch (_alternative)
                        {
                            case Probes.One:
                            default:
                                C_D_TFT = (C(game, 1) && C(game, 2)) ? 1 : 2; break;
                            case Probes.Two:
                                C_D_TFT = (!C(game, 1) && C(game, 2)) ? 0 : 2; break;
                            case Probes.Three:
                                C_D_TFT = (C(game, 1)) ? 1 : 2; break;
                            case Probes.Hard:
                                C_D_TFT = (C(game, 1) && C(game, 2)) ? 1 : 2; break;
                        }
                    }

                    if (C_D_TFT == 0)
                        return IPDGame.Choices.C;
                    else if (C_D_TFT == 1)
                        return IPDGame.Choices.D;
                    else
                        return C(game, game.T - 1) ? IPDGame.Choices.C : IPDGame.Choices.D;
                }
            }
        }

        private bool C(IPDGame game, int time)
        {
            return (game.GetPast(this, time) == IPDGame.Past.R || game.GetPast(this, time) == IPDGame.Past.T);
        }

        public override void Reset()
        {
            C_D_TFT = -1;

            List<IPDGame.Choices> c = new List<IPDGame.Choices>();

            switch(_alternative)
            {
                case Probes.Three:
                    c.AddRange(new IPDGame.Choices[] { IPDGame.Choices.D, IPDGame.Choices.C }); break;
                case Probes.Hard:
                    c.AddRange(new IPDGame.Choices[] { IPDGame.Choices.D, IPDGame.Choices.D, IPDGame.Choices.C, IPDGame.Choices.C }); break;
                default:
                    c.AddRange(new IPDGame.Choices[] { IPDGame.Choices.D, IPDGame.Choices.C, IPDGame.Choices.C }); break;
            }

            _initialChoices = new Queue<IPDGame.Choices>(c);
        }
    }
}
