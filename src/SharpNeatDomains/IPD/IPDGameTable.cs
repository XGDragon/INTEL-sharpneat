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
using System.Text;

namespace SharpNeat.Domains.IPD
{
    partial class IPDGameTable : AbstractDomainView
    {
        private IGenomeDecoder<NeatGenome, IBlackBox> _genomeDecoder;
        private IPDExperiment.Info _info;
        
        private IPDPlayer[] _players;
        private IPDGame[,] _games;
        
        private DataGridView _table;
        private DataGridViewColumn _cumulative;
        private DataGridViewColumn _rankings;

        public IPDGameTable(IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, IPDExperiment.Info info)
        {
            InitializeComponent();

            _genomeDecoder = genomeDecoder;
            _info = info;

            try
            {
                _players = new IPDPlayer[info.OpponentPool.Length + 1];
                _players[0] = new Players.IPDPlayerPhenome(null);
                Array.Copy(info.OpponentPool, 0, _players, 1, info.OpponentPool.Length);
                _games = new IPDGame[info.OpponentPool.Length + 1, info.OpponentPool.Length + 1];
                for (int i = 1; i < _players.Length; i++)
                    for (int j = 1; j < _players.Length; j++)
                        _games[i, j] = info.OpponentPoolGames[i - 1, j - 1];

                CreateTable();
            }
            catch (ArgumentNullException)
            {
                return;
            }
        }

        public override Size WindowSize => new Size(535, 555);

        public override void RefreshView(object genome)
        {
            NeatGenome neatGenome = genome as NeatGenome;
            var phenome = _genomeDecoder.Decode(neatGenome);

            _players[0] = new Players.IPDPlayerPhenome(phenome);
            for (int i = 1; i < _players.Length; i++)
            {
                phenome.ResetState();
                var g = new IPDGame(_info.NumberOfGames, _players[0], _players[i]);
                g.Run();
                _games[0, i] = g;
                _games[i, 0] = g;
            }

            UpdateTable();
        }
        
        private void CreateTable()
        {
            SuspendLayout();

            _table = new DataGridView();
            _table.Location = new Point(10, 10);
            _table.Size = new Size(500, 500);
            _table.AllowUserToAddRows = false;
            _table.RowHeadersWidth = 80;
            _table.ContextMenuStrip = new ContextMenuStrip();
            _table.ContextMenuStrip.Size = new Size(100, 30);
            _table.ContextMenuStrip.Items.Add("Save as CSV", null, 
                (sender, args) => 
                {
                    SaveTable();
                });

            //Create columns and rows
            DataGridViewColumn[] cols = new DataGridViewColumn[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                cols[i] = new DataGridViewTextBoxColumn();
                cols[i].HeaderText = _players[i].Name;
                cols[i].ValueType = typeof(double);
                cols[i].Width = 40;
            }
            _table.Columns.AddRange(cols);
            _table.Rows.Add(_players.Length);
            for (int i = 0; i < _players.Length; i++)
                _table.Rows[i].HeaderCell.Value = _players[i].Name;

            Controls.Add(_table);
            ResumeLayout(false);

            //Cumulative and Ranking columns
            _cumulative = new DataGridViewTextBoxColumn();
            _cumulative.HeaderText = "Cumulative Score";
            _cumulative.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            _table.Columns.Add(_cumulative);

            _rankings = new DataGridViewTextBoxColumn();
            _rankings.HeaderText = "Ranking";
            _rankings.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            _table.Columns.Add(_rankings);
        }

        private void UpdateTable()
        {
            //Fill in table
            double[] totals = new double[_players.Length];
            for (int i = 0; i < _players.Length; i++)
                for (int j = i + 1; j < _players.Length; j++)
                    if (i != j)
                    {
                        DataGridViewCell iCell = _table.Rows[i].Cells[j];   //top diagonal
                        DataGridViewCell jCell = _table.Rows[j].Cells[i];   //bottom diagonal
                        double iScore = _games[i, j].GetScore(_players[i]);
                        double jScore = _games[i, j].GetScore(_players[j]);

                        iCell.Value = iScore;
                        iCell.ToolTipText = jScore.ToString();

                        jCell.Value = jScore;
                        jCell.ToolTipText = iScore.ToString();

                        totals[i] += iScore;
                        totals[j] += jScore;
                    }

            //Calculate cumulative column
            for (int i = 0; i < _players.Length; i++)
            {
                _table.Rows[i].Cells[_cumulative.DisplayIndex].Value = totals[i];
                _table.Rows[i].Cells[_cumulative.DisplayIndex].ToolTipText = _players[i].Name + "'s total score";
            }
            
            //Calculate rankings column 
            var ranks = totals.Rank();
            for (int i = 0; i < _players.Length; i++)
            {
                _table.Rows[i].Cells[_rankings.DisplayIndex].Value = ranks[i];
                _table.Rows[i].Cells[_rankings.DisplayIndex].ToolTipText = (ranks[i] / (double)ranks.Length).ToString("F3");
            }
        }        

        private void SaveTable()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "CSV (*.csv)|*.csv";
            sfd.FileName = "Output.csv";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string filename = sfd.FileName.Substring(0, sfd.FileName.Length - 4);
                int c = 1;
                while (File.Exists(sfd.FileName))
                    sfd.FileName = filename + " (" + c++ + ").csv";

                int columnCount = _table.ColumnCount;
                string columnNames = ",";
                string[] output = new string[_table.RowCount + 1];
                for (int i = 0; i < columnCount; i++)
                    columnNames += _table.Columns[i].HeaderText.ToString() + ",";

                output[0] += columnNames;
                for (int i = 1; (i - 1) < _table.RowCount; i++)
                {
                    output[i] = _table.Rows[i - 1].HeaderCell.Value.ToString() + ",";
                    for (int j = 0; j < columnCount; j++)
                        output[i] += (_table.Rows[i - 1].Cells[j].Value != null)
                            ? _table.Rows[i - 1].Cells[j].Value.ToString() + ","
                            : ",";
                }

                System.IO.File.WriteAllLines(sfd.FileName, output, System.Text.Encoding.UTF8);
            }
        }
    }
}