using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SharpNeat.Core;
using SharpNeat.Domains.BoxesVisualDiscrimination;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using CsvHelper;
using System.IO;

namespace SharpNeat.Domains.IPD
{
    partial class IPDGameTable : AbstractDomainView
    {
        IGenomeDecoder<NeatGenome, IBlackBox> _genomeDecoder;

        int _numberOfGames;
        IPDPlayer[] _players;
        IPDGame[,] _games;
        bool _isValid = false;
        Button _save;
        Object _genomeLock = new Object();

        public IPDGameTable(IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, int numberOfGames, params IPDPlayer[] pool)
        {
            InitializeComponent();
            CreateSaveButton();
            _save.MouseClick += _save_MouseClick;

            _numberOfGames = numberOfGames;
            _genomeDecoder = genomeDecoder;

            _games = new IPDGame[pool.Length + 1, pool.Length + 1];
            _players = new IPDPlayer[pool.Length + 1];

            for (int i = 0; i < pool.Length; i++)
            {
                _players[i + 1] = pool[i];
                for (int j = i + 1; j < pool.Length; j++)
                {
                    if (i != j) //currently not against each other but..
                    {
                        IPDGame g = new IPDGame(_numberOfGames, pool[i], pool[j]);
                        g.Run();
                        _games[i + 1, j + 1] = g;
                        _games[j + 1, i + 1] = g;
                    }
                }
            }
        }

        private void _save_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _isValid)
            {
                SaveFileDialog save = new SaveFileDialog();
                save.FileName = "GameTables.csv";
                save.Filter = "CSV File | *.csv";
                if (save.ShowDialog() == DialogResult.OK)
                {
                    StreamWriter writer = new StreamWriter(save.OpenFile());
                    CsvWriter csv = new CsvWriter(writer);

                    //Header
                    csv.WriteField(string.Empty);
                    for (int i = 0; i < _players.Length; i++)
                        csv.WriteField(_players[i].Name);
                    csv.WriteField("Cumulative Score");
                    csv.NextRecord();

                    //Rows
                    for (int i = 0; i < _players.Length; i++)
                    {
                        double total = 0;
                        csv.WriteField(_players[i].Name);
                        for (int j = 0; j < _players.Length; j++)
                        {
                            if (i == j)
                                csv.WriteField(string.Empty);
                            else
                            {
                                double s = _games[i, j].GetScore(_players[i]);
                                total += s;
                                csv.WriteField(s.ToString());
                            }
                        }
                        csv.WriteField(total.ToString());
                        csv.NextRecord();
                    }

                    writer.Dispose();
                    writer.Close();
                }               
            }
        }

        public override void RefreshView(object genome)
        {
            NeatGenome neatGenome = genome as NeatGenome;
            var phenome = _genomeDecoder.Decode(neatGenome);

            _players[0] = new Players.IPDPlayerPhenome(phenome);
            for (int i = 1; i < _players.Length; i++)
            {
                phenome.ResetState();
                var g = new IPDGame(_numberOfGames, _players[0], _players[i]);
                g.Run();
                _games[0, i] = g;
                _games[i, 0] = g;
            }

            _isValid = true;
        }

        private void CreateSaveButton()
        {
            SuspendLayout();
            //Size = new Size(70, 70);

            _save = new Button();
            _save.Location = new Point(10, 10);
            _save.Size = new Size(100, 30);
            _save.Text = "Save as CSV";

            Controls.Add(_save);
            ResumeLayout(false);
        }
    }
}