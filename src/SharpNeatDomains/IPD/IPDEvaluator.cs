using SharpNeat.Core;
using SharpNeat.Phenomes;
using System.Linq;
using System.Collections.Generic;
using SharpNeat.Network;
using SharpNeat.Genomes.Neat;
using System.Collections;
using Priority_Queue;
using MathNet;
using System;

namespace SharpNeat.Domains.IPD
{
    class IPDEvaluator : IPhenomeEvaluator<IBlackBox>
    {
        public ulong EvaluationCount { get; private set; }

        private Object _stopLock = new Object();
        private bool _stopConditionSatisfied = false;
        public bool StopConditionSatisfied { get { return _stopConditionSatisfied; } }
        
        private IPDExperiment.Info _info;

        private double _noveltyFitness = 1.0d;
        private Object _archiveLock = new Object();
        private List<PhenomeInfo> _archive = new List<PhenomeInfo>();
        private double _archiveThreshold;
        private double _archiveThresholdFactor = 1.0d;

        public IPDEvaluator(ref IPDExperiment.Info info)
        {
            _info = info;
            _info.BestNoveltyGenome = () => { var m = _archive.Max(); return m.Phenome; };
            _info.Archive = () => { return _archive; };
            _info.Evaluations = () => { return EvaluationCount; };
            PhenomeInfo.Initialize(info);
        }

        /// <summary>
        /// Evaluate the provided IBlackBox against the IPD problem domain and return its fitness score.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            EvaluationCount++;
            //int gen = _info.CurrentGeneration;

            PhenomeInfo pi = EvaluateBehavior(phenome);
            double noveltyFitness = EvaluateNovelty(pi);

            double primaryFitness;
            switch (_info.EvaluationMode)
            {
                case IPDExperiment.EvaluationMode.Novelty:
                    primaryFitness = noveltyFitness; break;
                case IPDExperiment.EvaluationMode.Rank:
                    primaryFitness = pi.Rank; break;
                default:
                case IPDExperiment.EvaluationMode.Score:
                    primaryFitness = pi.Score; break;
            }

            lock (_stopLock)
            {
                if (_info.CurrentGeneration == 500)
                    _stopConditionSatisfied = true;
            }

            return new FitnessInfo(primaryFitness, pi.AuxiliaryFitnessInfo);
            
            //if (_info.EvaluationMode == IPDExperiment.EvaluationMode.Rank && pi.Rank == 1.0d && gen > 0)
            //{
            //    lock (_stopLock)
            //    {
            //        _stopConditionSatisfied = true;
            //        return new FitnessInfo(_info.BestFitness * 10, pi.Score);
            //    }
            //}
        }

        private PhenomeInfo EvaluateBehavior(IBlackBox phenome)
        {
            Players.IPDPlayerPhenome p = new Players.IPDPlayerPhenome(phenome);
            double[] scores = new double[_info.OpponentPool.Length + 1];
            int phenomeIndex = _info.OpponentPool.Length;
            IPDGame[] games = new IPDGame[phenomeIndex];

            for (int i = 0; i < phenomeIndex; i++)
            {
                games[i] = new IPDGame(_info.NumberOfGames, _info.OpponentPool[i], p);
                var s = games[i].Evaluate(_info.RandomRobustCheck);
                scores[i] += s.a + _info.OpponentScores[i];
                scores[phenomeIndex] += s.b;
            }

            double score = scores[phenomeIndex];
            var ranks = scores.Rank();
            double rank = (ranks[phenomeIndex] - 1) / ((double)ranks.Length - 1);
            if (rank == 1.0d)
            {
                //Ties are not allowed
                for (int i = 0; i < scores.Length; i++)
                    if (scores[i] == scores[phenomeIndex] && i != phenomeIndex)
                        rank -= 0.05d;
            }

            return new PhenomeInfo(phenome, rank, score, games);
        }

