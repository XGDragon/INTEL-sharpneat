using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Phenomes;
using System.Threading.Tasks;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerZD : IPDPlayer
    {
        public override string Name => "ZD";

        public IPDPlayerZD()
        {

        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            return IPDGame.Choices.C;
        }

        public override void Reset()
        {

        }
    }
}
