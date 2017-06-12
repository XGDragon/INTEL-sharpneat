using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpNeat.Phenomes;
using System.Threading.Tasks;
using System.Threading;

namespace SharpNeat.Domains.IPD.Players
{
    class IPDPlayerPattern : IPDPlayer
    {
        private string _name;
        public override string Name => _name;

        private IPDGame.Choices[] _pattern;
        private int _iter;

        private Object _choiceLock = new Object();

        private bool _hasRandom;

        public IPDPlayerPattern(string name)
        {
            lock (_choiceLock)
            {
                _name = name;

                _pattern = new IPDGame.Choices[name.Length];
                for (int i = 0; i < name.Length; i++)
                    _pattern[i] = (name[i] == 'C') ? IPDGame.Choices.C : IPDGame.Choices.D;

                _hasRandom = _pattern.Contains(IPDGame.Choices.R);
            }
        }

        public override IPDGame.Choices Choice(IPDGame game)
        {
            if (game.T == 0)
            game.HasRandom = _hasRandom;

            if (_iter >= _pattern.Length)
            _iter = 0;

            return _pattern[_iter++];
        }

        public override void Reset()
        {
            _iter = 0;
        }        
    }
}
