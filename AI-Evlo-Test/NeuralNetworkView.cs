using ArtificialNeuralNetwork;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AI_Evlo_Test
{
    public sealed class LayerLockToggleEventArgs : EventArgs
    {
        public int DestinationLayerIndex { get; }
        public bool Locked { get; }

        public LayerLockToggleEventArgs(int destinationLayerIndex, bool locked)
        {
            DestinationLayerIndex = destinationLayerIndex;
            Locked = locked;
        }
    }

    /// <summary>
    /// Immediate-mode renderer for a dense, strictly layered neural network. Geometry is drawn in
    /// one WPF visual instead of creating thousands of Line elements.
    /// </summary>
    public sealed class NeuralNetworkView : FrameworkElement
    {
        private readonly List<LockHit> lockHits = new List<LockHit>();
        private readonly List<NodeHit> nodeHits = new List<NodeHit>();
        private readonly ImageSource lockedImage = LoadImage("pack://application:,,,/img/nn-layer-locked.png");
        private readonly ImageSource unlockedImage = LoadImage("pack://application:,,,/img/nn-layer-unlocked.png");
        private INeuralNetwork network;
        private IReadOnlyList<bool> layerLocks = Array.Empty<bool>();
        private double zoom = 1;
        private Vector pan;
        private bool isPanning;
        private Point lastPanPoint;
        private double minimumAbsoluteWeight;

        public event EventHandler<LayerLockToggleEventArgs> LayerLockToggleRequested;

        public INeuralNetwork Network
        {
            get => network;
            set
            {
                network = value;
                InvalidateVisual();
            }
        }

        public double MinimumAbsoluteWeight
        {
            get => minimumAbsoluteWeight;
            set
            {
                minimumAbsoluteWeight = Math.Max(0, value);
                InvalidateVisual();
            }
        }

        public NeuralNetworkView()
        {
            Focusable = true;
            ClipToBounds = true;
            Cursor = Cursors.Arrow;
        }

        public void SetSnapshot(INeuralNetwork snapshot, IReadOnlyList<bool> locks)
        {
            network = snapshot;
            layerLocks = locks ?? Array.Empty<bool>();
            InvalidateVisual();
        }

        public void FitToView()
        {
            zoom = 1;
            pan = new Vector();
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFD)), null, new Rect(RenderSize));
            lockHits.Clear();
            nodeHits.Clear();

            if (network == null || ActualWidth < 100 || ActualHeight < 100)
            {
                DrawCenteredText(drawingContext, network == null ? "No network selected" : "Window is too small");
                return;
            }

            List<LayerVisual> layers = BuildLayers();
            Matrix viewMatrix = CreateViewMatrix();
            drawingContext.PushTransform(new MatrixTransform(viewMatrix));
            DrawLayerBands(drawingContext, layers);
            DrawEdges(drawingContext, layers);
            DrawNodesAndHeaders(drawingContext, layers, viewMatrix);
            drawingContext.Pop();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Point point = e.GetPosition(this);
            LockHit hit = lockHits.FirstOrDefault(candidate => candidate.Bounds.Contains(point));
            if (hit != null)
            {
                LayerLockToggleRequested?.Invoke(
                    this,
                    new LayerLockToggleEventArgs(hit.DestinationLayerIndex, !hit.Locked));
                e.Handled = true;
                return;
            }

            Focus();
            isPanning = true;
            lastPanPoint = point;
            CaptureMouse();
            Cursor = Cursors.ScrollAll;
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (isPanning)
            {
                isPanning = false;
                ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Point point = e.GetPosition(this);
            if (isPanning)
            {
                Vector delta = point - lastPanPoint;
                pan += delta;
                lastPanPoint = point;
                InvalidateVisual();
                return;
            }

            LockHit lockHit = lockHits.FirstOrDefault(candidate => candidate.Bounds.Contains(point));
            if (lockHit != null)
            {
                ToolTip = lockHit.Locked
                    ? "Locked: this layer's incoming weights and biases cannot mutate. Click to unlock."
                    : "Unlocked: this layer can mutate. Click to lock.";
                Cursor = Cursors.Hand;
                return;
            }

            NodeHit nodeHit = nodeHits.FirstOrDefault(candidate => candidate.Bounds.Contains(point));
            ToolTip = nodeHit?.ToolTip;
            Cursor = Cursors.Arrow;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            double oldZoom = zoom;
            zoom = Math.Max(0.45, Math.Min(3.5, zoom * (e.Delta > 0 ? 1.12 : 1 / 1.12)));
            if (Math.Abs(oldZoom - zoom) > 0.0001)
                InvalidateVisual();
            e.Handled = true;
        }

        private List<LayerVisual> BuildLayers()
        {
            var layers = new List<LayerVisual>();
            layers.Add(new LayerVisual
            {
                Title = "Inputs",
                Count = network.Inputs?.Count ?? 0,
                Fill = Color.FromRgb(0xFF, 0xF2, 0xB6)
            });

            for (int i = 0; i < (network.HiddenLayers?.Count ?? 0); i++)
            {
                ILayer layer = network.HiddenLayers[i];
                layers.Add(new LayerVisual
                {
                    Title = layer is ResidualLayer ? $"H{i + 1} · residual" : $"H{i + 1}",
                    Count = layer?.NeuronsInLayer?.Count ?? 0,
                    Layer = layer,
                    Fill = layer is ResidualLayer
                        ? Color.FromRgb(0xB9, 0xE8, 0xDF)
                        : Color.FromRgb(0xB5, 0xDA, 0xFF)
                });
            }

            layers.Add(new LayerVisual
            {
                Title = "Outputs",
                Count = network.OutputLayer?.NeuronsInLayer?.Count ?? 0,
                Layer = network.OutputLayer,
                Fill = Color.FromRgb(0xB6, 0xE8, 0xBF)
            });

            double left = 75;
            double right = 75;
            double top = 105;
            double bottom = 60;
            double width = Math.Max(1, ActualWidth - left - right);
            double height = Math.Max(1, ActualHeight - top - bottom);
            int maxNodes = Math.Max(1, layers.Max(layer => layer.Count));
            double radius = Math.Max(3, Math.Min(11, height / (maxNodes * 2.7)));

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                LayerVisual layer = layers[layerIndex];
                layer.X = layers.Count == 1
                    ? left + width / 2
                    : left + width * layerIndex / (layers.Count - 1);
                layer.NodeRadius = radius;
                for (int nodeIndex = 0; nodeIndex < layer.Count; nodeIndex++)
                {
                    double y = layer.Count <= 1
                        ? top + height / 2
                        : top + height * nodeIndex / (layer.Count - 1);
                    layer.Nodes.Add(new Point(layer.X, y));
                }
            }
            return layers;
        }

        private void DrawLayerBands(DrawingContext context, List<LayerVisual> layers)
        {
            for (int layerIndex = 1; layerIndex < layers.Count; layerIndex++)
            {
                bool locked = IsLocked(layerIndex - 1);
                Color bandColor = locked
                    ? Color.FromArgb(28, 0xE5, 0x8B, 0x38)
                    : Color.FromArgb(15, 0x3D, 0x78, 0xB5);
                context.DrawRoundedRectangle(
                    new SolidColorBrush(bandColor),
                    null,
                    new Rect(layers[layerIndex].X - 28, 54, 56, Math.Max(1, ActualHeight - 90)),
                    12,
                    12);
            }
        }

        private void DrawEdges(DrawingContext context, List<LayerVisual> layers)
        {
            for (int layerIndex = 1; layerIndex < layers.Count; layerIndex++)
            {
                LayerVisual source = layers[layerIndex - 1];
                LayerVisual target = layers[layerIndex];
                if (target.Layer?.NeuronsInLayer == null)
                    continue;

                if (target.Layer is ResidualLayer && source.Nodes.Count == target.Nodes.Count)
                {
                    var skipPen = new Pen(
                        new SolidColorBrush(Color.FromArgb(175, 0x2C, 0x83, 0xA5)),
                        1.6)
                    {
                        DashStyle = DashStyles.Dash
                    };
                    for (int nodeIndex = 0; nodeIndex < source.Nodes.Count; nodeIndex++)
                        context.DrawLine(skipPen, source.Nodes[nodeIndex], target.Nodes[nodeIndex]);
                }

                for (int targetIndex = 0; targetIndex < target.Nodes.Count; targetIndex++)
                {
                    INeuron targetNeuron = target.Layer.NeuronsInLayer[targetIndex];
                    for (int sourceIndex = 0; sourceIndex < source.Nodes.Count; sourceIndex++)
                    {
                        double weight = targetNeuron?.Soma?.Dendrites != null
                            && sourceIndex < targetNeuron.Soma.Dendrites.Count
                                ? targetNeuron.Soma.Dendrites[sourceIndex].Weight
                                : 0;
                        if (Math.Abs(weight) < minimumAbsoluteWeight)
                            continue;

                        byte alpha = (byte)Math.Min(185, 35 + Math.Abs(weight) * 100);
                        Color color = weight < 0
                            ? Color.FromArgb(alpha, 0xD9, 0x45, 0x45)
                            : Color.FromArgb(alpha, 0x2F, 0x91, 0x5A);
                        double thickness = 0.65 + Math.Min(2.7, Math.Abs(weight) * 1.8);
                        context.DrawLine(new Pen(new SolidColorBrush(color), thickness), source.Nodes[sourceIndex], target.Nodes[targetIndex]);
                    }
                }
            }
        }

        private void DrawNodesAndHeaders(DrawingContext context, List<LayerVisual> layers, Matrix viewMatrix)
        {
            var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0x43, 0x4A, 0x59)), 0.9);
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                LayerVisual layer = layers[layerIndex];
                DrawText(context, $"{layer.Title} ({layer.Count})", new Point(layer.X, 29), 13, FontWeights.SemiBold, TextAlignment.Center);

                if (layerIndex > 0)
                {
                    int destinationIndex = layerIndex - 1;
                    bool locked = IsLocked(destinationIndex);
                    Rect iconRect = new Rect(layer.X - 10, 51, 20, 20);
                    DrawLock(context, iconRect, locked);
                    lockHits.Add(new LockHit
                    {
                        DestinationLayerIndex = destinationIndex,
                        Locked = locked,
                        Bounds = TransformRect(iconRect, viewMatrix)
                    });
                }
                else
                {
                    DrawText(context, "fixed input", new Point(layer.X, 61), 9, FontWeights.Normal, TextAlignment.Center);
                }

                for (int nodeIndex = 0; nodeIndex < layer.Nodes.Count; nodeIndex++)
                {
                    Point center = layer.Nodes[nodeIndex];
                    Rect bounds = new Rect(
                        center.X - layer.NodeRadius,
                        center.Y - layer.NodeRadius,
                        layer.NodeRadius * 2,
                        layer.NodeRadius * 2);
                    context.DrawEllipse(new SolidColorBrush(layer.Fill), borderPen, center, layer.NodeRadius, layer.NodeRadius);
                    nodeHits.Add(new NodeHit
                    {
                        Bounds = TransformRect(bounds, viewMatrix),
                        ToolTip = BuildNodeToolTip(layerIndex, nodeIndex, layer)
                    });
                }
            }
        }

        private void DrawLock(DrawingContext context, Rect rect, bool locked)
        {
            ImageSource image = locked ? lockedImage : unlockedImage;
            if (image != null)
            {
                context.DrawImage(image, rect);
                return;
            }

            Brush brush = new SolidColorBrush(locked ? Color.FromRgb(0xB5, 0x64, 0x24) : Color.FromRgb(0x43, 0x78, 0xA5));
            context.DrawRoundedRectangle(brush, null, new Rect(rect.X + 3, rect.Y + 8, 14, 10), 2, 2);
            var geometry = new StreamGeometry();
            using (StreamGeometryContext geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(new Point(rect.X + (locked ? 6 : 10), rect.Y + 9), false, false);
                geometryContext.BezierTo(
                    new Point(rect.X + (locked ? 6 : 10), rect.Y + 1),
                    new Point(rect.X + 14, rect.Y + 1),
                    new Point(rect.X + 14, rect.Y + 9),
                    true,
                    false);
            }
            context.DrawGeometry(null, new Pen(brush, 2), geometry);
        }

        private string BuildNodeToolTip(int layerIndex, int nodeIndex, LayerVisual layer)
        {
            if (layerIndex == 0)
            {
                double value = network.Inputs != null && nodeIndex < network.Inputs.Count
                    ? network.Inputs[nodeIndex].Axon?.Value ?? 0
                    : 0;
                return $"Input {nodeIndex + 1}\nValue: {value:0.###}";
            }

            INeuron neuron = layer.Layer?.NeuronsInLayer != null && nodeIndex < layer.Layer.NeuronsInLayer.Count
                ? layer.Layer.NeuronsInLayer[nodeIndex]
                : null;
            return $"{layer.Title} · neuron {nodeIndex + 1}\n" +
                   $"Output: {neuron?.Axon?.Value ?? 0:0.###}\n" +
                   $"Bias: {neuron?.Soma?.Bias ?? 0:0.###}";
        }

        private bool IsLocked(int destinationLayerIndex)
            => destinationLayerIndex >= 0
               && destinationLayerIndex < layerLocks.Count
               && layerLocks[destinationLayerIndex];

        private Matrix CreateViewMatrix()
        {
            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;
            return new Matrix(
                zoom,
                0,
                0,
                zoom,
                centerX * (1 - zoom) + pan.X,
                centerY * (1 - zoom) + pan.Y);
        }

        private static Rect TransformRect(Rect rect, Matrix matrix)
        {
            Point topLeft = matrix.Transform(rect.TopLeft);
            Point bottomRight = matrix.Transform(rect.BottomRight);
            return new Rect(topLeft, bottomRight);
        }

        private void DrawCenteredText(DrawingContext context, string text)
        {
            DrawText(
                context,
                text,
                new Point(ActualWidth / 2, ActualHeight / 2),
                16,
                FontWeights.Normal,
                TextAlignment.Center);
        }

        private void DrawText(
            DrawingContext context,
            string text,
            Point point,
            double fontSize,
            FontWeight weight,
            TextAlignment alignment)
        {
            var formatted = new FormattedText(
                text ?? "",
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
                fontSize,
                new SolidColorBrush(Color.FromRgb(0x2F, 0x36, 0x45)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            {
                TextAlignment = alignment
            };
            Point origin = alignment == TextAlignment.Center
                ? new Point(point.X - formatted.Width / 2, point.Y - formatted.Height / 2)
                : point;
            context.DrawText(formatted, origin);
        }

        private static ImageSource LoadImage(string uri)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(uri, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private sealed class LayerVisual
        {
            public string Title;
            public int Count;
            public ILayer Layer;
            public Color Fill;
            public double X;
            public double NodeRadius;
            public List<Point> Nodes { get; } = new List<Point>();
        }

        private sealed class LockHit
        {
            public int DestinationLayerIndex;
            public bool Locked;
            public Rect Bounds;
        }

        private sealed class NodeHit
        {
            public Rect Bounds;
            public string ToolTip;
        }
    }
}
