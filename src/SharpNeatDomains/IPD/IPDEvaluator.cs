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

        private bool _stopConditionSatisfied = false;
        public bool StopConditionSatisfied { get { return _stopConditionSatisfied; } }
        
        private IPDExperiment.Info _info;
        
        private Object _archiveLock = new Object();
        private List<PhenomeInfo> _archive = new List<PhenomeInfo>();
        private double _archiveThreshold;
        private double _archiveThresholdFactor = 1.0d;

        public IPDEvaluator(IPDExperiment.Info info)
        {
            _info = info;
            PhenomeInfo.Initialize(info);
        }

        /// <summary>
        /// Evaluate the provided IBlackBox against the IPD problem domain and return its fitness score.
        /// </summary>
        public FitnessInfo Evaluate(IBlackBox phenome)
        {            
            EvaluationCount++;

            //switch (_info.NoveltyEvaluationMode)
            //{
            //    case IPDExperiment.NoveltyEvaluationMode.Disable:
            //        _noveltyMode = false; break;
            //    case IPDExperiment.NoveltyEvaluationMode.Immediate:
            //        _noveltyMode = _info.CurrentGeneration > 0; break;
            //    case IPDExperiment.NoveltyEvaluationMode.ArchiveFull:
            //        _noveltyMode = (_archive.Count == _info.NoveltyArchiveSize && _info.CurrentGeneration > _info.NoveltyArchiveSize); break;
            //    case IPDExperiment.NoveltyEvaluationMode.SlowArchiveFull:
            //        _noveltyMode = (_archive.Count == _info.NoveltyArchiveSize && _info.CurrentGeneration > _info.NoveltyArchiveSize * 2); break;
            //}

            //_stopConditionSatisfied = (pi.Rank == 1.0d);

            var pi = EvaluateBehavior(phenome);
            double objectiveFitness = pi.Fitness;
            double noveltyFitness = EvaluateNovelty(pi);

            if (pi.Rank == 1.0d)
            {
                _stopConditionSatisfied = true;
                return new FitnessInfo(double.MaxValue, pi.Score);
            }

            return (_info.NoveltyEvaluationMode == IPDExperiment.NoveltyEvaluationMode.Immediate)
                ? new FitnessInfo(noveltyFitness, objectiveFitness)
                : new FitnessInfo(objectiveFitness, noveltyFitness);
        }

        private PhenomeInfo EvaluateBehavior(IBlackBox phenome)
        {
            Players.IPDPlayerPhenome p = new Players.IPDPlayerPhenome(phenome);
            double[] scores = new double[_info.OpponentPool.Length + 1];
            int phenomeIndex = _info.OpponentPool.Length;
            IPDGame[] games = new IPDGame[phenomeIndex];

            for (int i = 0; i < phenomeIndex; i++)
            {
                games[i] = new IPDGame(_info.NumberOfGames, p, _info.OpponentPool[i]);
                games[i].Run();

                scores[i] += games[i].GetScore(_info.OpponentPool[i]) + _info.OpponentScores[i];
                scores[phenomeIndex] += games[i].GetScore(p);
            }

            double score = scores[phenomeIndex];
            var ranks = scores.Rank();
            double rank = ranks[phenomeIndex] / (double)ranks.Length;
            if (rank == 1.0d)
            {
                for (int i = 0; i < scores.Length; i++)
                    if (scores[i] == scores[phenomeIndex] && i != phenomeIndex)
                        rank -= 0.01d;
            }

            return new PhenomeInfo(rank, score, games);
        }

        private double EvaluateNovelty(PhenomeInfo info)
        {
            if (_info.NoveltyEvaluationMode == IPDExperiment.NoveltyEvaluationMode.Disable)
                return 0;

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
                    }
                    else
                    {
                        //novel phenome
                        _archiveThresholdFactor += 0.05;
                        _archive.Add(info);
                    }
                    return novelty;
                }
            }
        }

        public void Reset()
        {

        }

        public class PhenomeInfo
        {
            private static IPDExperiment.ObjectiveEvaluationMode _evaluationMode { get; set; }
            private static IPDExperiment.NoveltyMetric _metric { get; set; }

            public static void Initialize(IPDExperiment.Info info)
            {
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

            public double Rank { get; private set; }
            public double Score { get; private set; }
            public IPDGame[] Games { get; private set; }
            public double Fitness { get { return (_evaluationMode == IPDExperiment.ObjectiveEvaluationMode.Rank) ? Rank : Score; } }

            private double[] _pc;

            public PhenomeInfo(double rank, double score, IPDGame[] games)
            {
                Rank = rank;
                Score = score;
                Games = games;

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
                return "Score: " + Score.ToString();
            }
        }        
    }
}
