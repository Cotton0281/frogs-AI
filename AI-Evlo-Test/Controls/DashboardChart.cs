using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AI_Evlo_Test
{
    /// <summary>A lightweight snapshot-driven chart with no native or legacy UI dependencies.</summary>
    public sealed class DashboardChart : UserControl
    {
        private double[] primary = Array.Empty<double>();
        private double[] secondary = Array.Empty<double>();
        private string[] labels = Array.Empty<string>();
        private ChartKind kind;

        public string Caption { get; set; } = "";
        public Color PrimaryColor { get; set; } = Color.FromArgb(40, 120, 215);
        public Color SecondaryColor { get; set; } = Color.FromArgb(220, 90, 40);
        public string PrimaryLabel { get; set; } = "";
        public string SecondaryLabel { get; set; } = "";
        public bool BaselineZero { get; set; } = true;

        public DashboardChart()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        public void SetLine(double[] primaryValues, double[] secondaryValues = null)
        {
            primary = Copy(primaryValues);
            secondary = Copy(secondaryValues);
            labels = Array.Empty<string>();
            kind = ChartKind.Line;
            Invalidate();
        }

        public void SetLinePercentRight(double[] primaryValues, double[] percentValues)
        {
            primary = Copy(primaryValues);
            secondary = Copy(percentValues);
            labels = Array.Empty<string>();
            kind = ChartKind.LinePercent;
            Invalidate();
        }

        public void SetBars(double[] values, string[] axisLabels = null)
        {
            primary = Copy(values);
            secondary = Array.Empty<double>();
            labels = axisLabels?.ToArray() ?? Array.Empty<string>();
            kind = ChartKind.Bars;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var titleFont = new Font(Font, FontStyle.Bold);
            graphics.DrawString(Caption ?? "", titleFont, Brushes.DimGray, 8, 5);
            Rectangle plot = new Rectangle(42, 28, Math.Max(1, Width - 54), Math.Max(1, Height - 54));
            using var gridPen = new Pen(Color.FromArgb(235, 238, 242));
            using var axisPen = new Pen(Color.FromArgb(120, 126, 136));
            for (int i = 0; i <= 4; i++)
            {
                int y = plot.Top + plot.Height * i / 4;
                graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
            }
            graphics.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
            graphics.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

            if (primary.Length == 0 && secondary.Length == 0)
            {
                graphics.DrawString("collecting data…", Font, Brushes.Gray, plot.Left + 8, plot.Top + 8);
                return;
            }

            if (kind == ChartKind.Bars)
                DrawBars(graphics, plot);
            else
                DrawLines(graphics, plot);

            DrawLegend(graphics, plot);
        }

        private void DrawLines(Graphics graphics, Rectangle plot)
        {
            double min = BaselineZero ? 0 : Minimum(primary, secondary);
            double max = Maximum(primary, kind == ChartKind.LinePercent ? Array.Empty<double>() : secondary);
            if (max <= min)
                max = min + 1;

            DrawLine(graphics, plot, primary, min, max, PrimaryColor);
            if (secondary.Length > 0)
            {
                double secondaryMin = kind == ChartKind.LinePercent ? 0 : min;
                double secondaryMax = kind == ChartKind.LinePercent ? 100 : max;
                DrawLine(graphics, plot, secondary, secondaryMin, secondaryMax, SecondaryColor);
            }
        }

        private void DrawBars(Graphics graphics, Rectangle plot)
        {
            double max = Math.Max(1, primary.Max());
            float slot = (float)plot.Width / primary.Length;
            using var brush = new SolidBrush(PrimaryColor);
            for (int i = 0; i < primary.Length; i++)
            {
                float height = (float)(Math.Max(0, primary[i]) / max * plot.Height);
                graphics.FillRectangle(brush, plot.Left + i * slot + 1, plot.Bottom - height,
                    Math.Max(1, slot - 2), height);
                if (i < labels.Length && labels.Length <= 12)
                    graphics.DrawString(labels[i], Font, Brushes.DimGray, plot.Left + i * slot, plot.Bottom + 2);
            }
        }

        private static void DrawLine(Graphics graphics, Rectangle plot, double[] values,
            double min, double max, Color color)
        {
            if (values.Length == 0)
                return;

            using var pen = new Pen(color, 2);
            PointF PreviousPoint(int index) => new PointF(
                plot.Left + (values.Length == 1 ? 0 : (float)index / (values.Length - 1) * plot.Width),
                plot.Bottom - (float)((values[index] - min) / (max - min) * plot.Height));

            PointF previous = PreviousPoint(0);
            if (values.Length == 1)
                graphics.DrawEllipse(pen, previous.X - 1, previous.Y - 1, 2, 2);
            for (int i = 1; i < values.Length; i++)
            {
                PointF current = PreviousPoint(i);
                graphics.DrawLine(pen, previous, current);
                previous = current;
            }
        }

        private void DrawLegend(Graphics graphics, Rectangle plot)
        {
            float x = plot.Left + 6;
            if (!string.IsNullOrWhiteSpace(PrimaryLabel))
            {
                using var brush = new SolidBrush(PrimaryColor);
                graphics.FillRectangle(brush, x, plot.Top + 5, 10, 3);
                graphics.DrawString(PrimaryLabel, Font, Brushes.DimGray, x + 14, plot.Top);
                x += graphics.MeasureString(PrimaryLabel, Font).Width + 28;
            }
            if (!string.IsNullOrWhiteSpace(SecondaryLabel) && secondary.Length > 0)
            {
                using var brush = new SolidBrush(SecondaryColor);
                graphics.FillRectangle(brush, x, plot.Top + 5, 10, 3);
                graphics.DrawString(SecondaryLabel, Font, Brushes.DimGray, x + 14, plot.Top);
            }
        }

        private static double[] Copy(double[] values) => values?.ToArray() ?? Array.Empty<double>();

        private static double Minimum(double[] first, double[] second) =>
            first.Concat(second).DefaultIfEmpty(0).Min();

        private static double Maximum(double[] first, double[] second) =>
            first.Concat(second).DefaultIfEmpty(0).Max();

        private enum ChartKind { Line, LinePercent, Bars }
    }
}
