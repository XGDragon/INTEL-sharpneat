using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Linq;
using System.Collections.Generic;
using SharpNeat.Network;
using SharpNeat.Genomes.Neat;
using System.Collections;
using Priority_Queue;
using System;

namespace SharpNeat.Domains.IPD
{
    class IPDEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        private const int ARCHIVE_MAX_SIZE = 200;
        private const bool ALLOW_NOVELTY = true;

        public ulong EvaluationCount { get; private set; }

        public bool StopConditionSatisfied { get { return false; } }
        //need a way to calculate max fitness..

        private bool _noveltyMode = false;
        
        private IPDExperiment.Info _info;
        
        private Object _archiveLock = new Object();
        private List<Behavior> _temporaryArchive = new List<Behavior>();
        private List<Behavior> _archive = new List<Behavior>(ARCHIVE_MAX_SIZE + 1);

        public IPDEvaluator(IPDExperiment.Info info)
        {
            _info = info;
            Behavior.InitializePermutations(info);
        }

        //draw graphs of scoring for each player using DomainView thingy (preycapture)

        /// <summary>
        /// Evaluate the provided IBlackBox against the IPD problem domain and return its fitness score.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox phenome)
        {            
            EvaluationCount++;

            _noveltyMode = (ALLOW_NOVELTY && _archive.Count == ARCHIVE_MAX_SIZE && _info.CurrentGeneration > ARCHIVE_MAX_SIZE * 2);

            double objectiveFitness = EvaluateObjectively(phenome);
            double noveltyFitness = EvaluateNovelty(phenome);

            return (_noveltyMode)
                ? new FitnessInfo((_info.OpponentPool.Length * _info.NumberOfGames * 500) + noveltyFitness, objectiveFitness)
                : new FitnessInfo(objectiveFitness, noveltyFitness);
        }

        private double EvaluateObjectively(IBlackBox phenome)
        {
            Players.IPDPlayerPhenome p = new Players.IPDPlayerPhenome(phenome);
            double[] score = new double[_info.OpponentPool.Length];
            for (int i = 0; i < _info.OpponentPool.Length; i++)
            {
                phenome.ResetState();
                IPDGame g = new IPDGame(_info.NumberOfGames, p, _info.OpponentPool[i]);

                g.Run();
                score[i] += g.GetScore(p);
            }

            return score.Sum();
        }

        private double EvaluateNovelty(IBlackBox box)
        {
            //calculate doubles as per protocol
            //calculate distance to each behavior in archive
            //pick (3) nearest ones, calculate average; novelty score is this average
            //3 is rather ideal as per NetLogo simulation, probably due to proper triangulation
            //remember novelty scores for each generation, put in X top scores, remove X lowest scores

            Behavior b = new Behavior(box); //calculate doubles
            b.CalculateNovelty(_archive);

            lock (_archiveLock)
            {
                if (_info.HasNewGenerationOccured())
                {
                    Behavior mostNovel;
                    mostNovel = _temporaryArchive.Max();
                    //recalculate novelty within archive wrt new member 
                    _archive.ForEach(a => { a.RecalculateNovelty(mostNovel); });
                    //add the most novel to the archive
                    _archive.Add(mostNovel);
                    //if above archive max, remove the least novel from the archive
                    if (_archive.Count > ARCHIVE_MAX_SIZE)
                        _archive.Remove(_archive.Min());
                    //a new generation requires a new tempArchive to store all phenome behavior in
                    _temporaryArchive = new List<Behavior>();
                }
                _temporaryArchive.Add(b);
            }

            return b.Novelty;
        }



        private class Behavior : FastPriorityQueueNode, IComparable<Behavior>
        {
            private const int NearestK = 3;

            private static double[][] _permutations;
            public static void InitializePermutations(IPDExperiment.Info info)
            {
                int max = (int)System.Math.Pow(2, info.InputCount);
                _permutations = new double[max][];
                for (int i = 0; i < max; i++)
                {
                    _permutations[i] = new double[info.InputCount];
                    int b = i;
                    for (int j = info.InputCount - 1; j >= 0; j--)
                    {
                        _permutations[i][j] = b % 2;
                        b /= 2;
                    }
                }
            }

            public double Novelty { get; private set; }

            private double[] _behaviors;
            private List<Distance> _distances = new List<Distance>();

