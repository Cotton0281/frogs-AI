using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ArtificialNeuralNetwork.Genes;

namespace AI_Evlo_Test
{
    /// <summary>
    /// Draws a neural-network gene as fixed left-to-right layers (adapted from
    /// <see cref="NeuralNetworkView"/>) and colour-codes how much each weight and bias has changed
    /// between an <b>initial</b> golden brain and its <b>current</b> averaged brain.
    ///
    /// Colour encodes change: black = no change, saturated red = the largest change in the network.
    /// Line/node thickness encodes the element's value in this panel's state (initial or current),
    /// so the left panel shows the original brain's shape and the right panel the current one, while
    /// both share the same red change-heat so the diff lines up visually.
    ///
    /// Normalisation is shared across both panels (it is derived purely from initial vs current),
    /// so equal change always reads as the same red on both sides.
    /// </summary>
    public sealed class GeneDeltaNetworkView : Control
    {
        private readonly ToolTip nodeToolTip = new ToolTip();
        private readonly List<NodeHit> hits = new List<NodeHit>();
        private string lastToolTip = "";

        private NeuralNetworkGene initial;
        private NeuralNetworkGene current;

        /// <summary>When true, line/node thickness reflects the current brain; otherwise the initial brain.</summary>
        public bool ShowCurrent { get; set; }

        public string Caption { get; set; } = "";

        public GeneDeltaNetworkView()
        {
            BackColor = Color.FromArgb(28, 30, 38);
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        public void SetGenes(NeuralNetworkGene initialGene, NeuralNetworkGene currentGene)
        {
            initial = initialGene;
            current = currentGene;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                nodeToolTip.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            hits.Clear();

            if (!string.IsNullOrEmpty(Caption))
                using (Brush b = new SolidBrush(Color.FromArgb(220, 224, 230)))
                using (Font f = new Font(Font.FontFamily, 9f, FontStyle.Bold))
                    g.DrawString(Caption, f, b, 8, 6);

            List<LayerGene> layers = BuildLayerList(current ?? initial);
            if (layers == null || layers.Count == 0)
            {
                DrawCentered(g, "No golden brain yet — waiting for the first survivor to seed it.");
                return;
            }

            // Shared change normalisation derived from initial vs current.
            ComputeMaxDeltas(out double maxWeightDelta, out double maxBiasDelta);

            Rectangle client = ClientRectangle;
            float left = 30, right = 30, top = 30, bottom = 18;
            RectangleF bounds = new RectangleF(left, top,
                Math.Max(1, client.Width - left - right),
                Math.Max(1, client.Height - top - bottom));

            int maxNodes = 1;
            foreach (LayerGene l in layers)
                maxNodes = Math.Max(maxNodes, l?.Neurons?.Count ?? 0);
            float nodeRadius = Math.Max(2.5f, Math.Min(11f, bounds.Height / (maxNodes * 2.6f)));

            // Pre-compute node centres per layer.
            PointF[][] centres = new PointF[layers.Count][];
            for (int li = 0; li < layers.Count; li++)
            {
                int count = layers[li]?.Neurons?.Count ?? 0;
                centres[li] = new PointF[count];
                float x = layers.Count == 1
                    ? bounds.Left + bounds.Width / 2
                    : bounds.Left + bounds.Width * li / (layers.Count - 1);
                for (int ni = 0; ni < count; ni++)
                {
                    float y = count <= 1
                        ? bounds.Top + bounds.Height / 2
                        : bounds.Top + bounds.Height * ni / (count - 1);
                    centres[li][ni] = new PointF(x, y);
                }
            }

            NeuralNetworkGene thicknessGene = (ShowCurrent ? current : initial) ?? current ?? initial;
            List<LayerGene> thicknessLayers = BuildLayerList(thicknessGene);

            DrawEdges(g, layers, thicknessLayers, centres, nodeRadius, maxWeightDelta);
            DrawNodes(g, layers, thicknessLayers, centres, nodeRadius, maxBiasDelta);
        }

        private void DrawEdges(Graphics g, List<LayerGene> layers, List<LayerGene> thicknessLayers,
            PointF[][] centres, float nodeRadius, double maxWeightDelta)
        {
            for (int li = 0; li < layers.Count - 1; li++)
            {
                LayerGene src = layers[li];
                LayerGene srcThick = li < thicknessLayers.Count ? thicknessLayers[li] : null;
                int srcCount = src?.Neurons?.Count ?? 0;
                int dstCount = centres[li + 1].Length;

                for (int i = 0; i < srcCount; i++)
                {
                    NeuronGene srcNeuron = src.Neurons[i];
                    NeuronGene initNeuron = GeneAt(initial, li, i);
                    NeuronGene curNeuron = GeneAt(current, li, i);
                    NeuronGene thickNeuron = srcThick != null && i < srcThick.Neurons.Count ? srcThick.Neurons[i] : srcNeuron;

                    int weightCount = srcNeuron?.Axon?.Weights?.Count ?? 0;
                    for (int j = 0; j < weightCount && j < dstCount; j++)
                    {
                        double initW = WeightAt(initNeuron, j);
                        double curW = WeightAt(curNeuron, j);
                        double delta = Math.Abs(curW - initW);
                        double thickW = Math.Abs(WeightAt(thickNeuron, j));

                        float t = maxWeightDelta > 0 ? (float)Math.Min(1.0, delta / maxWeightDelta) : 0f;
                        DrawEdge(g, centres[li][i], centres[li + 1][j], t, thickW, nodeRadius, maxWeightDelta);
                    }
                }
            }
        }

        private void DrawEdge(Graphics g, PointF a, PointF b, float changeT, double magnitude, float nodeRadius, double maxWeightDelta)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len <= 0.01f) return;

