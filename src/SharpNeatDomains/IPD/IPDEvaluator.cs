using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Collections.Generic;

namespace SharpNeat.Domains.IPD
{
    class IPDEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        public ulong EvaluationCount { get; private set; }

        public bool StopConditionSatisfied { get { return false; } }

        int _numberOfGames;
        IPDPlayer[] _players;

        public IPDEvaluator(int numberOfGames, IPDExperiment.OpponentPool pool)
        {
            _numberOfGames = numberOfGames;

            switch (pool)
            {
                case IPDExperiment.OpponentPool.AllC_Only:
                    _players = new IPDPlayer[] { Players.IPDPlayerFactory.AllC }; break;
                case IPDExperiment.OpponentPool.AllD_Only:
                    _players = new IPDPlayer[] { Players.IPDPlayerFactory.AllC }; break;
                case IPDExperiment.OpponentPool.TripleThreat:
                    _players = new IPDPlayer[] {
                        Players.IPDPlayerFactory.AllC,
                        Players.IPDPlayerFactory.AllD,
                        Players.IPDPlayerFactory.TFT
                    }; break;
                case IPDExperiment.OpponentPool.TFTFails:
                    _players = new IPDPlayer[] {
                        Players.IPDPlayerFactory.AllC,
                        Players.IPDPlayerFactory.AllD,
                        Players.IPDPlayerFactory.TFT,
                        Players.IPDPlayerFactory.STFT
                    }; break;
            }

            for (int i = 0; i < _players.Length; i++)
                for (int j = 1; j < _players.Length; j++)
                {
                    var g = new IPDGame(_numberOfGames, _players[i], _players[j]);
                    g.Run();
                }
            //(0.5)(n - 1)n
        }

        //draw graphs of scoring for each player using DomainView thingy (preycapture)

        /// <summary>
        /// Evaluate the provided IBlackBox against the IPD problem domain and return its fitness score.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox phenome)
        {            
            EvaluationCount++;

            Players.IPDPlayerPhenome p = new Players.IPDPlayerPhenome(phenome);
            IPDGame g;
            for (int i = 0; i < _players.Length; i++)
            {
                phenome.ResetState();
                g = new IPDGame(_numberOfGames, p, _players[i]);
                g.Run(true);
            }

            double ts = p.TotalScore();

            //if (ts == )

            return new FitnessInfo(ts, ts);
        }

        /// <summary>
        /// Reset the internal state of the evaluation scheme if any exists.
        /// Note. The XOR problem domain has no internal state. This method does nothing.
        /// </summary>
        public void Reset()
        {

        }
    }
}
