using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork.Genes;

namespace AI_Evlo_Test
{
    /// <summary>
    /// Immutable snapshot of one population, built by MainWindow under simLock and handed to the
    /// dashboard's refresh timer. Cloning here means the WinForms timer never touches live model
    /// state on the simulation thread.
    /// </summary>
    public sealed class PopulationDashboardSnapshot
    {
        public string Name;
        public string Species;
        public int SizeLimit;
        public int AliveCount;
        public int TotalEver;
        public int LifeCycles;
        public double TopFitness;
        public double MeanFitness;
        public double MeanAge;
        public int ArchivedBestCount;

        public bool GoldenEnabled;
        public int GoldenAveragedCount;
        public double GoldenThreshold;
        public int GoldenRecordSurvivorCycles;
        public bool GoldenAlive;
        public int GoldenAge;

        public PopulationSample[] Series = Array.Empty<PopulationSample>();
        public GoldenLifetimeSample[] GoldenLifetimes = Array.Empty<GoldenLifetimeSample>();
        public GoldenAverageEvent[] GoldenEvents = Array.Empty<GoldenAverageEvent>();

        public NeuralNetworkGene GoldenInitialGene;
        public NeuralNetworkGene GoldenCurrentGene;
    }

    /// <summary>One selectable population for the dashboard's population dropdown.</summary>
    public sealed class DashboardPopulationOption
    {
        public string Id;
        public string Name;
        public string Species;
        public override string ToString() => $"{Name} ({Species})";
    }

    /// <summary>
    /// Real-time dashboard for a single population. Refreshes from a snapshot provider on a timer;
    /// the provider (MainWindow) reads model state under simLock so this form is fully decoupled
    /// from the simulation thread. The active population can be changed live via the header dropdown.
    /// </summary>
    public sealed class PopulationDashboard : Form
    {
        private readonly Func<string, PopulationDashboardSnapshot> snapshotProvider;
        private readonly Func<List<DashboardPopulationOption>> populationsProvider;
        private readonly Action<string, string> onPopulationSwitched;
        private readonly Timer timer = new Timer { Interval = 300 };

        // Which population this dashboard is currently showing.
        private string currentPopulationId;
        // Population selector.
        private ComboBox cmbPopulation;
        private bool suppressSelectionEvent;

        // Header tiles.
        private Label tileAlive, tileTotal, tileTop, tileMean, tileAge, tileCycles, tileArchived;
        // Golden tiles.
        private Label gldEnabled, gldCount, gldThreshold, gldRecord, gldAge;

        private DashboardChart chartPop, chartFitness, chartEvolution, chartDeaths;
        private DashboardChart chartLongevity, chartCadence, chartPerLayer;
        private ListBox lstEvents;
        private int lastEventCount = -1;

        private GeneDeltaNetworkView viewInitial, viewCurrent;
        private DataGridView gridChanged;

        /// <summary>The population this dashboard currently displays (changes via the dropdown).</summary>
        public string CurrentPopulationId => currentPopulationId;

        public PopulationDashboard(
            string title,
            Func<List<DashboardPopulationOption>> populations,
            Func<string, PopulationDashboardSnapshot> provider,
            Action<string, string> onSwitched,
            string initialPopulationId)
        {
            populationsProvider = populations;
            snapshotProvider = provider;
            onPopulationSwitched = onSwitched;
            currentPopulationId = initialPopulationId;
            Text = title;
            Width = 1040;
            Height = 720;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 246, 248);
            Font = new Font("Segoe UI", 8.25f);

            if (WindowBoundsStore.TryGet("PopulationDashboard", out double w, out double h))
                Size = new Size((int)w, (int)h);
            FormClosing += (s, e) => WindowBoundsStore.Save("PopulationDashboard", Width, Height);

            BuildUi();

            timer.Tick += (s, e) => Refresh_();
            Load += (s, e) => { timer.Start(); Refresh_(); };
            FormClosed += (s, e) => timer.Stop();
        }

