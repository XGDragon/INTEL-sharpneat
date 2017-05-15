using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Collections.Generic;

namespace SharpNeat.Domains.IPD
{
    class IPDEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        public ulong EvaluationCount { get; private set; }

        public bool StopConditionSatisfied { get { return false; } }
        //need a way to calculate max fitness..

        int _numberOfGames;
        IPDPlayer[] _players;

        public IPDEvaluator(int numberOfGames, params IPDPlayer[] pool)
        {
            _numberOfGames = numberOfGames;
            _players = pool;            

            for (int i = 0; i < _players.Length; i++)
                for (int j = i + 1; j < _players.Length; j++)
                    if (i != j) //currently not against each other but.. 
                        new IPDGame(_numberOfGames, _players[i], _players[j]).Run();
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
            for (int i = 0; i < _players.Length; i++)
            {
                phenome.ResetState();
                new IPDGame(_numberOfGames, p, _players[i]).Run(true);
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