        private double EvaluateNovelty(PhenomeInfo info)
        {
            //if (_info.EvaluationMode != IPDExperiment.EvaluationMode.Novelty)
            //    return 0;

            int ii = 0;
            double CalculateNovelty(PhenomeInfo pi)
            {
                _archive.Sort((a, b) => a.Distance(pi).CompareTo(b.Distance(pi)));
                double dist = 0;
                for (int i = 0; i < _info.NoveltyK; i++)
                    dist += _archive[i + ii].Distance(pi);
                return dist / _info.NoveltyK;
            }

            if (_archiveThresholdFactor >= 1.1)
            {
                _archiveThreshold *= _archiveThresholdFactor;
                _archiveThresholdFactor = 1.0d;
            }

            lock (_archiveLock)
            {
                info.ID = _archive.Count;
                if (_archive.Count <= _info.NoveltyK)
                {
                    _archive.Add(info);
                    ii = 1;
                    if (_archive.Count == _info.NoveltyK + 1)    //last free add
                        _archiveThreshold = _archive.Average(a => { return CalculateNovelty(a); });
                    return 0;
                }
                else
                {
                    double novelty = CalculateNovelty(info);
                    double threshold = _archiveThreshold * _archiveThresholdFactor;
                    if (novelty < threshold)
                    {
                        //not a novel phenome
                        _archiveThresholdFactor -= 0.002;
                        return (novelty / threshold) * _noveltyFitness;
                    }
                    else
                    {
                        //novel phenome
                        _archiveThresholdFactor += 0.05;
                        _archive.Add(info);
                        return ++_noveltyFitness;
                        //if (_archive.Count > 100)
                        //    _archive.RemoveAt(0);
                    }
                }
            }
        }

        public void Reset()
        {

        }

        public class PhenomeInfo : IComparable<PhenomeInfo>
        {
            private static IPDExperiment.EvaluationMode _evaluationMode { get; set; }
            private static IPDExperiment.NoveltyMetric _metric { get; set; }

            public static void Initialize(IPDExperiment.Info info)
            {
                _evaluationMode = info.EvaluationMode;
                switch (_metric = info.NoveltyMetric)
                {
                    default:
                    case IPDExperiment.NoveltyMetric.Score:
                        _distance = (a, b) => { return Math.Abs(a.Score - b.Score); }; break;
                    case IPDExperiment.NoveltyMetric.Choice:
                    case IPDExperiment.NoveltyMetric.Past:
                        _distance = (a, b) => { return MathNet.Numerics.Distance.SSD(a._pc, b._pc); }; break;
                }
            }

            private delegate double DistanceMetric(PhenomeInfo a, PhenomeInfo b);
            private static DistanceMetric _distance;

            public IBlackBox Phenome { get; private set; }
            public double Rank { get; private set; }
            public double Score { get; private set; }
            public IPDGame[] Games { get; private set; }
            public AuxFitnessInfo[] AuxiliaryFitnessInfo { get { _aux[0]._value = Score; _aux[1]._value = Rank; return _aux; } }

            public long ID { get; set; }

            private double[] _pc;
            private AuxFitnessInfo[] _aux;

            public PhenomeInfo(IBlackBox phenome, double rank, double score, IPDGame[] games)
            {
                Phenome = phenome;
                Rank = rank;
                Score = score;
                Games = games;
                _aux = new AuxFitnessInfo[2] { new AuxFitnessInfo("Score", 0), new AuxFitnessInfo("Rank", 0) };

                if (_metric == IPDExperiment.NoveltyMetric.Choice)
                {
                    _pc = new double[2];
                    for (int i = 0; i < games.Length; i++)
                    {
                        double[] choices = games[i].GetChoices(games[i].A);
                        for (int j = 0; j < _pc.Length; j++)
                            _pc[j] += choices[j];
                    }
                }
                if (_metric == IPDExperiment.NoveltyMetric.Past)
                {
                    _pc = new double[4];
                    for (int i = 0; i < games.Length; i++)
                    {
                        double[] pasts = games[i].GetPasts(games[i].A);
                        for (int j = 0; j < _pc.Length; j++)
                            _pc[j] += pasts[j];
                    }
                }
            }            

            public double Distance(PhenomeInfo other)
            {
                return _distance(this, other);
            }

            public override string ToString()
            {
                return ID + "; Score: " + Score.ToString() + ", Rank: " + Rank.ToString();
            }

            public int CompareTo(PhenomeInfo other)
            {
                if (Rank > other.Rank)
                    return 1;
                if (Rank < other.Rank)
                    return -1;
                if (Rank == other.Rank)
                {
                    if (Score > other.Score)
                        return 1;
                    if (Score < other.Score)
                        return -1;
                    return 0;
                }
                return 0;
            }
        }        
    }
}
