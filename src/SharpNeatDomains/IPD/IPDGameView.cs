using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SharpNeat.Core;
using SharpNeat.Domains.BoxesVisualDiscrimination;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System.Threading;

namespace SharpNeat.Domains.IPD
{
    partial class IPDGameView : AbstractDomainView
    {
        IGenomeDecoder<NeatGenome, IBlackBox> _genomeDecoder;

        int _numberOfGames;
        IPDPlayer[] _players;
        IPDGame[] _games;

        TabControl _window = new TabControl();

        public IPDGameView(IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, int numberOfGames, params IPDPlayer[] pool)
        {
            InitializeComponent();

            _numberOfGames = numberOfGames;
            _players = pool;
            _genomeDecoder = genomeDecoder;

        }

        public override void RefreshView(object genome)
        {
            NeatGenome neatGenome = genome as NeatGenome;
            var phenome = _genomeDecoder.Decode(neatGenome);

            Players.IPDPlayerPhenome p = new Players.IPDPlayerPhenome(phenome);
            _games = new IPDGame[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                phenome.ResetState();
                _games[i] = new IPDGame(_numberOfGames, p, _players[i]);
                _games[i].Run(true);
            }

            CreateLayout();
        }

        private void CreateLayout()
        {
            Controls.Clear();
            SuspendLayout();
            _window = new TabControl();
            _window.SuspendLayout();
            _window.Dock = System.Windows.Forms.DockStyle.Fill;

            for (int i = 0; i < _players.Length; i++)
                _window.Controls.Add(CreatePlayerPage(_games[i]));

            Controls.Add(_window);
            _window.ResumeLayout(false);
            ResumeLayout(false);
        }

        private TabPage CreatePlayerPage(IPDGame game)
        {
            TabPage page = new TabPage(game.PlayerB.Name);

            return page;
        }
    }
}
