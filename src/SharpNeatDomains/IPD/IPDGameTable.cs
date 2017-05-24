﻿using System;
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
        private ToolStripMenuItem _history;
        private ToolStripMenuItem _save;
        private DataGridViewColumn _cumulative;
        private DataGridViewColumn _rankings;

        public IPDGameTable(IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ref IPDExperiment.Info info)
        {
            InitializeComponent();

            try
            {
                _genomeDecoder = genomeDecoder;
                _info = info;

                _players = new IPDPlayer[info.OpponentPool.Length + 1];
                _players[0] = new Players.IPDPlayerPhenome(null);
                Array.Copy(info.OpponentPool, 0, _players, 1, info.OpponentPool.Length);
                _games = new IPDGame[info.OpponentPool.Length + 1, info.OpponentPool.Length + 1];
                for (int i = 1; i < _players.Length; i++)
                    for (int j = 1; j < _players.Length; j++)
                        _games[i, j] = info.OpponentPoolGames[i - 1, j - 1];

                CreateTable();
            }
            catch
            {

            }
        }

        private void IPDGameTable_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'g' || e.KeyChar == '\x1B')
                ParentForm.Close();
        }

        public override Size WindowSize => new Size(535, 555);

        public override void RefreshView(object genome)
        {
            IBlackBox phenome;
            if (_info.EvaluationMode == IPDExperiment.EvaluationMode.Novelty)
                phenome = _info.BestNoveltyGenome();
            else
                phenome = _genomeDecoder.Decode(genome as NeatGenome);

            _players[0] = new Players.IPDPlayerPhenome(phenome);
            for (int i = 1; i < _players.Length; i++)
            {
                var g = new IPDGame(_info.NumberOfGames, _players[0], _players[i]);
                g.Run();
                _games[0, i] = g;
                _games[i, 0] = g;
            }
            

            UpdateTable();
        }

        private IPDGame GetGameFromCell(DataGridViewCell cell)
        {
            return _games[cell.ColumnIndex, cell.RowIndex];
        }
        
        private void CreateTable()
        {
            SuspendLayout();

            _table = new DataGridView();
            _table.Location = new Point(10, 10);
            _table.Size = new Size(500, 500);
            _table.AllowUserToAddRows = false;
            _table.MultiSelect = false;
            _table.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _table.EditMode = DataGridViewEditMode.EditProgrammatically;
            _table.RowHeadersWidth = 80;
            _table.ContextMenuStrip = new ContextMenuStrip();
            _table.ContextMenuStrip.Size = new Size(100, 30);
            _table.SelectionChanged += (sender, args) => { _history.Visible = true; try { _history.Text = "Show history for " + GetGameFromCell(_table.SelectedCells[0]).ToString(); } catch { _history.Visible = false; } };
            _table.ContextMenuStrip.Items.Add(_history = new ToolStripMenuItem(string.Empty, null, (sender, args) => { ShowHistory(GetGameFromCell(_table.SelectedCells[0])); }));
            _table.ContextMenuStrip.Items.Add(_save = new ToolStripMenuItem("Save table as CSV", null, (sender, args) => { SaveTable(); }));

            //Create columns and rows
            DataGridViewColumn[] cols = new DataGridViewColumn[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                cols[i] = new DataGridViewTextBoxColumn();
                cols[i].HeaderText = _players[i].Name;
                cols[i].ValueType = typeof(double);
                cols[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                cols[i].Width = 40;
            }
            _table.Columns.AddRange(cols);

            DataGridViewRow[] rows = new DataGridViewRow[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                rows[i] = new DataGridViewRow();
                rows[i].HeaderCell.Value = _players[i].Name;
            }
            _table.Rows.AddRange(rows);

            //Cumulative and Ranking columns
            _cumulative = new DataGridViewTextBoxColumn();
            _cumulative.HeaderText = "Cumulative Score";
            _cumulative.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            _cumulative.SortMode = DataGridViewColumnSortMode.NotSortable;
            _table.Columns.Add(_cumulative);

            _rankings = new DataGridViewTextBoxColumn();
            _rankings.HeaderText = "Ranking";
            _rankings.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            _rankings.SortMode = DataGridViewColumnSortMode.NotSortable;
            _table.Columns.Add(_rankings);

            Controls.Add(_table);
            ResumeLayout(false);
        }

        private void UpdateTable()
        {
            ParentForm.KeyPreview = true;
            ParentForm.KeyPress += IPDGameTable_KeyPress;

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
                double r = (ranks[i] / (double)ranks.Length);
                _table.Rows[i].Cells[_rankings.DisplayIndex].Value = ranks[i] + ((r == 1.0d) ? " (Best)" : string.Empty);
                _table.Rows[i].Cells[_rankings.DisplayIndex].ToolTipText = r.ToString("F3");
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

                File.WriteAllLines(sfd.FileName, output, Encoding.UTF8);
            }
        }

        private void ShowHistory(IPDGame game)
        {
            IPDPlayer[] players = new IPDPlayer[2] { game.A, game.B };

            int formWidth = 320;
            Form history = new Form();
            history.Text = game.ToString();
            history.Size = new Size(formWidth, 500);
            history.FormBorderStyle = FormBorderStyle.Sizable;
            history.MinimumSize = new Size(formWidth, 300);
            history.MaximumSize = new Size(formWidth, 5000);
            history.MaximizeBox = false;
            history.MinimizeBox = false;            

            DataGridView table = new DataGridView();
            table.Location = new Point(0, 0);
            table.Dock = DockStyle.Fill;
            table.AllowUserToAddRows = false;
            table.RowHeadersWidth = 50;
            table.EditMode = DataGridViewEditMode.EditProgrammatically;

            DataGridViewColumn[] cols = new DataGridViewColumn[4];
            string[] colHeaders = new string[4] { players[0].Name, players[1].Name, "Result", "Score" };
            for (int i = 0; i < cols.Length; i++)
            {
                cols[i] = new DataGridViewTextBoxColumn();
                cols[i].HeaderText = colHeaders[i];
                cols[i].SortMode = DataGridViewColumnSortMode.NotSortable;
                cols[i].Width = 60;
            }
            table.Columns.AddRange(cols);
            
            table.Rows.Add(game.Length + 1);
            int a = 0, b = 0;
            for (int i = 0; i < game.Length; i++)
            {
                table.Rows[i].HeaderCell.Value = i.ToString();
                table.Rows[i].Cells[0].Value = game.GetChoice(players[0], i);
                table.Rows[i].Cells[1].Value = game.GetChoice(players[1], i);
                table.Rows[i].Cells[2].Value = game.GetPast(players[0], i) + ", " + game.GetPast(players[1], i);
                a += (int)game.GetPast(players[0], i); b += (int)game.GetPast(players[1], i);
                table.Rows[i].Cells[3].Value = a + ", " + b;
            }

            DataGridViewRow total = new DataGridViewRow();
            table.Rows.Add();
            table.Rows[game.Length].HeaderCell.Value = "T";
            table.Rows[game.Length].Cells[0].Value = game.GetScore(players[0]).ToString("F0");
            table.Rows[game.Length].Cells[1].Value = game.GetScore(players[1]).ToString("F0");
            
            history.Controls.Add(table);
            history.Show();
        }
    }
}