        private void BuildUi()
        {
            FlowLayoutPanel tiles = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6, 6, 6, 0),
                BackColor = Color.White,
                WrapContents = false,
                AutoScroll = true
            };
            tileAlive = AddTile(tiles, "Alive / limit");
            tileTotal = AddTile(tiles, "Total ever");
            tileTop = AddTile(tiles, "Top fitness");
            tileMean = AddTile(tiles, "Mean fitness");
            tileAge = AddTile(tiles, "Mean age");
            tileCycles = AddTile(tiles, "Life cycles");
            tileArchived = AddTile(tiles, "Archived best");

            // Population selector (top-right): switches the whole dashboard to another live population.
            FlowLayoutPanel selector = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(6, 12, 6, 0),
                BackColor = Color.White
            };
            Label selLabel = new Label
            {
                Text = "Show population",
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0),
                ForeColor = Color.FromArgb(120, 126, 136)
            };
            cmbPopulation = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 190,
                Font = new Font("Segoe UI", 9f)
            };
            cmbPopulation.SelectedIndexChanged += OnPopulationSelected;
            selector.Controls.Add(selLabel);
            selector.Controls.Add(cmbPopulation);

            TableLayoutPanel header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 56,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            header.Controls.Add(tiles, 0, 0);
            header.Controls.Add(selector, 1, 0);

            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildPopulationTab());
            tabs.TabPages.Add(BuildGoldenTab());
            tabs.TabPages.Add(BuildBrainDiffTab());

            Controls.Add(tabs);
            Controls.Add(header);
        }

        private TabPage BuildPopulationTab()
        {
            TabPage page = new TabPage("Population") { BackColor = Color.FromArgb(245, 246, 248) };
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(6)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            chartPop = NewChart("Population size", Color.FromArgb(40, 120, 215));
            chartPop.PrimaryLabel = "alive";
            chartPop.SecondaryLabel = "% of limit";
            chartPop.SecondaryColor = Color.FromArgb(230, 150, 40);
            chartFitness = NewChart("Longevity — top vs mean", Color.FromArgb(220, 90, 40));
            chartFitness.PrimaryLabel = "top";
            chartFitness.SecondaryLabel = "mean";
            chartFitness.SecondaryColor = Color.FromArgb(90, 170, 90);
            chartEvolution = NewChart("Evolution rate — Δ mean longevity / 1000 cycles", Color.FromArgb(150, 80, 200));
            chartEvolution.PrimaryLabel = "Δ / 1k";
            chartDeaths = NewChart("Agents lost per 1000 cycles (death rate)", Color.FromArgb(200, 70, 60));
            chartDeaths.PrimaryLabel = "lost / 1k";
            chartDeaths.SecondaryLabel = "% of size";
            chartDeaths.SecondaryColor = Color.FromArgb(40, 120, 215);

            grid.Controls.Add(chartPop, 0, 0);
            grid.Controls.Add(chartFitness, 1, 0);
            grid.Controls.Add(chartEvolution, 0, 1);
            grid.Controls.Add(chartDeaths, 1, 1);
            page.Controls.Add(grid);
            return page;
        }

        private TabPage BuildGoldenTab()
        {
            TabPage page = new TabPage("Golden Agent") { BackColor = Color.FromArgb(245, 246, 248) };

            FlowLayoutPanel tiles = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(6, 6, 6, 0) };
            gldEnabled = AddTile(tiles, "Feature");
            gldCount = AddTile(tiles, "Brains merged");
            gldThreshold = AddTile(tiles, "Threshold (cycles)");
            gldRecord = AddTile(tiles, "Record survivor");
            gldAge = AddTile(tiles, "Live golden age");

            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(6) };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            chartLongevity = NewChart("Golden longevity per life (cycles)", Color.FromArgb(210, 160, 30));
            chartCadence = NewChart("Cycles between brain merges (lower = more frequent)", Color.FromArgb(180, 120, 40));

            lstEvents = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 8f),
                IntegralHeight = false
            };
            Panel eventsPanel = WrapTitled("Merge events (newest first)", lstEvents);

            grid.Controls.Add(chartLongevity, 0, 0);
            grid.Controls.Add(chartCadence, 1, 0);
            grid.Controls.Add(eventsPanel, 0, 1);
            grid.SetColumnSpan(eventsPanel, 2);

            page.Controls.Add(grid);
            page.Controls.Add(tiles);
            return page;
        }

        private TabPage BuildBrainDiffTab()
        {
            TabPage page = new TabPage("Brain Diff") { BackColor = Color.FromArgb(245, 246, 248) };

            Label legend = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.White,
                Text = "Colour = change since initial:  black = unchanged   →   red = largest change.   " +
                       "Line/node thickness = weight/bias magnitude in that state.  Hover a node for values."
            };

            SplitContainer split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };

            SplitContainer nets = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };
            viewInitial = new GeneDeltaNetworkView { Dock = DockStyle.Fill, ShowCurrent = false, Caption = "Initial golden brain" };
            viewCurrent = new GeneDeltaNetworkView { Dock = DockStyle.Fill, ShowCurrent = true, Caption = "Current golden brain" };
            nets.Panel1.Controls.Add(viewInitial);
            nets.Panel2.Controls.Add(viewCurrent);

            TableLayoutPanel rightCol = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

            gridChanged = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            gridChanged.Columns.Add("Element", "Element");
            gridChanged.Columns.Add("Initial", "Initial");
            gridChanged.Columns.Add("Current", "Current");
            gridChanged.Columns.Add("Delta", "Δ");

            chartPerLayer = NewChart("Mean |change| per layer", Color.FromArgb(200, 70, 60));

            rightCol.Controls.Add(WrapTitled("Most-changed weights & biases", gridChanged), 0, 0);
            rightCol.Controls.Add(chartPerLayer, 0, 1);

            split.Panel1.Controls.Add(nets);
            split.Panel2.Controls.Add(rightCol);
            split.SplitterDistance = 600;

            page.Controls.Add(split);
            page.Controls.Add(legend);
            return page;
        }

        // ---- refresh -------------------------------------------------------------------------

        private void Refresh_()
        {
            RefreshPopulationList();
            RenderCurrent();
        }

        /// <summary>Rebuilds the population dropdown from the live population set (only when it changes).</summary>
        private void RefreshPopulationList()
        {
            List<DashboardPopulationOption> opts;
            try { opts = populationsProvider?.Invoke(); }
            catch { return; }
            if (opts == null)
                return;

            // Skip the rebuild when the set is unchanged, so the open dropdown is not disrupted.
            bool changed = opts.Count != cmbPopulation.Items.Count;
            if (!changed)
            {
                for (int i = 0; i < opts.Count; i++)
                    if (!(cmbPopulation.Items[i] is DashboardPopulationOption cur)
                        || cur.Id != opts[i].Id || cur.Name != opts[i].Name)
                    { changed = true; break; }
            }
            if (!changed)
                return;

            suppressSelectionEvent = true;
            cmbPopulation.BeginUpdate();
            cmbPopulation.Items.Clear();
            int selectIndex = -1;
            for (int i = 0; i < opts.Count; i++)
            {
                cmbPopulation.Items.Add(opts[i]);
                if (opts[i].Id == currentPopulationId)
                    selectIndex = i;
            }
            // If the watched population vanished, fall back to the first available one.
            if (selectIndex < 0 && cmbPopulation.Items.Count > 0)
            {
                selectIndex = 0;
                SwitchTo((DashboardPopulationOption)cmbPopulation.Items[0]);
            }
            if (cmbPopulation.Items.Count > 0)
                cmbPopulation.SelectedIndex = selectIndex;
            cmbPopulation.EndUpdate();
            suppressSelectionEvent = false;
        }

        private void OnPopulationSelected(object sender, EventArgs e)
        {
            if (suppressSelectionEvent)
                return;
            if (cmbPopulation.SelectedItem is DashboardPopulationOption opt)
                SwitchTo(opt);
        }

        /// <summary>Points the dashboard at a different population and notifies the host to move the stats collector.</summary>
        private void SwitchTo(DashboardPopulationOption opt)
        {
            if (opt == null || opt.Id == currentPopulationId)
                return;
            string previous = currentPopulationId;
            currentPopulationId = opt.Id;
            try { onPopulationSwitched?.Invoke(previous, currentPopulationId); }
            catch { }
            RenderCurrent();
        }

        private void RenderCurrent()
        {
            PopulationDashboardSnapshot s;
            try { s = snapshotProvider?.Invoke(currentPopulationId); }
            catch { return; }
            if (s == null)
                return;

            Text = $"Dashboard — {s.Name} ({s.Species})";

            SetTile(tileAlive, $"{s.AliveCount} / {s.SizeLimit}");
            SetTile(tileTotal, s.TotalEver.ToString());
            SetTile(tileTop, s.TopFitness.ToString("0"));
            SetTile(tileMean, s.MeanFitness.ToString("0"));
            SetTile(tileAge, s.MeanAge.ToString("0"));
            SetTile(tileCycles, s.LifeCycles.ToString());
            SetTile(tileArchived, s.ArchivedBestCount.ToString());

            SetTile(gldEnabled, s.GoldenEnabled ? "enabled" : "off");
            SetTile(gldCount, s.GoldenAveragedCount.ToString());
            SetTile(gldThreshold, Math.Ceiling(s.GoldenThreshold).ToString("0"));
            SetTile(gldRecord, s.GoldenRecordSurvivorCycles.ToString());
            SetTile(gldAge, s.GoldenAlive ? s.GoldenAge.ToString() : "—");

            // Population charts.
            double[] aliveSeries = s.Series.Select(x => (double)x.Alive).ToArray();
            chartPop.SetLinePercentRight(aliveSeries, PercentOfSize(aliveSeries, s.SizeLimit));
            chartFitness.SetLine(
                s.Series.Select(x => x.TopFitness).ToArray(),
                s.Series.Select(x => x.MeanFitness).ToArray());
            chartEvolution.SetLine(EvolutionRatePer1000(s.Series));

            double[] deathRate = DeathsPer1000Cycles(s.Series);
            chartDeaths.SetLinePercentRight(deathRate, PercentOfSize(deathRate, s.SizeLimit));

            // Golden charts.
            chartLongevity.SetBars(s.GoldenLifetimes.Select(x => (double)x.Lifetime).ToArray());
            chartCadence.SetBars(MergeIntervals(s.GoldenEvents));
            UpdateEvents(s.GoldenEvents);

            // Brain diff.
            viewInitial.SetGenes(s.GoldenInitialGene, s.GoldenCurrentGene);
            viewCurrent.SetGenes(s.GoldenInitialGene, s.GoldenCurrentGene);
            UpdateChangedTable(s.GoldenInitialGene, s.GoldenCurrentGene);
            chartPerLayer.SetBars(PerLayerMeanDelta(s.GoldenInitialGene, s.GoldenCurrentGene, out string[] labels), labels);
        }

        private void UpdateEvents(GoldenAverageEvent[] events)
        {
            if (events.Length == lastEventCount)
                return;
            lastEventCount = events.Length;

            lstEvents.BeginUpdate();
            lstEvents.Items.Clear();
            for (int i = events.Length - 1; i >= 0; i--)
            {
                GoldenAverageEvent ev = events[i];
                lstEvents.Items.Add($"cycle {ev.Cycle,-8}  merge #{ev.AverageCount,-4}  {ev.SurvivorId} (age {ev.SurvivorCycles})");
            }
            lstEvents.EndUpdate();
        }

        private void UpdateChangedTable(NeuralNetworkGene initial, NeuralNetworkGene current)
        {
            List<ChangedElement> changes = CollectChanges(initial, current);
            changes.Sort((a, b) => Math.Abs(b.Delta).CompareTo(Math.Abs(a.Delta)));

            gridChanged.SuspendLayout();
            gridChanged.Rows.Clear();
            int n = Math.Min(25, changes.Count);
            for (int i = 0; i < n; i++)
            {
                ChangedElement c = changes[i];
                int rowIndex = gridChanged.Rows.Add(c.Label, c.Initial.ToString("0.###"), c.Current.ToString("0.###"), c.Delta.ToString("+0.###;-0.###"));
                double t = changes[0].Delta != 0 ? Math.Min(1.0, Math.Abs(c.Delta) / Math.Abs(changes[0].Delta)) : 0;
                gridChanged.Rows[rowIndex].Cells[3].Style.ForeColor = Color.FromArgb((int)(80 + 175 * t), 30, 30);
            }
            gridChanged.ResumeLayout();
        }

        // ---- helpers -------------------------------------------------------------------------

        private DashboardChart NewChart(string caption, Color color)
        {
            return new DashboardChart { Dock = DockStyle.Fill, Margin = new Padding(4), Caption = caption, PrimaryColor = color };
        }

        private static Panel WrapTitled(string title, Control inner)
        {
            Panel panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(4), Padding = new Padding(0, 16, 0, 0), BackColor = Color.White };
            inner.Dock = DockStyle.Fill;
            Label label = new Label { Dock = DockStyle.Top, Height = 16, Text = title, Font = new Font("Segoe UI", 8.25f, FontStyle.Bold), ForeColor = Color.FromArgb(70, 76, 86) };
            panel.Controls.Add(inner);
            panel.Controls.Add(label);
            return panel;
        }

        private Label AddTile(FlowLayoutPanel parent, string caption)
        {
            Panel tile = new Panel { Width = 132, Height = 44, Margin = new Padding(3), BackColor = Color.FromArgb(248, 249, 251), BorderStyle = BorderStyle.FixedSingle };
            Label cap = new Label { Dock = DockStyle.Top, Height = 16, Text = caption, ForeColor = Color.FromArgb(120, 126, 136), Font = new Font("Segoe UI", 7.5f) };
            Label val = new Label { Dock = DockStyle.Fill, Text = "—", ForeColor = Color.FromArgb(30, 34, 42), Font = new Font("Segoe UI", 12f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            tile.Controls.Add(val);
            tile.Controls.Add(cap);
            parent.Controls.Add(tile);
            return val;
        }

        private static void SetTile(Label tile, string value)
        {
            if (tile != null && tile.Text != value)
                tile.Text = value;
        }

        /// <summary>
        /// Derives the death rate (agents lost per 1000 cycles) between consecutive samples.
        /// Members are removed on death and <c>TotalEver</c> only ever grows on birth, so within a
        /// window: deaths = births − Δalive = (TotalEver_now − TotalEver_prev) − (alive_now − alive_prev).
        /// Normalising by the cycle gap gives a rate that is comparable regardless of sample spacing.
        /// </summary>
        /// <summary>
        /// Rate of change of mean longevity (fitness) per 1000 cycles between consecutive samples.
        /// Positive = the average agent is surviving longer over time (evolution gaining ground);
        /// negative = the mean is falling, e.g. when long-lived survivors are replaced by fresh agents.
        /// </summary>
        private static double[] EvolutionRatePer1000(PopulationSample[] series)
        {
            if (series == null || series.Length < 2)
                return Array.Empty<double>();

            double[] rate = new double[series.Length - 1];
            for (int i = 1; i < series.Length; i++)
            {
                int cycleDelta = series[i].Cycle - series[i - 1].Cycle;
                double meanDelta = series[i].MeanFitness - series[i - 1].MeanFitness;
                rate[i - 1] = cycleDelta > 0 ? meanDelta * 1000.0 / cycleDelta : 0;
            }
            return rate;
        }

        /// <summary>Expresses each value as a percentage of the population size limit, clamped to 0–100%.</summary>
        private static double[] PercentOfSize(double[] values, int sizeLimit)
        {
            if (values == null || values.Length == 0 || sizeLimit <= 0)
                return Array.Empty<double>();

            double[] pct = new double[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                double p = values[i] * 100.0 / sizeLimit;
                pct[i] = p < 0 ? 0 : (p > 100 ? 100 : p);
            }
            return pct;
        }

        private static double[] DeathsPer1000Cycles(PopulationSample[] series)
        {
            if (series == null || series.Length < 2)
                return Array.Empty<double>();

            double[] rate = new double[series.Length - 1];
            for (int i = 1; i < series.Length; i++)
            {
                int cycleDelta = series[i].Cycle - series[i - 1].Cycle;
                int births = series[i].TotalEver - series[i - 1].TotalEver;
                int aliveDelta = series[i].Alive - series[i - 1].Alive;
                int deaths = births - aliveDelta;
                if (deaths < 0)
                    deaths = 0;
                rate[i - 1] = cycleDelta > 0 ? deaths * 1000.0 / cycleDelta : 0;
            }
            return rate;
        }

        private static double[] MergeIntervals(GoldenAverageEvent[] events)
        {
            if (events == null || events.Length < 2)
                return Array.Empty<double>();
            double[] intervals = new double[events.Length - 1];
            for (int i = 1; i < events.Length; i++)
                intervals[i - 1] = Math.Max(0, events[i].Cycle - events[i - 1].Cycle);
            return intervals;
        }

        private static List<LayerGene> Layers(NeuralNetworkGene gene)
        {
            List<LayerGene> layers = new List<LayerGene>();
            if (gene == null) return layers;
            if (gene.InputGene != null) layers.Add(gene.InputGene);
            if (gene.HiddenGenes != null) layers.AddRange(gene.HiddenGenes);
            if (gene.OutputGene != null) layers.Add(gene.OutputGene);
            return layers;
        }

        private static string LayerTitle(int index, int count)
        {
            if (index == 0) return "In";
            if (index == count - 1) return "Out";
            return "H" + index;
        }

        private static List<ChangedElement> CollectChanges(NeuralNetworkGene initial, NeuralNetworkGene current)
        {
            List<ChangedElement> list = new List<ChangedElement>();
            List<LayerGene> a = Layers(initial);
            List<LayerGene> b = Layers(current);
            int layerCount = Math.Min(a.Count, b.Count);
            for (int li = 0; li < layerCount; li++)
            {
                string title = LayerTitle(li, layerCount);
                int neurons = Math.Min(a[li]?.Neurons?.Count ?? 0, b[li]?.Neurons?.Count ?? 0);
                for (int ni = 0; ni < neurons; ni++)
                {
                    NeuronGene na = a[li].Neurons[ni];
                    NeuronGene nb = b[li].Neurons[ni];
                    double ba = na?.Soma?.Bias ?? 0, bb = nb?.Soma?.Bias ?? 0;
                    list.Add(new ChangedElement { Label = $"{title}.{ni + 1} bias", Initial = ba, Current = bb, Delta = bb - ba });

                    int w = Math.Min(na?.Axon?.Weights?.Count ?? 0, nb?.Axon?.Weights?.Count ?? 0);
                    for (int wi = 0; wi < w; wi++)
                    {
                        double wa = na.Axon.Weights[wi], wbv = nb.Axon.Weights[wi];
                        list.Add(new ChangedElement { Label = $"{title}.{ni + 1} w{wi + 1}", Initial = wa, Current = wbv, Delta = wbv - wa });
                    }
                }
            }
            return list;
        }

        private static double[] PerLayerMeanDelta(NeuralNetworkGene initial, NeuralNetworkGene current, out string[] labels)
        {
            List<LayerGene> a = Layers(initial);
            List<LayerGene> b = Layers(current);
            int layerCount = Math.Min(a.Count, b.Count);
            double[] means = new double[layerCount];
            labels = new string[layerCount];
            for (int li = 0; li < layerCount; li++)
            {
                labels[li] = LayerTitle(li, layerCount);
                double sum = 0;
                int n = 0;
                int neurons = Math.Min(a[li]?.Neurons?.Count ?? 0, b[li]?.Neurons?.Count ?? 0);
                for (int ni = 0; ni < neurons; ni++)
                {
                    NeuronGene na = a[li].Neurons[ni];
                    NeuronGene nb = b[li].Neurons[ni];
                    sum += Math.Abs((nb?.Soma?.Bias ?? 0) - (na?.Soma?.Bias ?? 0)); n++;
                    int w = Math.Min(na?.Axon?.Weights?.Count ?? 0, nb?.Axon?.Weights?.Count ?? 0);
                    for (int wi = 0; wi < w; wi++) { sum += Math.Abs(nb.Axon.Weights[wi] - na.Axon.Weights[wi]); n++; }
                }
                means[li] = n > 0 ? sum / n : 0;
            }
            return means;
        }

        private struct ChangedElement
        {
            public string Label;
            public double Initial;
            public double Current;
            public double Delta;
        }
    }
}
