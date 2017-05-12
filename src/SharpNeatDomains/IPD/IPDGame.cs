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
        public enum Past { T, R, P, S };

        public int T { get; private set; }
        public Past this[int time] { get { return _history[(time > T) ? T : time]; } }

        private Past[] _history;

        public IPDGame(int numberOfGames)
        {
            _history = new Past[numberOfGames];
        }
    }
}
