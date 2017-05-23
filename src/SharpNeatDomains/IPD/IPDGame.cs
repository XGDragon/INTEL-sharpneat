using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD
{
    class IPDGame
    {
        public enum Choices { C, D, R };
        public enum Past { None = -1, T = 5, R = 3, P = 1, S = 0 };

        private static Random _r = IPDExperiment.R;

        public static Choices RandomChoice() { return (_r.NextDouble() < 0.5) ? Choices.C : Choices.D; }

        public static int PastToIndex(Past p)
        {
            switch (p)
            {
                default:
                case Past.T:
                    return 0;
                case Past.R:
                    return 1;
                case Past.P:
                    return 2;
                case Past.S:
                    return 3;
            }
        }

        public int T { get; private set; }
        public int Length { get; private set; }

        public IPDPlayer A { get { return _a.Player; } }
        public IPDPlayer B { get { return _b.Player; } }
                
        private PlayerCard _a;
        private PlayerCard _b;

        public IPDGame(int numberOfGames, IPDPlayer a, IPDPlayer b)
        {
            _a = new PlayerCard(a, numberOfGames);
            _b = new PlayerCard(b, numberOfGames);
            Length = numberOfGames;
        }

        public void Run()
        {
            A.Reset();
            B.Reset();

            while (T < Length)
            {
                Choices a = A.Choice(this);
                Choices b = B.Choice(this);

                if (a == Choices.R)
                    a = RandomChoice();
                if (b == Choices.R)
                    b = RandomChoice();

                _a.AddPast(a, b);
                _b.AddPast(b, a);
                T++;
            }
        }

        public override string ToString()
        {
            return A.Name + " v. " + B.Name;
        }

        public Past GetPast(IPDPlayer ab, int time)
        {
            if (time < 0 || (ab != A && ab != B))
                return Past.None;

            int t = (time > T) ? T : time;
            return (ab == A) ? _a[t] : _b[t];
        }

        public Choices GetChoice(IPDPlayer ab, int time)
        {
            if (time < 0 || (ab != A && ab != B))
                return Choices.R;

            switch (GetPast(ab, time))
            {
                case Past.R:
                case Past.S:
                    return Choices.C;
                case Past.P:
                case Past.T:
                    return Choices.D;
                default:
                    return Choices.R;
            }
        }

        public double GetScore(IPDPlayer ab)
        {
            if (ab != _a.Player && ab != _b.Player)
                return 0;
            return (ab == _a.Player) ? _a.Score : _b.Score;
        }

        public double[] GetChoices(IPDPlayer ab)
        {
            if (ab != _a.Player && ab != _b.Player)
                return new double[2];
            return (ab == _a.Player) ? _a.ChoiceCounter : _b.ChoiceCounter;
        }

        public double[] GetPasts(IPDPlayer ab)
        {
            if (ab != _a.Player && ab != _b.Player)
                return new double[4];
            return (ab == _a.Player) ? _a.PastCounter : _b.PastCounter;
        }

        private struct PlayerCard
        {
            public IPDPlayer Player { get; private set; }
            public double Score { get; private set; }

            public double[] ChoiceCounter { get; private set; }
            public double[] PastCounter { get; private set; }

            public Past this[int time] { get { return _history[time]; } }

            private int _t;
            private Past[] _history;

            public PlayerCard(IPDPlayer player, int numberofGames)
            {
                Player = player;
                _history = new Past[numberofGames];
                Score = 0;
                _t = 0;
                ChoiceCounter = new double[2];
                PastCounter = new double[4];
            }

            public void AddPast(Choices myChoice, Choices opponentsChoice)
            {
                if (myChoice == Choices.R || opponentsChoice == Choices.R)
                    throw new Exception("Cannot add random past");

                if (myChoice == Choices.C)
                {
                    if (opponentsChoice == Choices.C)
                        _history[_t] = Past.R;
                    else _history[_t] = Past.S;//D
                }
                else//D
                {
                    if (opponentsChoice == Choices.C)
                        _history[_t] = Past.T;
                    else _history[_t] = Past.P;//D
                }

                ChoiceCounter[(int)myChoice]++;
                PastCounter[PastToIndex(_history[_t])]++;

                Score += (int)_history[_t];
                _t++;
            }
        }
    }
}
