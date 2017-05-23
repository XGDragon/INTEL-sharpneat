using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD
{
    abstract class IPDPlayer
    {
        public abstract string Name { get; }

        public override string ToString()
        {
            return Name;
        }

        public abstract IPDGame.Choices Choice(IPDGame game);

        public abstract void Reset();

        public delegate IPDGame.Choices[] QFunction(int alpha, int beta);
    }
}
