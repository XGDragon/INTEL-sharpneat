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

        int _currentGeneration;
        CurrentGeneration _generation;

        Object _behaviorLock;
        List<List<Behavior>> _behaviorArchive;

        public IPDEvaluator(CurrentGeneration generationGetter, (int, int) io, int numberOfGames, params IPDPlayer[] pool)
        {
            _generation = generationGetter;
            _currentGeneration = (int)_generation();

            _numberOfGames = numberOfGames;
            _players = pool;

            Behavior.InitializeNovelty(io);
            _behaviorLock = new Object();
            _behaviorArchive = new List<List<Behavior>>() { new List<Behavior>() };
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

        private const int NOVELTY_CULL = 5;
        
        private double EvaluateNovelty(IBlackBox box)
        {
            if (false)//MIX == 1.0)
                return 0;
            
            int gen = (int)_generation();
            lock (_behaviorLock)
            {
                if (gen > _currentGeneration)
                {
                    _behaviorArchive[_currentGeneration].Sort();
                    List<Behavior> newList = new List<Behavior>();
                    for (int i = 0; i < NOVELTY_CULL; i++)
                        newList[i] = _behaviorArchive[_currentGeneration][i];
                    _behaviorArchive[_currentGeneration] = newList;

                    _currentGeneration = gen;
                    _behaviorArchive.Add(new List<Behavior>());
                }
            }


            //http://eplex.cs.ucf.edu/noveltysearch/userspage/#howtoimplement
            //http://eplex.cs.ucf.edu/papers/lehman_cec11.pdf 3/8

            Behavior b = new Behavior(box);
            _behaviorArchive[gen].Add(b);

            for (int i = 0; i < _currentGeneration; i++)
            {
                for (int j = 0; j < _behaviorArchive[i].Count; j++)
                    b.Novelty += _behaviorArchive[i][j].DistanceTo(b);
            }

            //http://accord-framework.net/docs/html/T_Accord_MachineLearning_KNearestNeighbors_1.htm

            //MathNet.Numerics.Distance.SAD()
            //check behavior for each possible input state?
            //somehow create a distance metric from the resulting vector
            //remember interesting metrics to create k-distance measuring thing

            //consider 4 inputs, S_-1, O_-1, S_-2, O_-2 => 2^4 = 16 combinations
            //a behavior is then the output for each of those combinations => 16 arrays of { C, D }

            //OPTION A: 
            //  we flatten the combinations to an kdTree with 32 dimensions. 
            //  kmeans sparseness is novelty.
            //OPTION B: << current choice
            //  for each S_n, O_n pair create seperate tree kdTree_n (2^2 = 4 combinations, flatten outputs to 8 dimensions)
            //  kmeans sparseness per kdTree_n. sum is novelty, but can adjust with weights possibly
            //  archive each kdTree_n in a seperate generation

            //remember sparse individuals in an archive by generation..
            return b.Novelty;
        }
        
        public void Reset()
        {
            int d = 0;
        }

        private class Behavior : IComparable<Behavior>
        {
            private static (int input, int output) _io;
            private static double[][] _permutations;

            private double[] _behaviors;
            public double Novelty { get; set; }

            public Behavior(IBlackBox box)
            {
                _behaviors = new double[_permutations.Length * _io.output];

                int b = 0;
                for (int i = 0; i < _permutations.Length; i++, b += 2)
                {
                    box.ResetState();
                    box.InputSignalArray.CopyFrom(_permutations[i], 0);

                    box.Activate();
                    if (!box.IsStateValid)
                        continue;

                    box.OutputSignalArray.CopyTo(_behaviors, b);
                }
            }

            public double DistanceTo(Behavior b)
            {
                return MathNet.Numerics.Distance.SAD(this._behaviors, b._behaviors);
            }

            public static void InitializeNovelty((int input, int output) io)
            {
                _io = io;

                InitializePermutations();
                //InitializeForest();
            }

            private static void InitializePermutations()
            {
                int max = (int)System.Math.Pow(2, _io.input);
                _permutations = new double[max][];
                for (int i = 0; i < max; i++)
                {
                    _permutations[i] = new double[_io.input];
                    int b = i;
                    for (int j = _io.input - 1; j >= 0; j--)
                    {
                        _permutations[i][j] = b % 2;
                        b /= 2;
                    }
                }
            }

            public int CompareTo(Behavior other)
            {
                if (this.Novelty > other.Novelty)
                    return 1;
                else if (this.Novelty < other.Novelty)
                    return -1;
                else
                    return 0;
            }

            //private static void InitializeForest()
            //{
            //    _forest = new List<Accord.Collections.KDTree<double>[]>();

            //    NewTreeForest();
            //}

            //public static void NewTreeForest()
            //{
            //    //input / 2 gives the "reach" of the NN, which equals the number of trees
            //    _forest.Add(new Accord.Collections.KDTree<double>[_io.input / 2]);
            //    int forestEnd = _forest.Count - 1;

            //    //Always 8, because 2^2 inputs has 4 combinations which generate 2 outputs each (2 * 4 = 8)
            //    for (int i = 0; i < _forest[forestEnd].Length; i++)
            //        _forest[forestEnd][i] = new Accord.Collections.KDTree<double>(8);
            //}
        }
    }
}
