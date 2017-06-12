using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using System.IO;
using System.Text;
using ZedGraph;
using System.Diagnostics;

namespace SharpNeat.Domains.IPD
{
    partial class IPDDomain : AbstractDomainView
    {
        public override Size WindowSize => new Size(_tableSize.Width + _graphSize.Width + 50, 120 + Math.Max(_tableSize.Height, _graphSize.Height));

        private IGenomeDecoder<NeatGenome, IBlackBox> _genomeDecoder;
        private IPDExperiment.Info _info;        
        private IPDPlayer[] _players;
        private IPDGame[,] _games;
        
        private Size _labelSize = new Size(990, 30);
        private Point _labelLocation1 = new Point(10, 10);
        private Point _labelLocation2 = new Point(10, 40);

        private DataGridView _table;
        private Size _tableSize = new Size(500, 700);
        private Point _tableLocation = new Point(10, 70);
        private ToolStripMenuItem _history;
        private ToolStripMenuItem _save;
        private DataGridViewColumn _cumulative;
        private DataGridViewColumn _rankings;

        private ZedGraphControl _graphArchive;
        private Size _graphSize = new Size(700, 700);
        private Point _graphLocation = new Point(520, 70);

        public IPDDomain(IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder, ref IPDExperiment.Info info)
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


                SuspendLayout();
                CreateTable();
                CreateArchiveGraph();
                CreateInfoLabel();
                CreateSSButtons();
                ResumeLayout();
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
            UpdateArchiveGraph();
        }

        private void CreateSSButtons()
        {
            string path = Directory.GetCurrentDirectory() + "\\Screenshots";

            var screenshot = new System.Windows.Forms.Button();
            screenshot.Size = new Size(100, 50);
            screenshot.Text = "Screenshot";
            screenshot.Location = new Point(_labelLocation1.X + _labelSize.Width + 10, 10);
            screenshot.Click += (sender, args) =>
            {
                Bitmap captureBitmap = new Bitmap(Screen.FromControl(this).Bounds.Width, Screen.FromControl(this).Bounds.Height);
                Rectangle captureRectangle = Screen.FromControl(this).Bounds;
                Graphics captureGraphics = Graphics.FromImage(captureBitmap);
                captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);

                string s = "-";
                string image;
                if (_info.EvaluationMode == IPDExperiment.EvaluationMode.Novelty)
                    image = _info.EvaluationMode.ToString() + s
                        + _info.PopulationSize + s
                        + _info.NumberOfGames + s
                        + _info.NoveltyMetric + s
                        + "K" + _info.NoveltyK + s
                        + "Reach" + (_info.InputCount / 2) + s;
                else
                    image = _info.EvaluationMode.ToString() + s
                        + _info.PopulationSize + s
                        + _info.NumberOfGames + s
                        + "Reach" + (_info.InputCount / 2) + s;
                string opponents = "";
                int r = 0;
                for (int i = 0; i < _info.OpponentPool.Length; i++)
                {
                    if (_info.OpponentPool[i].Name.Length > 9 && _info.OpponentPool[i].Name.Substring(0, 9) == "Generated")
                        r++;
                    else
                        opponents += _info.OpponentPool[i].Name + ((i < _info.OpponentPool.Length - 1) ? "," : "");
                    if (image.Length + opponents.Length > 230)
                        break;
                }
                if (r > 0)
                    opponents += ",Random" + r;
                                
                Directory.CreateDirectory(path);
                captureBitmap.Save(path + "\\" + image + opponents + ".png", System.Drawing.Imaging.ImageFormat.Png);
            };

