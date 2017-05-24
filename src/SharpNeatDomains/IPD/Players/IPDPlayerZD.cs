using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Phenomes;
using System.Threading.Tasks;
using System.Threading;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerZD : IPDPlayer
    {
        private string _name;
        public override string Name => _name;

        private IPDGame.Choices _start;

        private double _r;
        private double _s;
        private double _t;
        private double _p;

        private bool _hasRandom;

        public IPDPlayerZD(string name, double r, double s, double t, double p, IPDGame.Choices startingChoice)
        {
            _name = name;
            _start = startingChoice;

            _r = r;
            _s = s;
            _t = t;
            _p = p;
            
            _hasRandom = (_r % 1 != 0 || _s % 1 != 0 || _t % 1 != 0 || _p % 1 != 0);
        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            if (game.T == 0)
            {
                game.HasRandom = _hasRandom;
                return _start;
            }
            
            switch (game.GetPast(this, game.T - 1))
            {
                case IPDGame.Past.T:
                    return CooperateProbability(_t);
                case IPDGame.Past.R:
                    return CooperateProbability(_r);
                case IPDGame.Past.P:
                    return CooperateProbability(_p);
                case IPDGame.Past.S:
                    return CooperateProbability(_s);
            }

            throw new Exception("How did we get here?");
        }

        private IPDGame.Choices CooperateProbability(double p)
        {
            if (p == 0)
                return IPDGame.Choices.D;
            else if (p == 1)
                return IPDGame.Choices.C;
            else
                return (R.Next() < p) ? IPDGame.Choices.C : IPDGame.Choices.D;
        }

        public override void Reset()
        {

        }

        private static class R
        {
            static int seed = Environment.TickCount;

            static readonly ThreadLocal<Random> random =
                new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

            public static double Next()
            {
                return random.Value.NextDouble();
            }
        }
    }
}
