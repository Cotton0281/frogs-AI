using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AI_Evlo_Test.Objects
{
    /// <summary>
    /// Draws raycasting visualization for the selected agent on a WPF Canvas.
    /// Reuses pooled Line and Ellipse elements to avoid per-frame GC pressure.
    /// 
    /// Color legend:
    ///   Gray       = ray miss (reached max distance)
    ///   LimeGreen  = hit food/target
    ///   DodgerBlue = hit raft
    ///   Gold       = hit ally frog (same population)
    ///   OrangeRed  = hit other frog (different population)
    ///   White dot  = exact hit point
    /// </summary>
    public class RayVisualizer
    {
        private readonly Canvas _canvas;
        private readonly List<Line> _rayLines = new List<Line>();
        private readonly List<Ellipse> _hitDots = new List<Ellipse>();
        private bool _isVisible = true;

        private static readonly Brush BrushMiss = Brushes.Gray;
        private static readonly Brush BrushFood = Brushes.LimeGreen;
        private static readonly Brush BrushRaft = Brushes.DodgerBlue;
        private static readonly Brush BrushRaftSunk = Brushes.MediumPurple;
        private static readonly Brush BrushAlly = Brushes.Gold;
        private static readonly Brush BrushOther = Brushes.OrangeRed;
        private static readonly Brush BrushShark = Brushes.Crimson;
        private static readonly Brush BrushHitDot = Brushes.White;

        private const double RayOpacity = 0.5;
        private const double RayThickness = 1.0;
        private const double HitDotSize = 6.0;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                Visibility vis = value ? Visibility.Visible : Visibility.Collapsed;
                foreach (Line line in _rayLines) line.Visibility = vis;
                foreach (Ellipse dot in _hitDots) dot.Visibility = vis;
            }
        }

        public RayVisualizer(Canvas canvas, int rayCount)
        {
            _canvas = canvas;
            EnsureElements(rayCount);
        }

        /// <summary>
        /// Update the ray visuals from the selected agent's RayPerception.
        /// Call once per tick on the UI thread, after perception.Update().
        /// </summary>
        public void Draw(Point agentLocation, RayPerception perception)
        {
            if (!_isVisible || perception == null)
                return;

            EnsureElements(perception.RayCount);

            for (int r = 0; r < perception.RayCount; r++)
            {
                RayHit hit = perception.Hits[r];
                Line line = _rayLines[r];
                Ellipse dot = _hitDots[r];

                if (!hit.IsValid)
                {
                    line.Visibility = Visibility.Collapsed;
                    dot.Visibility = Visibility.Collapsed;
                    continue;
                }

                // Draw the ray line from agent to hit/max point
                line.X1 = agentLocation.X;
                line.Y1 = agentLocation.Y;
                line.X2 = hit.HitPoint.X;
                line.Y2 = hit.HitPoint.Y;
                line.Stroke = GetBrushForCategory(hit.Category);
                line.Visibility = Visibility.Visible;

                // Draw a dot only where something was actually hit
                if (hit.Category.HasValue)
                {
                    Canvas.SetLeft(dot, hit.HitPoint.X - HitDotSize / 2);
                    Canvas.SetTop(dot, hit.HitPoint.Y - HitDotSize / 2);
                    dot.Fill = GetBrushForCategory(hit.Category);
                    dot.Visibility = Visibility.Visible;
                }
                else
                {
                    dot.Visibility = Visibility.Collapsed;
                }
            }

            // Hide any extra pooled elements
            for (int r = perception.RayCount; r < _rayLines.Count; r++)
            {
                _rayLines[r].Visibility = Visibility.Collapsed;
                _hitDots[r].Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Remove all visual elements from the canvas.
        /// </summary>
        public void Clear()
        {
            foreach (Line line in _rayLines)
                _canvas.Children.Remove(line);
            foreach (Ellipse dot in _hitDots)
                _canvas.Children.Remove(dot);
            _rayLines.Clear();
            _hitDots.Clear();
        }

        private void EnsureElements(int count)
        {
            while (_rayLines.Count < count)
            {
                Line line = new Line
                {
                    StrokeThickness = RayThickness,
                    Opacity = RayOpacity,
                    IsHitTestVisible = false
                };
                _canvas.Children.Add(line);
                _rayLines.Add(line);

                Ellipse dot = new Ellipse
                {
                    Width = HitDotSize,
                    Height = HitDotSize,
                    Stroke = BrushHitDot,
                    StrokeThickness = 1,
                    IsHitTestVisible = false
                };
                _canvas.Children.Add(dot);
                _hitDots.Add(dot);
            }
        }

        private static Brush GetBrushForCategory(ObjectCategory? category)
        {
            if (!category.HasValue) return BrushMiss;
            switch (category.Value)
            {
                case ObjectCategory.Food: return BrushFood;
                case ObjectCategory.Raft: return BrushRaft;
                case ObjectCategory.Raft_Sunk: return BrushRaftSunk;
                case ObjectCategory.Frog: return BrushAlly;
                case ObjectCategory.Bird: return BrushOther;
                case ObjectCategory.Bird_Landed: return BrushOther;
                case ObjectCategory.Shark: return BrushShark;
                default: return BrushMiss;
            }
        }
    }
}