            public Behavior(IBlackBox box)
            {
                _behaviors = new double[_permutations.Length * box.OutputCount];

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
            //can avoid constantly recreating the priority queues by using QueueIndex and returned top QueueIndexers
            public void CalculateNovelty(List<Behavior> archive)
            {
                if (archive.Count > 0)
                {
                    FastPriorityQueue<Distance> dists = new FastPriorityQueue<Distance>(archive.Count + 1);

                    for (int i = 0; i < archive.Count; i++)
                    {
                        Distance d = new Distance(this, archive[i]);
                        _distances.Add(d);
                        dists.Enqueue(d, d.Priority);
                    }

                    Novelty = GetNearestDistances(dists).Average();
                }
                else
                    Novelty = 0;
            }

            public void RecalculateNovelty(Behavior newMember)
            {
                _distances.Add(new Distance(this, newMember));
                FastPriorityQueue<Distance> dists = new FastPriorityQueue<Distance>(_distances.Count);

                for (int i = 0; i < _distances.Count; i++)
                    dists.Enqueue(_distances[i], _distances[i].Priority);
                
                Novelty = GetNearestDistances(dists).Average();
            }

            private float[] GetNearestDistances(FastPriorityQueue<Distance> dists)
            {
                float[] nearest = new float[Math.Min(NearestK, dists.Count)];
                for (int i = 0; i < nearest.Length; i++)
                    nearest[i] = dists.Dequeue().Priority;
                return nearest;
            }

            public int CompareTo(Behavior other)
            {
                if (this.Novelty > other.Novelty)
                    return 1;
                else if (this.Novelty < other.Novelty)
                    return -1;
                return 0;
            }

            private class Distance : FastPriorityQueueNode
            {
                public Behavior Behavior { get; private set; }

                public Distance(Behavior self, Behavior other)
                {
                    Priority = (float)MathNet.Numerics.Distance.SAD(self._behaviors, other._behaviors);
                    //self is the owner of this object
                    Behavior = other;
                }
            }
        }

        //private double EvaluateNovelty(IBlackBox box)
        //{
        //if (false)//MIX == 1.0)
        //    return 0;

        //int gen = (int)_generation();
        //lock (_behaviorLock)
        //{
        //    if (gen > _currentGeneration)
        //    {
        //        _behaviorArchive[_currentGeneration].Sort();
        //        List<Behavior> newList = new List<Behavior>();
        //        for (int i = 0; i < NOVELTY_CULL; i++)
        //            newList[i] = _behaviorArchive[_currentGeneration][i];
        //        _behaviorArchive[_currentGeneration] = newList;

        //        _currentGeneration = gen;
        //        _behaviorArchive.Add(new List<Behavior>());
        //    }
        //}


        ////http://eplex.cs.ucf.edu/noveltysearch/userspage/#howtoimplement
        ////http://eplex.cs.ucf.edu/papers/lehman_cec11.pdf 3/8

        //Behavior b = new Behavior(box);
        //_behaviorArchive[gen].Add(b);

        //for (int i = 0; i < _currentGeneration; i++)
        //{
        //    for (int j = 0; j < _behaviorArchive[i].Count; j++)
        //        b.Novelty += _behaviorArchive[i][j].DistanceTo(b);
        //}
        ////http://antoniosliapis.com/papers/constrained_novelty_search.pdf
        ////http://accord-framework.net/docs/html/T_Accord_MachineLearning_KNearestNeighbors_1.htm

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
        //return b.Novelty;
        //}

        public void Reset()
        {

        }

        //private class Behavior : IComparable<Behavior>
        //{
        //    private static (int input, int output) _io;
        //    private static double[][] _permutations;

        //    private double[] _behaviors;
        //    public double Novelty { get; set; }

        //    public Behavior(IBlackBox box)
        //    {
        //        _behaviors = new double[_permutations.Length * _io.output];

        //        int b = 0;
        //        for (int i = 0; i < _permutations.Length; i++, b += 2)
        //        {
        //            box.ResetState();
        //            box.InputSignalArray.CopyFrom(_permutations[i], 0);

        //            box.Activate();
        //            if (!box.IsStateValid)
        //                continue;

        //            box.OutputSignalArray.CopyTo(_behaviors, b);
        //        }
        //    }

        //    public double DistanceTo(Behavior b)
        //    {
        //        return MathNet.Numerics.Distance.SAD(this._behaviors, b._behaviors);
        //    }

        //    public static void InitializeNovelty((int input, int output) io)
        //    {
        //        _io = io;

        //        InitializePermutations();
        //        //InitializeForest();
        //    }

        //    private static void InitializePermutations()
        //    {
        //        int max = (int)System.Math.Pow(2, _io.input);
        //        _permutations = new double[max][];
        //        for (int i = 0; i < max; i++)
        //        {
        //            _permutations[i] = new double[_io.input];
        //            int b = i;
        //            for (int j = _io.input - 1; j >= 0; j--)
        //            {
        //                _permutations[i][j] = b % 2;
        //                b /= 2;
        //            }
        //        }
        //    }

        //    public int CompareTo(Behavior other)
        //    {
        //        if (this.Novelty > other.Novelty)
        //            return 1;
        //        else if (this.Novelty < other.Novelty)
        //            return -1;
        //        else
        //            return 0;
        //    }

        //    //private static void InitializeForest()
        //    //{
        //    //    _forest = new List<Accord.Collections.KDTree<double>[]>();

        //    //    NewTreeForest();
        //    //}

        //    //public static void NewTreeForest()
        //    //{
        //    //    //input / 2 gives the "reach" of the NN, which equals the number of trees
        //    //    _forest.Add(new Accord.Collections.KDTree<double>[_io.input / 2]);
        //    //    int forestEnd = _forest.Count - 1;

        //    //    //Always 8, because 2^2 inputs has 4 combinations which generate 2 outputs each (2 * 4 = 8)
        //    //    for (int i = 0; i < _forest[forestEnd].Length; i++)
        //    //        _forest[forestEnd][i] = new Accord.Collections.KDTree<double>(8);
        //    //}
        //}
    }
}
