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

        public IPDPlayer PlayerA { get { return _a.Player; } }
        public IPDPlayer PlayerB { get { return _b.Player; } }
        public int T { get; private set; }

        private static Random _r = new Random();

        private PlayerCard _a;
        private PlayerCard _b;

        private int _maxGames;

        public IPDGame(int numberOfGames, IPDPlayer a, IPDPlayer b)
        {
            _a = new PlayerCard(a, numberOfGames);
            _b = new PlayerCard(b, numberOfGames);
            _maxGames = numberOfGames;
        }

        public void Run(bool ignoreScore = false)
        {
            while (T < _maxGames)
            {
                Choices a = _a.Player.Choice(this);
                Choices b = _b.Player.Choice(this);
                a = (a == Choices.R) ? RandomChoice() : a;
                b = (b == Choices.R) ? RandomChoice() : b;

                _a.AddPast(a, b);
                _b.AddPast(b, a);
                T++;
            }

            _a.Player.AddScore(_b.Player, _a.Score);
            if (!ignoreScore)
                _b.Player.AddScore(_a.Player, _b.Score);
        }

        private Choices RandomChoice()
        {
            return (_r.NextDouble() < 0.5) ? Choices.C : Choices.D;
        }

        public Past GetPast(IPDPlayer ab, int time)
        {
            if (time < 0)
                return Past.None;

            int t = (time > T) ? T : time;
            return (ab == _a.Player) ? _a[t] : _b[t];
        }

        private struct PlayerCard
        {
            public IPDPlayer Player { get; private set; }
            public double Score { get; private set; }

            private int _t;
            private Past[] _history;
            public Past this[int time] { get { return _history[time]; } }

            public PlayerCard(IPDPlayer player, int numberofGames)
            {
                Player = player;
                _history = new Past[numberofGames];
                Score = 0;
                _t = 0;
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

                Score += (int)_history[_t];
                _t++;
            }
        }
    }
}
