using System;
using System.Drawing;
using System.Windows.Forms;
using ScottPlot;
using ScottPlot.WinForms;
// Both System.Drawing and ScottPlot define a Color type; bare "Color" means the WinForms one here.
using Color = System.Drawing.Color;

namespace AI_Evlo_Test
{
    /// <summary>
    /// A small ScottPlot-backed chart used by the Population Dashboard. Wraps a <see cref="FormsPlot"/>
    /// and exposes a tiny, snapshot-friendly API: <see cref="SetLine"/> for one or two line series and
    /// <see cref="SetBars"/> for a single bar series. Styling is kept light to match the dashboard.
    /// Replaces the earlier dependency-free GDI sparkline with sharper, auto-scaling plots.
    /// </summary>
    public sealed class DashboardChart : UserControl
    {
        private readonly FormsPlot _plot = new FormsPlot { Dock = DockStyle.Fill };
        // Lazily-created secondary Y axis (0–100%) on the right, reused across refreshes.
        private IYAxis _rightAxis;

        public string Caption { get; set; } = "";
        public Color PrimaryColor { get; set; } = Color.FromArgb(40, 120, 215);
        public Color SecondaryColor { get; set; } = Color.FromArgb(220, 90, 40);
        public string PrimaryLabel { get; set; } = "";
        public string SecondaryLabel { get; set; } = "";

        /// <summary>When true, the Y axis is forced to start at zero (used by the line charts).</summary>
        public bool BaselineZero { get; set; } = true;

        public DashboardChart()
        {
            Controls.Add(_plot);
            StyleClean(_plot.Plot);
        }

        /// <summary>Draws one or two index-based line series (e.g. top vs mean fitness).</summary>
        public void SetLine(double[] primary, double[] secondary = null)
        {
            Plot plot = _plot.Plot;
            plot.Clear();
            ApplyTitle(plot);

            if (primary == null || primary.Length == 0)
            {
                ShowCollecting(plot);
                _plot.Refresh();
                return;
            }

            var sig = plot.Add.Signal(primary);
            sig.Color = ToColor(PrimaryColor);
            sig.LineWidth = 2;
            sig.LegendText = PrimaryLabel;

            if (secondary != null && secondary.Length > 0)
            {
                var sig2 = plot.Add.Signal(secondary);
                sig2.Color = ToColor(SecondaryColor);
                sig2.LineWidth = 2;
                sig2.LegendText = SecondaryLabel;
            }

            if (!string.IsNullOrEmpty(PrimaryLabel) || !string.IsNullOrEmpty(SecondaryLabel))
                plot.ShowLegend(Alignment.UpperLeft);
            else
                plot.HideLegend();

            plot.Axes.AutoScale();
            if (BaselineZero)
            {
                AxisLimits lim = plot.Axes.GetLimits();
                plot.Axes.SetLimitsY(Math.Min(0, lim.Bottom), lim.Top);
            }

            _plot.Refresh();
        }

        /// <summary>
        /// Draws the primary series against the left (auto-scaled) axis and a companion percentage
        /// series against a dedicated right axis pinned to 0–100%. Used to pair an absolute count with
        /// its share of a capacity (e.g. alive vs % of limit, death rate vs % of population).
        /// </summary>
        public void SetLinePercentRight(double[] primary, double[] percent)
        {
            Plot plot = _plot.Plot;
            plot.Clear();
            ApplyTitle(plot);

            bool hasPrimary = primary != null && primary.Length > 0;
            bool hasPercent = percent != null && percent.Length > 0;
            if (!hasPrimary && !hasPercent)
            {
                ShowCollecting(plot);
                _plot.Refresh();
                return;
            }

            if (hasPrimary)
            {
                var sig = plot.Add.Signal(primary);
                sig.Color = ToColor(PrimaryColor);
                sig.LineWidth = 2;
                sig.LegendText = PrimaryLabel;
            }

            IYAxis right = EnsureRightAxis(plot);
            if (hasPercent)
            {
                var sigP = plot.Add.Signal(percent);
                sigP.Color = ToColor(SecondaryColor);
                sigP.LineWidth = 2;
                sigP.LegendText = SecondaryLabel;
                sigP.Axes.YAxis = right;
            }

            plot.ShowLegend(Alignment.UpperLeft);

            // Auto-scale the left/X axes from the primary series, force the left baseline to zero,
            // then pin the right axis to a fixed 0–100% range.
            plot.Axes.AutoScale();
            if (BaselineZero)
            {
                AxisLimits lim = plot.Axes.GetLimits();
                plot.Axes.SetLimitsY(Math.Min(0, lim.Bottom), lim.Top);
            }
            plot.Axes.SetLimitsY(0, 100, right);

            _plot.Refresh();
        }

        /// <summary>Draws a single bar series, optionally labelled along the X axis.</summary>
        public void SetBars(double[] values, string[] labels = null)
        {
            Plot plot = _plot.Plot;
            plot.Clear();
            ApplyTitle(plot);
            plot.HideLegend();

            if (values == null || values.Length == 0)
            {
                ShowCollecting(plot);
                _plot.Refresh();
                return;
            }

            ScottPlot.Plottables.BarPlot bars = plot.Add.Bars(values);
            ScottPlot.Color color = ToColor(PrimaryColor);
            foreach (Bar bar in bars.Bars)
            {
                bar.FillColor = color;
                bar.LineWidth = 0;
            }

            if (labels != null && labels.Length > 0)
            {
                double[] positions = new double[labels.Length];
                for (int i = 0; i < labels.Length; i++)
                    positions[i] = i;
                plot.Axes.Bottom.SetTicks(positions, labels);
            }

            plot.Axes.AutoScale();
            AxisLimits lim = plot.Axes.GetLimits();
            plot.Axes.SetLimitsY(0, lim.Top <= 0 ? 1 : lim.Top);

            _plot.Refresh();
        }

        // Creates the right "%" axis once and reuses it; Plot.Clear() keeps axes, so re-adding each
        // refresh would stack duplicates.
        private IYAxis EnsureRightAxis(Plot plot)
        {
            if (_rightAxis == null)
            {
                var axis = plot.Axes.AddRightAxis();
                axis.LabelText = "%";
                axis.LabelFontColor = ToColor(SecondaryColor);
                axis.TickLabelStyle.ForeColor = ToColor(SecondaryColor);
                _rightAxis = axis;
            }
            return _rightAxis;
        }

        private void ApplyTitle(Plot plot)
        {
            if (string.IsNullOrEmpty(Caption))
                return;
            plot.Title(Caption);
            plot.Axes.Title.Label.FontSize = 13;
        }

        private static void ShowCollecting(Plot plot)
        {
            plot.Add.Annotation("collecting data…");
        }

        private static void StyleClean(Plot plot)
        {
            plot.FigureBackground.Color = new ScottPlot.Color(255, 255, 255);
            plot.DataBackground.Color = new ScottPlot.Color(255, 255, 255);
            plot.Grid.MajorLineColor = new ScottPlot.Color(235, 238, 242);
            plot.Axes.Color(new ScottPlot.Color(120, 126, 136));
        }

        private static ScottPlot.Color ToColor(Color c) => new ScottPlot.Color(c.R, c.G, c.B, c.A);
    }
}
