using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD
{
    abstract class IPDPlayer
    {
        public abstract IPDGame.Choices Choice(IPDGame game);
    }
}
