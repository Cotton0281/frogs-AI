using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AI_Evlo_Test
{
    /// <summary>
    /// A tiny self-scaling GDI chart used by the Population Dashboard. Supports a line mode
    /// (one or two series sharing an auto-scaled Y axis) and a bar mode (one series, used for
    /// histograms and the golden-longevity chart). Deliberately dependency-free — the project
    /// only carries Newtonsoft + Windows.Compatibility, so no third-party chart library.
    /// </summary>
    public sealed class SparklineChart : Control
    {
        public enum ChartKind { Line, Bars }

        private double[] primary = Array.Empty<double>();
        private double[] secondary;
        private string[] barLabels;

        public ChartKind Kind { get; set; } = ChartKind.Line;
        public string Caption { get; set; } = "";
        public Color PrimaryColor { get; set; } = Color.FromArgb(40, 120, 215);
        public Color SecondaryColor { get; set; } = Color.FromArgb(220, 90, 40);
        public string PrimaryLabel { get; set; } = "";
        public string SecondaryLabel { get; set; } = "";

        /// <summary>When true the Y axis is forced to start at zero rather than the data minimum.</summary>
        public bool BaselineZero { get; set; } = true;

        public SparklineChart()
        {
            BackColor = Color.White;
            DoubleBuffered = true;
            ResizeRedraw = true;
            Padding = new Padding(8);
        }

        public void SetLine(double[] primarySeries, double[] secondarySeries = null)
        {
            Kind = ChartKind.Line;
            primary = primarySeries ?? Array.Empty<double>();
            secondary = secondarySeries;
            barLabels = null;
            Invalidate();
        }

        public void SetBars(double[] values, string[] labels = null)
        {
            Kind = ChartKind.Bars;
            primary = values ?? Array.Empty<double>();
            secondary = null;
            barLabels = labels;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF plot = new RectangleF(
                Padding.Left,
                Padding.Top + (string.IsNullOrEmpty(Caption) ? 0 : 16),
                Math.Max(1, Width - Padding.Horizontal),
                Math.Max(1, Height - Padding.Vertical - (string.IsNullOrEmpty(Caption) ? 0 : 16)));

            using (Pen border = new Pen(Color.FromArgb(225, 228, 232)))
                g.DrawRectangle(border, plot.X, plot.Y, plot.Width, plot.Height);

            if (!string.IsNullOrEmpty(Caption))
                using (Brush capBrush = new SolidBrush(Color.FromArgb(70, 76, 86)))
                using (Font capFont = new Font(Font.FontFamily, 8.25f, FontStyle.Bold))
                    g.DrawString(Caption, capFont, capBrush, Padding.Left, Padding.Top - 1);

            if (primary.Length == 0)
            {
                DrawCenteredMessage(g, plot, "collecting data…");
                return;
            }

            if (Kind == ChartKind.Bars)
                DrawBars(g, plot);
            else
                DrawLines(g, plot);
        }

        private void DrawLines(Graphics g, RectangleF plot)
        {
            GetRange(out double min, out double max, primary, secondary);
            DrawSeries(g, plot, primary, min, max, PrimaryColor);
            if (secondary != null && secondary.Length > 0)
                DrawSeries(g, plot, secondary, min, max, SecondaryColor);
            DrawLegend(g, plot, max);
        }

        private void DrawSeries(Graphics g, RectangleF plot, double[] data, double min, double max, Color color)
        {
            if (data.Length < 2)
                return;

            double span = max - min;
            if (span <= 0) span = 1;

            PointF[] pts = new PointF[data.Length];
            float stepX = plot.Width / (data.Length - 1);
            for (int i = 0; i < data.Length; i++)
            {
                float x = plot.X + i * stepX;
                float y = plot.Bottom - (float)((data[i] - min) / span) * (plot.Height - 2) - 1;
                pts[i] = new PointF(x, y);
            }

            using (Pen pen = new Pen(color, 1.6f))
                g.DrawLines(pen, pts);
        }

        private void DrawBars(Graphics g, RectangleF plot)
        {
            GetRange(out double min, out double max, primary, null);
            if (BaselineZero) min = 0;
            double span = max - min;
            if (span <= 0) span = 1;

            float slot = plot.Width / primary.Length;
            float barWidth = Math.Max(1f, slot * 0.7f);
            using (Brush bar = new SolidBrush(PrimaryColor))
            using (Brush lbl = new SolidBrush(Color.FromArgb(110, 116, 126)))
            using (Font lblFont = new Font(Font.FontFamily, 6.75f))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                for (int i = 0; i < primary.Length; i++)
                {
                    float h = (float)((primary[i] - min) / span) * (plot.Height - 2);
                    if (h < 0) h = 0;
                    float x = plot.X + i * slot + (slot - barWidth) / 2;
                    g.FillRectangle(bar, x, plot.Bottom - h - 1, barWidth, h);

                    if (barLabels != null && i < barLabels.Length)
                        g.DrawString(barLabels[i], lblFont, lbl, x + barWidth / 2, plot.Bottom + 1, sf);
                }
            }

            using (Brush maxBrush = new SolidBrush(Color.FromArgb(150, 156, 166)))
            using (Font f = new Font(Font.FontFamily, 7f))
                g.DrawString(FormatValue(max), f, maxBrush, plot.X + 2, plot.Y + 1);
        }

        private void DrawLegend(Graphics g, RectangleF plot, double max)
        {
            using (Brush axisBrush = new SolidBrush(Color.FromArgb(150, 156, 166)))
            using (Font f = new Font(Font.FontFamily, 7f))
            {
                g.DrawString(FormatValue(max), f, axisBrush, plot.X + 2, plot.Y + 1);

                float lx = plot.X + 2;
                float ly = plot.Bottom - 12;
                if (!string.IsNullOrEmpty(PrimaryLabel))
                {
                    using (Brush b = new SolidBrush(PrimaryColor))
                        g.FillRectangle(b, lx, ly + 3, 8, 4);
                    g.DrawString(PrimaryLabel, f, axisBrush, lx + 10, ly);
                    lx += 10 + g.MeasureString(PrimaryLabel, f).Width + 8;
                }
                if (!string.IsNullOrEmpty(SecondaryLabel) && secondary != null)
                {
                    using (Brush b = new SolidBrush(SecondaryColor))
                        g.FillRectangle(b, lx, ly + 3, 8, 4);
                    g.DrawString(SecondaryLabel, f, axisBrush, lx + 10, ly);
                }
            }
        }

        private void GetRange(out double min, out double max, double[] a, double[] b)
        {
            min = double.MaxValue;
            max = double.MinValue;
            Extend(a, ref min, ref max);
            Extend(b, ref min, ref max);
            if (min == double.MaxValue) { min = 0; max = 1; }
            if (BaselineZero && min > 0) min = 0;
            if (max <= min) max = min + 1;
        }

        private static void Extend(double[] data, ref double min, ref double max)
        {
            if (data == null) return;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] < min) min = data[i];
                if (data[i] > max) max = data[i];
            }
        }

        private static string FormatValue(double v)
        {
            if (Math.Abs(v) >= 1000)
                return (v / 1000.0).ToString("0.#") + "k";
            return v.ToString("0.##");
        }

        private void DrawCenteredMessage(Graphics g, RectangleF plot, string message)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(150, 156, 166)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(message, Font, brush, plot, sf);
        }
    }
}
