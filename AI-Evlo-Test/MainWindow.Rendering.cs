using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using AI_Evlo_Test.Objects;
using AI_Evlo_Test.Enumerators;

namespace AI_Evlo_Test
{
    public partial class MainWindow
    {
        private readonly record struct AgentFrame(ISmartObject Identity, FrameworkElement Shape, Point Position,
            double Angle, double Opacity, bool Healing, bool Golden, bool Flash, ImageSource Sprite, Color Color);
        private readonly record struct TargetFrame(FrameworkElement Shape, Point Position, double Size, double Opacity, double RotationSpeed);
        private sealed record WorldFrame(AgentFrame[] Agents, TargetFrame[] Rafts, FrameworkElement[] Removed,
            EEnvironmentType Environment, Point? SelectedPosition, RayHit[,] Rays);
        private sealed record HudFrame(int Cycle, int Alive, double TopFitness, string Rafts,
            string SelectedTitle, string SelectedDetail, double Hp, int MaxHp,
            (PopulationCard Card, string Title, string Stats, string Tip, bool Selected)[] Cards);
        private sealed class RaftAnimation
        {
            public double Angle;
            public int Frame;
            public DateTime NextFrame;
        }
        private readonly Dictionary<FrameworkElement, RaftAnimation> raftAnimations = new();

        // Called on the UI thread under simLock. Only scalar values and frozen sprite references
        // leave the lock; the model identity is retained solely for attaching a newly created visual.
        private WorldFrame CaptureWorldFrame()
        {
            var agents = new AgentFrame[lsObjects.Count];
            for (int i = 0; i < agents.Length; i++)
            {
                var agent = (SmartObject)lsObjects[i];
                Color color = agent.VisibleShape == null
                    ? FindPopulationForObject(agent)?.PopulationColor ?? Colors.SteelBlue : default;
                agents[i] = new AgentFrame(agent, agent.VisibleShape, agent.Location,
                    Vector.AngleBetween(new Vector(0, -1), agent.FaceDirection),
                    Math.Clamp(2 * agent.HP / agent.EffectiveMaxHp, 0, 1), agent.IsGettingHP,
                    agent.IsGoldenAgent, agent.IsGoldenMergeFlashActive, agent.GetSpriteFrame(), color);
            }
            var rafts = Targets.Select(t => new TargetFrame(t.VisibleShape, t.Location, t.Size,
                t.Underwater >= 0 ? 0.6 : 0.3, t.RotationDegPerSec)).ToArray();
            var removed = _visualsToRemove.ToArray();
            _visualsToRemove.Clear();
            Point? selectedPosition = null;
            RayHit[,] rays = null;
            if (TryGetRenderableSelectedSmartObject(out var selected))
            {
                selectedPosition = selected.Location;
                rays = (RayHit[,])selected.Perception.HitLayers.Clone();
                for (int i = 0; i < selected.Perception.RayCount; i++)
                    if (!rays[i, 0].IsValid) rays[i, 0] = selected.Perception.Hits[i];
            }
            return new WorldFrame(agents, rafts, removed, eEnvironmentType, selectedPosition, rays);
        }

        private void PaintWorldFrame(WorldFrame frame)
        {
            foreach (var shape in frame.Removed)
            {
                shape.MouseDown -= ObjectInterface_MouseDown;
                shapeToObjectMap.Remove(shape);
                panlUniverseView.Children.Remove(shape);
            }
            foreach (var agent in frame.Agents)
            {
                var shape = agent.Shape;
                if (shape == null)
                {
                    shape = agent.Identity switch
                    {
                        Frog => CreateNewFrogImage(), Bird => CreateNewBirdImage(), Shark => CreateNewSharkImage(),
                        _ => CreateNewTrianglePolygon(new SolidColorBrush(agent.Color))
                    };
                    // Creation and event wiring are UI-only. Commit the reference only if still alive.
                    lock (simLock)
                    {
                        if (!lsObjects.Contains(agent.Identity))
                        {
                            panlUniverseView.Children.Remove(shape);
                            continue;
                        }
                        agent.Identity.VisibleShape = shape;
                    }
                    shape.MouseDown += ObjectInterface_MouseDown;
                    shapeToObjectMap[shape] = agent.Identity;
                    if (agent.Golden)
                    {
                        shape.ToolTip = "Golden agent";
                        shape.Effect = new System.Windows.Media.Effects.DropShadowEffect
                        { Color = Colors.Gold, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 };
                    }
                }
                shape.Opacity = agent.Opacity;
                if (shape.RenderTransform is RotateTransform rotation) rotation.Angle = agent.Angle;
                else shape.RenderTransform = new RotateTransform(agent.Angle);
                if (shape is Image image && agent.Sprite != null)
                    image.Source = agent.Golden
                        ? agent.Flash ? GoldenTintCache.GetRedTinted(agent.Sprite) : GoldenTintCache.GetTinted(agent.Sprite)
                        : agent.Sprite;
                else if (agent.Golden && shape is Shape goldenShape) goldenShape.Fill = agent.Flash ? Brushes.Red : Brushes.Gold;
                if (shape is Polygon polygon && frame.Environment == EEnvironmentType.TwoTargets)
                    polygon.Stroke = agent.Healing ? Brushes.GreenYellow : Brushes.OrangeRed;
                DrawImage(shape, agent.Position);
            }
            DateTime now = DateTime.Now;
            double elapsed = (now - _lastRaftVisualUpdate).TotalSeconds;
            _lastRaftVisualUpdate = now;
            if (elapsed < 0 || elapsed > 1) elapsed = 0;
            foreach (var raft in frame.Rafts)
            {
                if (raft.Shape == null) continue;
                raft.Shape.Opacity = raft.Opacity;
                if (frame.Environment == EEnvironmentType.OneTarget)
                {
                    raft.Shape.Width = raft.Shape.Height = raft.Size;
                }
                else if (raft.Shape is Image image)
                {
                    if (!raftAnimations.TryGetValue(raft.Shape, out var animation))
                        raftAnimations.Add(raft.Shape, animation = new RaftAnimation());
                    animation.Angle = (animation.Angle + raft.RotationSpeed * elapsed) % 360;
                    if (image.RenderTransform is RotateTransform rotation) rotation.Angle = animation.Angle;
                    else image.RenderTransform = new RotateTransform(animation.Angle);
                    if (now >= animation.NextFrame)
                    {
                        image.Source = RaftSheetCache.Frame(++animation.Frame);
                        animation.NextFrame = now.AddSeconds(0.75);
                    }
                }
                DrawImage(raft.Shape, raft.Position);
            }
            foreach (var shape in raftAnimations.Keys.Where(s => !frame.Rafts.Any(r => ReferenceEquals(r.Shape, s))).ToArray())
                raftAnimations.Remove(shape);
            if (frame.SelectedPosition.HasValue && frame.Rays != null)
                rayVisualizer?.DrawSnapshot(frame.SelectedPosition.Value, frame.Rays);
            else rayVisualizer?.Hide();
            if (frame.Environment == EEnvironmentType.OneTarget && frame.Rafts.Length > 0)
                drawLine(frame.SelectedPosition ?? frame.Rafts[0].Position, frame.Rafts[0].Position);
        }

