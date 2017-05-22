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
        public ulong EvaluationCount { get; private set; }

        public bool StopConditionSatisfied { get { return _stopConditionSatisfied; } }

        private bool _noveltyMode = false;
        private bool _stopConditionSatisfied = false;
        
        private IPDExperiment.Info _info;
        
        private Object _archiveLock = new Object();
        private List<Behavior> _temporaryArchive = new List<Behavior>();
        private List<Behavior> _archive;

        public IPDEvaluator(IPDExperiment.Info info)
        {
            _info = info;
            _archive = new List<Behavior>(info.NoveltyArchiveSize + 1);

            Behavior.InitializePermutations(info);
        }

        /// <summary>
        /// Evaluate the provided IBlackBox against the IPD problem domain and return its fitness score.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox phenome)
        {            
            EvaluationCount++;
            
            switch (_info.NoveltyEvaluationMode)
            {
                case IPDExperiment.NoveltyEvaluationMode.Disable:
                    _noveltyMode = false; break;
                case IPDExperiment.NoveltyEvaluationMode.Immediate:
                    _noveltyMode = _info.CurrentGeneration > 0; break;
                case IPDExperiment.NoveltyEvaluationMode.ArchiveFull:
                    _noveltyMode = (_archive.Count == _info.NoveltyArchiveSize && _info.CurrentGeneration > _info.NoveltyArchiveSize); break;
                case IPDExperiment.NoveltyEvaluationMode.SlowArchiveFull:
                    _noveltyMode = (_archive.Count == _info.NoveltyArchiveSize && _info.CurrentGeneration > _info.NoveltyArchiveSize * 2); break;
            }

            double objectiveFitness = EvaluateObjectively(phenome);
            double noveltyFitness = EvaluateNovelty(phenome);

            _stopConditionSatisfied = (_info.ObjectiveEvaluationMode == IPDExperiment.ObjectiveEvaluationMode.Rank && objectiveFitness == 1.0d);

            return (_noveltyMode)
                ? new FitnessInfo(noveltyFitness, objectiveFitness)
                : new FitnessInfo(objectiveFitness, noveltyFitness);
        }

        private double EvaluateObjectively(IBlackBox phenome)
        {
            Players.IPDPlayerPhenome p = new Players.IPDPlayerPhenome(phenome);  
            if (_info.ObjectiveEvaluationMode == IPDExperiment.ObjectiveEvaluationMode.Rank)
            {
                int phenomeIndex = _info.OpponentPool.Length;
                double[] scores = new double[_info.OpponentPool.Length + 1];
                for (int i = 0; i < phenomeIndex; i++)
                {
                    IPDGame g = new IPDGame(_info.NumberOfGames, p, _info.OpponentPool[i]);
                    g.Run();

                    scores[i] += g.GetScore(_info.OpponentPool[i]) + _info.OpponentScores[i];
                    scores[phenomeIndex] += g.GetScore(p);
                }
                var ranks = scores.Rank();
                return ranks[phenomeIndex] / (double)ranks.Length;
            }
            else //IPDExperiment.ObjectiveEvaluationMode.Fitness
            {
                double score = 0;
                for (int i = 0; i < _info.OpponentPool.Length; i++)
                {
                    phenome.ResetState();
                    IPDGame g = new IPDGame(_info.NumberOfGames, p, _info.OpponentPool[i]);
                    g.Run();
                    score += g.GetScore(p);
                }
                return score;
            }
        }

        private double EvaluateNovelty(IBlackBox box)
        {
            if (_info.NoveltyEvaluationMode == IPDExperiment.NoveltyEvaluationMode.Disable)
                return 0;
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
                    if (_archive.Count > _info.NoveltyArchiveSize)
                        _archive.Remove(_archive.Min());
                    //a new generation requires a new tempArchive to store all phenome behavior in
                    _temporaryArchive = new List<Behavior>();
                }
                _temporaryArchive.Add(b);
            }            

            return b.Novelty + (_info.NumberOfGames * _info.OpponentPool.Length * (int)IPDGame.Past.T);
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
        ////http://eplex.cs.ucf.edu/noveltysearch/userspage/#howtoimplement
        ////http://eplex.cs.ucf.edu/papers/lehman_cec11.pdf 3/8
        

        public void Reset()
        {

        }
    }
}