            var open = new System.Windows.Forms.Button();
            open.Size = new Size(100, 50);
            open.Text = "Open Folder";
            open.Location = new Point(screenshot.Location.X + 110, 10);
            open.Click += (sender, args) =>
            {
                if (Directory.Exists(path))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo("explorer.exe", path);
                    Process.Start(startInfo);
                };
            };

            Controls.Add(open);
            Controls.Add(screenshot);
        }

        private void CreateInfoLabel()
        {
            var label1 = new System.Windows.Forms.Label();
            label1.Size = _labelSize;
            label1.Location = _labelLocation1;
            label1.Font = new Font(label1.Font.FontFamily, 12);

            label1.Text =
                  "Generation "
                + _info.CurrentGeneration
                + "; "
                + _info.Evaluations()
                + " evaluations; "
                + _info.PopulationSize
                + " genomes; "
                + _info.NumberOfGames
                + " iterated games per opponent";

            var label2 = new System.Windows.Forms.Label();
            label2.Size = _labelSize;
            label2.Location = _labelLocation2;
            label2.Font = new Font(label2.Font.FontFamily, 10);

            if (_info.EvaluationMode == IPDExperiment.EvaluationMode.Novelty)
            {
                label2.Text =
                      "("
                    + _info.EvaluationMode
                    + ") Evaluation Mode, ("
                    + _info.NoveltyMetric
                    + ") Novelty Metric with KNN-"
                    + _info.NoveltyK
                    + ", memory-"
                    + _info.InputCount / 2;
            }
            else
            {
                label2.Text =
                      "("
                    + _info.EvaluationMode
                    + ") Evaluation Mode, memory-"
                    + _info.InputCount / 2;
            }

            Controls.Add(label1);
            Controls.Add(label2);
        }

        private void CreateArchiveGraph()
        {
            _graphArchive = new ZedGraphControl();
            _graphArchive.Location = _graphLocation;
            _graphArchive.Size = _graphSize;
            _graphArchive.IsShowPointValues = true;

            GraphPane g = _graphArchive.GraphPane;
            g.Title.Text = "Novelty Archive Visualization";
            g.XAxis.Title.Text = "Archive Index";
            g.YAxis.Title.Text = "Score";

            g.Y2Axis.IsVisible = true;
            g.Y2Axis.Scale.Min = 0.0;
            g.Y2Axis.Scale.Max = 1.0;

            Controls.Add(_graphArchive);
        }

        private void UpdateArchiveGraph()
        {
            GraphPane g = _graphArchive.GraphPane;
            g.CurveList.Clear();

            var archive = _info.Archive();
            archive.Sort((a, b) => a.ID.CompareTo(b.ID));

            double[] ii = new double[archive.Count];
            double[] score = new double[archive.Count];
            double[] rank = new double[archive.Count];
            double[] averageRank = new double[archive.Count];
            PointPairList averageWinningScore = new PointPairList();
            PointPairList winningScores = new PointPairList();
            PointPairList topScore = new PointPairList();

            double averageRankCounter = 0;
            double averageWinningScoreCounter = 0;
            //double topScoreCounter = -1;
            int first = -1;
            for (int i = 0; i < ii.Length; i++)
            {
                ii[i] = i;
                score[i] = archive[i].Score;
                rank[i] = archive[i].Rank;

                averageRankCounter += rank[i];
                averageRank[i] = averageRankCounter / (i + 1);
                //topScoreCounter = (score[i] > topScoreCounter) ? score[i] : topScoreCounter;
                //topScore.Add(new PointPair(i, topScoreCounter));

                if (rank[i] == 1.0)
                {
                    if (first == -1)
                        first = i;

                    averageWinningScoreCounter += score[i]; //average score, only counting those with rank 1
                    averageWinningScore.Add(new PointPair(i, averageWinningScoreCounter / (averageWinningScore.Count + 1)));
                    
                }
            }

            Color[] greyscale = new Color[4]
            {
                Color.FromArgb(180, 180, 180), //lightest
                Color.FromArgb(120, 120, 120),
                Color.FromArgb(60, 60, 60),
                Color.FromArgb(0, 0, 0) //darkest
            };

            //var f = g.AddCurve("First hit", new PointPairList() { new PointPair(first, 1.0d) }, Color.White, SymbolType.Triangle);
            //f.IsY2Axis = true;
            //f.Symbol.Size = 20;
            //f.Symbol.Fill = new Fill(Brushes.Green);
            
            var ar = g.AddCurve("Average Rank", ii, averageRank, greyscale[3], SymbolType.None);
            ar.IsY2Axis = true;
            ar.Line.Width = 3;

            var @as = g.AddCurve("Average Winning Score", averageWinningScore, greyscale[2], SymbolType.Star);
            @as.Line.Width = 0;
            @as.Line.IsVisible = false;
            @as.Symbol.Size = 2;
            @as.Symbol.Fill = new Fill(greyscale[2]);

            //var ws = g.AddCurve("Winning Behavior", winningScores, greyscale[2], SymbolType.Star);
            //ws.Line.Width = 0;
            //ws.Line.IsVisible = false;
            //ws.Symbol.Size = 3;
            //ws.Symbol.Fill = new Fill(greyscale[2]);

            //var t = g.AddCurve("Top Score", topScore, greyscale[0], SymbolType.None);
            //t.Line.Width = 2;

            var s = g.AddCurve("Behavior", ii, score, greyscale[1], SymbolType.HDash);
            s.Line.IsVisible = false;
            s.Symbol.Size = 3;

            //var r = g.AddCurve("Ranking", ii, rank, Color.Coral, SymbolType.Diamond);
            //r.IsY2Axis = true;
            //r.Symbol.Fill = new Fill(Brushes.Coral);
            //r.Symbol.Size = 3;
            //r.Line.IsVisible = false;

            _graphArchive.AxisChange();
        }
        
        private void CreateTable()
        {
            IPDGame GetGameFromCell(DataGridViewCell cell) { return _games[cell.ColumnIndex, cell.RowIndex]; }
            
            _table = new DataGridView();
            _table.Location = _tableLocation;
            _table.Size = _tableSize;
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
                a += IPDGame.PastToScore(game.GetPast(players[0], i)); b += IPDGame.PastToScore(game.GetPast(players[1], i));
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