        private HudFrame CaptureHudFrame()
        {
            int alive = 0;
            double top = 0;
            foreach (var agent in lsObjects)
            {
                if (agent is SmartObject smart && smart.IsGoldenAgent) continue;
                alive++; top = Math.Max(top, agent.Fitness);
            }
            string title = "No agent selected", detail = "Click an agent to inspect it";
            double hp = 0; int maxHp = 1;
            if (TryGetRenderableSelectedSmartObject(out var selected))
            {
                title = $"{GetObjectKindName(selected)} {selected.ID}";
                detail = $"Gen {selected.Generation} · lived {selected.Cycles} cycles";
                if (selected is Bird bird) detail += $" · ate {bird.SharksEaten}";
                if (selected is Shark shark) detail += $" · ate {shark.FrogsEaten}";
                var population = FindPopulationForObject(selected);
                if (selected.IsGoldenAgent && population != null)
                    detail += $" · {population.GoldenAveragedNetworkCount} golden merges · threshold {Math.Ceiling(population.GoldenThreshold)}";
                hp = selected.HP; maxHp = selected.EffectiveMaxHp;
            }
            var cards = new List<(PopulationCard, string, string, string, bool)>();
            for (int i = 0; i < lsPopulations.Count && i < lsPopuCards.Count; i++)
            {
                var population = lsPopulations[i];
                int living = population.Members.Count(m => m != null && m.HP > 0);
                double mean = population.Members.Count == 0 ? 0 : population.Members.Average(m => m.Fitness);
                string stats = $"{living}/{population.SizeLimit} alive · mean fitness {mean:0}\n" +
                    (population.GoldenAgentEnabled ? $"Golden: {population.GoldenAveragedNetworkCount} merges · threshold {Math.Ceiling(population.GoldenThreshold)}" : "Golden agent off");
                cards.Add((lsPopuCards[i], $"{population.Name} · {GetPopulationBeingName(population.Being)}", stats,
                    $"{population.TotalMembersCount - living} agents lost. Golden threshold is the survival age needed to contribute a brain.",
                    ReferenceEquals(population, SelectedPopulation)));
            }
            string rafts = string.Join("", Targets.Select((t, i) => $" · Raft {i + 1}: {t.ObjectsOnTop} aboard"));
            return new HudFrame(CycleCount, alive, top, rafts, title, detail, hp, maxHp, cards.ToArray());
        }

        private void PaintHudFrame(HudFrame frame)
        {
            DateTime now = DateTime.Now;
            double elapsed = (now - lastCpsCheckTime).TotalSeconds;
            if (elapsed >= 1)
            {
                cyclesPerSecond = (frame.Cycle - lastCpsCheckCycle) / elapsed;
                lastCpsCheckCycle = frame.Cycle; lastCpsCheckTime = now;
            }
            string status = $"{(simulationRunning ? "Running" : "Paused")}{(isHeadlessMode ? " · Headless" : "")} · Cycle {frame.Cycle} · {cyclesPerSecond:0.0} cycles/s · {frame.Alive} alive{frame.Rafts}";
            lblStatusBar.Content = status; lblStatusBar.ToolTip = status;
            lblSelectedTitle.Text = frame.SelectedTitle; lblSelectedSub.Text = frame.SelectedDetail;
            double percent = Math.Clamp(100 * frame.Hp / frame.MaxHp, 0, 100);
            pbSelectedHP.Value = percent; pbSelectedHP.Foreground = HpBrush(percent);
            lblSelectedHP.Text = frame.Hp > 0 ? $"{frame.Hp:0}/{frame.MaxHp}" : "";
            foreach (var card in frame.Cards)
            {
                card.Card.Title.Text = card.Title; card.Card.Stats.Text = card.Stats; card.Card.Root.ToolTip = card.Tip;
                if (card.Selected) { lblPopulationInfo.Content = card.Stats; lblPopulationInfo.ToolTip = card.Tip; }
            }
            PushSparkSample(_sparkAlive, frame.Alive); PushSparkSample(_sparkFitness, frame.TopFitness);
            BuildSparkline(sparkAlive, _sparkAlive); BuildSparkline(sparkFitness, _sparkFitness);
        }
    }
}
