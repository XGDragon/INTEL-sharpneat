using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Linq;
using System.Collections.Generic;
using SharpNeat.Network;
using SharpNeat.Genomes.Neat;

namespace SharpNeat.Domains.IPD
{
    class IPDEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        const double MIX = 1.0; //0.0 is full novelty

        public ulong EvaluationCount { get; private set; }

        public bool StopConditionSatisfied { get { return false; } }
        //need a way to calculate max fitness..

        public delegate uint CurrentGeneration();
        
        int _numberOfGames;
        IPDPlayer[] _players;
        CurrentGeneration _generation;

        public IPDEvaluator(CurrentGeneration generationGetter, int numberOfGames, params IPDPlayer[] pool)
        {
            _generation = generationGetter;
            _numberOfGames = numberOfGames;
            _players = pool;
            //(0.5)(n - 1)n
        }

        //draw graphs of scoring for each player using DomainView thingy (preycapture)

        /// <summary>
        /// Evaluate the provided IBlackBox against the IPD problem domain and return its fitness score.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox phenome)
        {            
            EvaluationCount++;

            //novelty search ideas: give a blackbox all possible inputs (TTTTTT, TTTTTR etc), activate and check its outputs
            //combine novel fitness with objective fitness, where as pool grows, novelty becomes more important?
            //double[] avg = new double[100];
            //for (int i = 0; i < avg.Length; i++)
            //    avg[i] = EvaluateObjectively(phenome);
            //double objectiveFitness = avg.Average();
            double objectiveFitness = EvaluateObjectively(phenome);
            double noveltyFitness = EvaluateNovelty(phenome);

            double finalFitness = (objectiveFitness * MIX) + (noveltyFitness * (1.0 - MIX));
            
            return new FitnessInfo(finalFitness, objectiveFitness);
        }

        private double EvaluateObjectively(IBlackBox phenome)
        {
            Players.IPDPlayerPhenome p = new Players.IPDPlayerPhenome(phenome);
            double[] score = new double[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                phenome.ResetState();
                IPDGame g = new IPDGame(_numberOfGames, p, _players[i]);
                g.Run();
                score[i] += g.GetScore(p);
            }

            return score.Sum();
        }

        private double EvaluateNovelty(IBlackBox box)
        {
            //http://eplex.cs.ucf.edu/noveltysearch/userspage/#howtoimplement
            //http://eplex.cs.ucf.edu/papers/lehman_cec11.pdf 3/8

            //check behavior for each possible input state?
            //somehow create a distance metric from the resulting vector
            //remember interesting metrics to create k-distance measuring thing

            //remember sparse individuals in an archive by generation..
            return 0;
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