            PointF start = new PointF(a.X + dx / len * nodeRadius, a.Y + dy / len * nodeRadius);
            PointF end = new PointF(b.X - dx / len * nodeRadius, b.Y - dy / len * nodeRadius);

            Color color = ElementColor(changeT, hasBaseline: maxWeightDelta > 0);
            float width = 0.6f + Math.Min(3f, (float)magnitude);

            using (Pen pen = new Pen(color, width))
                g.DrawLine(pen, start, end);
        }

        /// <summary>
        /// On the dark background: the initial panel draws everything in a light neutral colour
        /// (it is the reference). The current panel ramps light-grey (unchanged) → red (largest
        /// change), so divergence from the initial brain reads as red.
        /// </summary>
        private Color ElementColor(float changeT, bool hasBaseline)
        {
            if (!ShowCurrent || !hasBaseline)
                return Color.FromArgb(210, 200, 210, 222); // light steel — the original brain, clearly visible

            int r = (int)(225 + 30 * changeT);
            int gb = (int)(225 * (1 - changeT));
            int alpha = (int)(110 + 145 * changeT);
            return Color.FromArgb(Math.Min(255, alpha), Math.Min(255, r), gb, gb);
        }

        private void DrawNodes(Graphics g, List<LayerGene> layers, List<LayerGene> thicknessLayers,
            PointF[][] centres, float nodeRadius, double maxBiasDelta)
        {
            string[] titles = LayerTitles(layers.Count);
            using (Pen border = new Pen(Color.FromArgb(180, 185, 195), 1f))
            using (Brush headerBrush = new SolidBrush(Color.FromArgb(170, 176, 186)))
            using (Font headerFont = new Font(Font.FontFamily, 7.5f, FontStyle.Bold))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center })
            {
                for (int li = 0; li < layers.Count; li++)
                {
                    int count = centres[li].Length;
                    if (count > 0)
                        g.DrawString($"{titles[li]} ({count})", headerFont, headerBrush, centres[li][0].X, 18, sf);

                    for (int ni = 0; ni < count; ni++)
                    {
                        double initBias = BiasAt(initial, li, ni);
                        double curBias = BiasAt(current, li, ni);
                        double delta = Math.Abs(curBias - initBias);
                        float t = maxBiasDelta > 0 ? (float)Math.Min(1.0, delta / maxBiasDelta) : 0f;

                        PointF c = centres[li][ni];
                        RectangleF rect = new RectangleF(c.X - nodeRadius, c.Y - nodeRadius, nodeRadius * 2, nodeRadius * 2);
                        Color fillColor = (ShowCurrent && maxBiasDelta > 0)
                            ? Color.FromArgb(Math.Min(255, (int)(225 + 30 * t)), (int)(225 * (1 - t)), (int)(225 * (1 - t)))
                            : Color.FromArgb(205, 212, 222); // light neutral on the initial panel
                        using (Brush fill = new SolidBrush(fillColor))
                            g.FillEllipse(fill, rect);
                        g.DrawEllipse(border, rect.X, rect.Y, rect.Width, rect.Height);

                        hits.Add(new NodeHit
                        {
                            Bounds = rect,
                            Text = $"{titles[li]}.{ni + 1}\r\ninitial bias: {initBias:0.###}\r\ncurrent bias: {curBias:0.###}\r\nΔ {curBias - initBias:+0.###;-0.###}"
                        });
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            string text = "";
            for (int i = 0; i < hits.Count; i++)
                if (hits[i].Bounds.Contains(e.Location)) { text = hits[i].Text; break; }

            if (text != lastToolTip)
            {
                lastToolTip = text;
                nodeToolTip.SetToolTip(this, text);
            }
        }

        // ---- gene helpers --------------------------------------------------------------------

        private void ComputeMaxDeltas(out double maxWeightDelta, out double maxBiasDelta)
        {
            maxWeightDelta = 0;
            maxBiasDelta = 0;
            if (initial == null || current == null)
                return;

            List<LayerGene> a = BuildLayerList(initial);
            List<LayerGene> b = BuildLayerList(current);
            int layerCount = Math.Min(a.Count, b.Count);
            for (int li = 0; li < layerCount; li++)
            {
                int neurons = Math.Min(a[li]?.Neurons?.Count ?? 0, b[li]?.Neurons?.Count ?? 0);
                for (int ni = 0; ni < neurons; ni++)
                {
                    NeuronGene na = a[li].Neurons[ni];
                    NeuronGene nb = b[li].Neurons[ni];
                    maxBiasDelta = Math.Max(maxBiasDelta, Math.Abs((nb?.Soma?.Bias ?? 0) - (na?.Soma?.Bias ?? 0)));

                    int w = Math.Min(na?.Axon?.Weights?.Count ?? 0, nb?.Axon?.Weights?.Count ?? 0);
                    for (int wi = 0; wi < w; wi++)
                        maxWeightDelta = Math.Max(maxWeightDelta, Math.Abs(nb.Axon.Weights[wi] - na.Axon.Weights[wi]));
                }
            }
        }

        private static List<LayerGene> BuildLayerList(NeuralNetworkGene gene)
        {
            if (gene == null)
                return new List<LayerGene>();

            List<LayerGene> layers = new List<LayerGene>();
            if (gene.InputGene != null) layers.Add(gene.InputGene);
            if (gene.HiddenGenes != null) layers.AddRange(gene.HiddenGenes);
            if (gene.OutputGene != null) layers.Add(gene.OutputGene);
            return layers;
        }

        private static string[] LayerTitles(int count)
        {
            string[] titles = new string[count];
            for (int i = 0; i < count; i++)
            {
                if (i == 0) titles[i] = "In";
                else if (i == count - 1) titles[i] = "Out";
                else titles[i] = "H" + i;
            }
            return titles;
        }

        private static NeuronGene GeneAt(NeuralNetworkGene gene, int layerIndex, int neuronIndex)
        {
            List<LayerGene> layers = BuildLayerList(gene);
            if (layerIndex < 0 || layerIndex >= layers.Count) return null;
            LayerGene layer = layers[layerIndex];
            if (layer?.Neurons == null || neuronIndex >= layer.Neurons.Count) return null;
            return layer.Neurons[neuronIndex];
        }

        private static double WeightAt(NeuronGene neuron, int index)
        {
            if (neuron?.Axon?.Weights == null || index >= neuron.Axon.Weights.Count) return 0;
            return neuron.Axon.Weights[index];
        }

        private static double BiasAt(NeuralNetworkGene gene, int layerIndex, int neuronIndex)
        {
            return GeneAt(gene, layerIndex, neuronIndex)?.Soma?.Bias ?? 0;
        }

        private void DrawCentered(Graphics g, string message)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(170, 176, 186)))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(message, Font, brush, ClientRectangle, sf);
        }

        private struct NodeHit
        {
            public RectangleF Bounds;
            public string Text;
        }
    }
}
