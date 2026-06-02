using ArtificialNeuralNetwork;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AI_Evlo_Test
{
    /// <summary>
    /// Draws neural networks as fixed left-to-right layers. A custom layout is a better fit
    /// than a generic graph layout because neural networks already have strict columns.
    /// </summary>
    public sealed class NeuralNetworkView : Control
    {
        private readonly ToolTip nodeToolTip = new ToolTip();
        private readonly List<NodeVisual> lastNodes = new List<NodeVisual>();
        private INeuralNetwork network;
        private string lastToolTipText = "";

        public NeuralNetworkView()
        {
            BackColor = Color.White;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public INeuralNetwork Network
        {
            get { return network; }
            set
            {
                network = value;
                Invalidate();
            }
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

            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            lastNodes.Clear();

            Rectangle client = ClientRectangle;
            if (client.Width <= 0 || client.Height <= 0)
                return;

            if (network == null)
            {
                DrawCenteredMessage(e.Graphics, "No network selected");
                return;
            }

            List<LayerVisual> layers = BuildLayers(network, client);
            if (layers.Count == 0)
            {
                DrawCenteredMessage(e.Graphics, "Network has no layers");
                return;
            }

            DrawEdges(e.Graphics, layers);
            DrawNodes(e.Graphics, layers);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            string text = "";
            for (int i = 0; i < lastNodes.Count; i++)
            {
                if (lastNodes[i].Bounds.Contains(e.Location))
                {
                    text = lastNodes[i].ToolTip;
                    break;
                }
            }

            if (text != lastToolTipText)
            {
                lastToolTipText = text;
                nodeToolTip.SetToolTip(this, text);
            }
        }

        private static List<LayerVisual> BuildLayers(INeuralNetwork network, Rectangle client)
        {
            List<LayerDescriptor> descriptors = new List<LayerDescriptor>();

            int inputCount = network.Inputs?.Count ?? 0;
            descriptors.Add(new LayerDescriptor("Inputs", inputCount, null, Color.FromArgb(255, 248, 190)));

            if (network.HiddenLayers != null)
            {
                for (int i = 0; i < network.HiddenLayers.Count; i++)
                {
                    ILayer layer = network.HiddenLayers[i];
                    int count = layer?.NeuronsInLayer?.Count ?? 0;
                    descriptors.Add(new LayerDescriptor($"H{i + 1}", count, layer, Color.FromArgb(170, 215, 255)));
                }
            }

            int outputCount = network.OutputLayer?.NeuronsInLayer?.Count ?? 0;
            descriptors.Add(new LayerDescriptor("Outputs", outputCount, network.OutputLayer, Color.FromArgb(175, 232, 175)));

            int maxNodes = 1;
            for (int i = 0; i < descriptors.Count; i++)
                maxNodes = Math.Max(maxNodes, descriptors[i].NodeCount);

            float left = 70;
            float right = 70;
            float top = 48;
            float bottom = 48;
            RectangleF bounds = new RectangleF(
                left,
                top,
                Math.Max(1, client.Width - left - right),
                Math.Max(1, client.Height - top - bottom));

            float nodeRadius = Math.Max(3, Math.Min(14, bounds.Height / (maxNodes * 3.2f)));
            int layerCount = Math.Max(1, descriptors.Count);
            List<LayerVisual> layers = new List<LayerVisual>();

            for (int layerIndex = 0; layerIndex < descriptors.Count; layerIndex++)
            {
                LayerDescriptor descriptor = descriptors[layerIndex];
                float x = layerCount == 1
                    ? bounds.Left + bounds.Width / 2
                    : bounds.Left + (bounds.Width * layerIndex / (layerCount - 1));

                LayerVisual visual = new LayerVisual
                {
                    Title = $"{descriptor.Title} ({descriptor.NodeCount})",
                    Layer = descriptor.Layer,
                    FillColor = descriptor.FillColor,
                    X = x,
                    NodeRadius = nodeRadius
                };

                for (int nodeIndex = 0; nodeIndex < descriptor.NodeCount; nodeIndex++)
                {
                    float y = descriptor.NodeCount <= 1
                        ? bounds.Top + bounds.Height / 2
                        : bounds.Top + (bounds.Height * nodeIndex / (descriptor.NodeCount - 1));

                    string id = GetNodeId(descriptor.Title, nodeIndex);
                    visual.Nodes.Add(new NodeVisual
                    {
                        Id = id,
                        Bounds = new RectangleF(x - nodeRadius, y - nodeRadius, nodeRadius * 2, nodeRadius * 2),
                        ToolTip = BuildToolTip(network, layerIndex, nodeIndex, id)
                    });
                }

                layers.Add(visual);
            }

            return layers;
        }

        private static string GetNodeId(string layerTitle, int nodeIndex)
        {
            if (layerTitle == "Inputs")
                return $"I{nodeIndex + 1}";

            if (layerTitle == "Outputs")
                return $"O{nodeIndex + 1}";

            return $"{layerTitle}.{nodeIndex + 1}";
        }

        private static string BuildToolTip(INeuralNetwork network, int layerIndex, int nodeIndex, string id)
        {
            if (layerIndex == 0)
            {
                double value = network.Inputs != null && nodeIndex < network.Inputs.Count
                    ? network.Inputs[nodeIndex].Axon?.Value ?? 0
                    : 0;
                return $"{id}\r\nValue: {value:0.###}";
            }

            ILayer layer = null;
            int hiddenCount = network.HiddenLayers?.Count ?? 0;
            if (layerIndex <= hiddenCount)
                layer = network.HiddenLayers[layerIndex - 1];
            else
                layer = network.OutputLayer;

            INeuron neuron = layer?.NeuronsInLayer != null && nodeIndex < layer.NeuronsInLayer.Count
                ? layer.NeuronsInLayer[nodeIndex]
                : null;

            double output = neuron?.Axon?.Value ?? 0;
            double bias = neuron?.Soma?.Bias ?? 0;
            return $"{id}\r\nOutput: {output:0.###}\r\nBias: {bias:0.###}";
        }

        private static void DrawEdges(Graphics g, List<LayerVisual> layers)
        {
            for (int layerIndex = 1; layerIndex < layers.Count; layerIndex++)
            {
                LayerVisual sourceLayer = layers[layerIndex - 1];
                LayerVisual targetLayer = layers[layerIndex];
                ILayer neuralLayer = targetLayer.Layer;
                if (neuralLayer?.NeuronsInLayer == null)
                    continue;

                for (int targetIndex = 0; targetIndex < targetLayer.Nodes.Count; targetIndex++)
                {
                    INeuron targetNeuron = targetIndex < neuralLayer.NeuronsInLayer.Count
                        ? neuralLayer.NeuronsInLayer[targetIndex]
                        : null;

                    PointF target = Center(targetLayer.Nodes[targetIndex].Bounds);
                    for (int sourceIndex = 0; sourceIndex < sourceLayer.Nodes.Count; sourceIndex++)
                    {
                        double weight = GetWeight(targetNeuron, sourceIndex);
                        PointF source = Center(sourceLayer.Nodes[sourceIndex].Bounds);
                        DrawWeightedLine(g, source, target, weight, sourceLayer.NodeRadius, targetLayer.NodeRadius);
                    }
                }
            }
        }

        private static void DrawWeightedLine(Graphics g, PointF source, PointF target, double weight, float sourceRadius, float targetRadius)
        {
            float dx = target.X - source.X;
            float dy = target.Y - source.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0.01f)
                return;

            PointF start = new PointF(source.X + dx / length * sourceRadius, source.Y + dy / length * sourceRadius);
            PointF end = new PointF(target.X - dx / length * targetRadius, target.Y - dy / length * targetRadius);

            int alpha = Math.Min(180, 35 + (int)(Math.Abs(weight) * 80));
            Color color = weight < 0
                ? Color.FromArgb(alpha, 210, 45, 35)
                : Color.FromArgb(alpha, 30, 125, 60);
            float width = 1f + Math.Min(3f, (float)Math.Abs(weight));

            using (Pen pen = new Pen(color, width))
                g.DrawLine(pen, start, end);
        }

        private static double GetWeight(INeuron targetNeuron, int sourceIndex)
        {
            if (targetNeuron?.Soma?.Dendrites != null && sourceIndex < targetNeuron.Soma.Dendrites.Count)
                return targetNeuron.Soma.Dendrites[sourceIndex].Weight;

            return 0;
        }

        private void DrawNodes(Graphics g, List<LayerVisual> layers)
        {
            using (Pen borderPen = new Pen(Color.FromArgb(40, 40, 40), 1f))
            using (Font headerFont = new Font(Font.FontFamily, 9, FontStyle.Bold))
            using (Font nodeFont = new Font(Font.FontFamily, 7, FontStyle.Regular))
            using (StringFormat center = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (Brush textBrush = new SolidBrush(Color.FromArgb(30, 30, 30)))
            {
                for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
                {
                    LayerVisual layer = layers[layerIndex];
                    g.DrawString(layer.Title, headerFont, textBrush, layer.X, 12, center);

                    using (Brush fill = new SolidBrush(layer.FillColor))
                    {
                        for (int nodeIndex = 0; nodeIndex < layer.Nodes.Count; nodeIndex++)
                        {
                            NodeVisual node = layer.Nodes[nodeIndex];
                            g.FillEllipse(fill, node.Bounds);
                            g.DrawEllipse(borderPen, node.Bounds);

                            if (layer.NodeRadius >= 7)
                            {
                                string label = (nodeIndex + 1).ToString();
                                g.DrawString(label, nodeFont, textBrush, Center(node.Bounds), center);
                            }

                            lastNodes.Add(node);
                        }
                    }
                }
            }
        }

        private static PointF Center(RectangleF bounds)
        {
            return new PointF(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        }

        private void DrawCenteredMessage(Graphics g, string message)
        {
            using (Brush brush = new SolidBrush(Color.FromArgb(80, 80, 80)))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(message, Font, brush, ClientRectangle, format);
        }

        private sealed class LayerDescriptor
        {
            public LayerDescriptor(string title, int nodeCount, ILayer layer, Color fillColor)
            {
                Title = title;
                NodeCount = nodeCount;
                Layer = layer;
                FillColor = fillColor;
            }

            public string Title { get; }
            public int NodeCount { get; }
            public ILayer Layer { get; }
            public Color FillColor { get; }
        }

        private sealed class LayerVisual
        {
            public string Title { get; set; }
            public ILayer Layer { get; set; }
            public Color FillColor { get; set; }
            public float X { get; set; }
            public float NodeRadius { get; set; }
            public List<NodeVisual> Nodes { get; } = new List<NodeVisual>();
        }

        private struct NodeVisual
        {
            public string Id { get; set; }
            public RectangleF Bounds { get; set; }
            public string ToolTip { get; set; }
        }
    }
}
