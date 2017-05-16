using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Linq;
using System.Collections.Generic;
using SharpNeat.Network;
using SharpNeat.Genomes.Neat;
using System.Collections;
using MathNet.Numerics;
using Accord;
using System;

namespace SharpNeat.Domains.IPD
{
    class IPDEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        /// <summary>
        /// 0.0 is full novelty
        /// </summary>
        const double MIX = 1.0; 

        public ulong EvaluationCount { get; private set; }

        public bool StopConditionSatisfied { get { return false; } }
        //need a way to calculate max fitness..

        public delegate uint CurrentGeneration();
        
        int _numberOfGames;
        IPDPlayer[] _players;
        CurrentGeneration _generation;

        public IPDEvaluator(CurrentGeneration generationGetter, int inputLength, int numberOfGames, params IPDPlayer[] pool)
        {
            _generation = generationGetter;
            _numberOfGames = numberOfGames;
            _players = pool;

            Behavior.InitializeNovelty(inputLength);
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

        private Dictionary<uint, List<Behavior>> _behaviorPerGeneration = new Dictionary<uint, List<Behavior>>();
        private Object _listLock = new Object();

        private double EvaluateNovelty(IBlackBox box)
        {
            if (false)//MIX == 1.0)
                return 0;

            //http://eplex.cs.ucf.edu/noveltysearch/userspage/#howtoimplement
            //http://eplex.cs.ucf.edu/papers/lehman_cec11.pdf 3/8

            uint gen = _generation();
            lock (_listLock)
            {
                if (!_behaviorPerGeneration.ContainsKey(gen))
                {
                    _behaviorPerGeneration.Add(gen, new List<Behavior>());
                    if (gen != 0)
                    {
                        //remove all but the most prominent behaviors (thin the clusters)
                        //maximum distance is equal to inputCount
                        var l = _behaviorPerGeneration[gen - 1];
                        
                    }
                }
            }

            Behavior b = new Behavior(box);
            //http://accord-framework.net/docs/html/T_Accord_MachineLearning_KNearestNeighbors_1.htm
            //Accord.MachineLearning.KNearestNeighbors k 

            //MathNet.Numerics.Distance.SAD()
            //check behavior for each possible input state?
            //somehow create a distance metric from the resulting vector
            //remember interesting metrics to create k-distance measuring thing

            //remember sparse individuals in an archive by generation..
            return 0;
        }
        
        public void Reset()
        {

        }

        private struct Behavior
        {
            private static double[][] _permutations;
            private static double _maxDistance;

            private double[] _behaviors;
            //private MathNet.Numerics.Distance d

            public Behavior(IBlackBox box)
            {
                _behaviors = new double[_permutations.Length * box.OutputCount];    //length * 2
                for (int i = 0, b = 0; i < _permutations.Length; i++, b += 2)
                {
                    box.ResetState();
                    box.InputSignalArray.CopyFrom(_permutations[i], 0);
                    //for (int j = 0; i < _permutations[i].Length; j++)
                    //    box.InputSignalArray[j] = _permutations[i][j];

                    box.Activate();
                    if (!box.IsStateValid)
                        continue;

                    box.OutputSignalArray.CopyTo(_behaviors, b);
                }
            }

            public static double CalculateDistance(Behavior a, Behavior b)
            {
                return Distance.SAD(a._behaviors, b._behaviors);
            }

            public static void InitializeNovelty(int length)
            {
                int max = (int)System.Math.Pow(2, length);
                _permutations = new double[max][];
                for (int i = 0; i < max; i++)
                {
                    _permutations[i] = new double[length];
                    int b = i;
                    for (int j = length - 1; j >= 0; j--)
                    {
                        _permutations[i][j] = b % 2;
                        b /= 2;
                    }
                }
            }
        }
    }
}
