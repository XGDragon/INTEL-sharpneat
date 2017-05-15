using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD
{
    abstract class IPDPlayer
    {
        protected static Random _r = new Random();

        public abstract string Name { get; }

        private Dictionary<IPDPlayer, double> _scoreCard = new Dictionary<IPDPlayer, double>();

        public double TotalScore()
        {
            return _scoreCard.Sum(c => c.Value);
        }

        public void AddScore(IPDPlayer opponent, double score)
        {
            _scoreCard.Add(opponent, score);
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract IPDGame.Choices Choice(IPDGame game);

        public delegate IPDGame.Choices[] QFunction(int alpha, int beta);
    }
}
