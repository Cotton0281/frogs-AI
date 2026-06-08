using AI_Evlo_Test.Enumerators;
using AI_Evlo_Test.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AI_Evlo_Test
{
    // Simulation logic: tick loop, agent movement, target movement, perception snapshot.
    public partial class MainWindow
    {
        private void SimulationTick()
        {
            //Target move
            MoveTarget();

            if (lsObjects != null && lsObjects.Count > 0)
            {
                if (eEnvironmentType == EEnvironmentType.OneTarget)
                    MoveAgentsEnvirnoment1();
                else if (eEnvironmentType == EEnvironmentType.TwoTargets)
                    MoveAgentsEnvirnoment2();
            }


            // Remove dead agents, then gradually regrow depleted populations one member at a time.
            UpdateGoldenAveragesForLiveSurvivors();
            int totalObj = lsObjects?.Count ?? 0;

            // remove the unsuccessful by looping backwards
            for (int i = totalObj - 1; i >= 0; i--)
            {
                if (lsObjects[i].HP <= 0)
                    DisposeObject(lsObjects[i]);
            }
            // Reselection of a dead inspected agent happens on the UI thread (OnRendering).

            // Remove from populations
            foreach (Population Popul in lsPopulations)
            {
                Popul.LifeCycles++;
                Popul.Members.RemoveAll(o => o.HP <= 0);
                if (Popul.Members.Count < Popul.SizeLimit)
                    ReGrowPopulation(Popul);

                // Time-series sampling is a no-op (single null check) unless a dashboard is open.
                Popul.Stats?.SampleIfDue(Popul, CycleCount);
            }

            CycleCount++;
        }

        private void UpdateGoldenAveragesForLiveSurvivors()
        {
            foreach (Population population in lsPopulations)
            {
                for (int i = 0; i < population.Members.Count; i++)
                {
                    ISmartObject member = population.Members[i];
                    if (ShouldAttemptGoldenAverage(population, member))
                        TryUpdateGoldenAverage(population, member);
                }
            }
        }

        internal static bool ShouldAttemptGoldenAverage(Population population, ISmartObject member)
        {
            return member != null
                && population != null
                && population.ShouldCheckGoldenAverage(member);
        }

        private void InitTargets()
        {
            // save the existing location
            Point targLocation = new Point(100, 100);
            if (Targets.Count > 0 && !Targets[0].Location.ToString().Contains("N"))
                targLocation = Targets[0].Location;


            // remove existing targets
            Targets.ForEach(o => panlUniverseView.Children.Remove(o.VisibleShape)); // clear targets
            Targets.ForEach(o => o.VisibleShape = null); // clear targets
            Targets.ForEach(o => o = null); // clear targets
            Targets.Clear();

            // create new targets
            int intTargets = eEnvironmentType == EEnvironmentType.OneTarget ? 1 : 2;
            for (int t = 1; t <= intTargets; t++)
            {
                Targets.Add(new TargetObj()
                { ID = ObjectsIdCounter++.ToString(), Size = dblTargetSize });
            }

            if (targLocation.X == 0 && targLocation.Y == 0)
            {
                targLocation = new Point(panlUniverseView.ActualWidth / 2, panlUniverseView.ActualHeight / 2);
            }
            //create target
            foreach (TargetObj target in Targets)
            {
                target.SetLocation(targLocation);
                if (eEnvironmentType == EEnvironmentType.OneTarget)
                {
                    Ellipse TargetEllipse = (Ellipse)CreateNewShape(Brushes.Aquamarine);
                    TargetEllipse.Height = target.Size;
                    TargetEllipse.Width = target.Size;
                    TargetEllipse.StrokeThickness = 1;
                    TargetEllipse.Stroke = Brushes.White;
                    //TargetEllipse.Fill = Brushes.Aquamarine;
                    TargetEllipse.Opacity = 0.2;
                    target.VisibleShape = TargetEllipse;
                    DrawImage(target.VisibleShape, target.Location);
                }
                else if (eEnvironmentType == EEnvironmentType.TwoTargets)
                {
                    target.VisibleShape = CreateRaftImage();
                }
                targLocation.X += target.VisibleShape.Width;
                target.Intertia.X = NextRandomDouble() / 4 + 0.1;
                target.Intertia.Y = NextRandomDouble() / 4 + 0.1;
                target.RotationDegPerSec = RandomRotationSpeed();
                target.NextSpriteChangeTime = DateTime.Now.AddSeconds(NextRandomDouble());
            }

            Targets[0].Trajectory = new Path_spiral()
            {
                ClockwiseDirection = true,
                SpiralingAngle = -3,
                Speed = 0.3,
                SpiralCenter = new Point(panlUniverseView.ActualWidth / 2, panlUniverseView.ActualHeight / 2)
            };
        }

        private void MoveAgentsEnvirnoment1()
        {
            IReadOnlyList<SensableSnapshot> snapshot = BuildSensableSnapshot();
            double targetRadius = Target.Size / 2;

            Parallel.ForEach(lsObjects, smartObject =>
            {
                SmartObject smart = smartObject as SmartObject;

                // Each species declares what it can and cannot perceive
                smart.Perception.Update(smart.Location, smart.FaceDirection, snapshot, smart.ID, smart.IgnoredCategories);

                // Build NN inputs: HP deficit + recurrent memory + ray signals.
                double hpDeficit = 1.0 - (smartObject.HP / smart.EffectiveMaxHp);
                smart.Perception.FillInputs(smart.CachedInputs, hpDeficit, smart.Memory);

                smartObject.Act(smart.CachedInputs);

                // HP logic
                Vector toTarget = Point.Subtract(Target.Location, smartObject.Location);
                double distToTargetSq = toTarget.LengthSquared;
                smartObject.IsGettingHP = distToTargetSq <= targetRadius * targetRadius;

                if (smartObject.IsGettingHP)
                    smartObject.HP += 1;
                else
                    smartObject.HP -= 1;
            });
        }

        private void MoveAgentsEnvirnoment2()
        {
            CarryAgentsOnRafts();

            IReadOnlyList<SensableSnapshot> snapshot = BuildSensableSnapshot();

            Parallel.ForEach(lsObjects, smartObject =>
            {
                SmartObject smart = smartObject as SmartObject;

                // Each species declares what it can and cannot perceive
                smart.Perception.Update(smart.Location, smart.FaceDirection, snapshot, smart.ID, smart.IgnoredCategories);

                // Build NN inputs: HP deficit + recurrent memory + ray signals.
                double hpDeficit = 1.0 - (smartObject.HP / smart.EffectiveMaxHp);
                smart.Perception.FillInputs(smart.CachedInputs, hpDeficit, smart.Memory);

                smartObject.Act(smart.CachedInputs);
            });

            ApplyRaftEnvironmentEffects();
        }

        // ---- Rendering (UI thread only) ----------------------------------------------
        // Reads model state and updates WPF visuals. Kept separate from the model step so
        // the simulation can run without touching the UI.

        private void RenderWorld()
        {
            ReconcileVisuals();
            RenderTargets();
            if (eEnvironmentType == EEnvironmentType.OneTarget)
                RenderAgentsEnv1();
            else
                RenderAgentsEnv2();
        }

        private void RenderTargets()
        {
            foreach (TargetObj target in Targets)
            {
                if (target.VisibleShape == null)
                    continue;

                if (eEnvironmentType == EEnvironmentType.OneTarget)
                {
                    target.VisibleShape.Width = target.Size;
                    target.VisibleShape.Height = target.Size;
                }

                target.VisibleShape.Opacity = target.Underwater >= 0 ? 0.6 : 0.3;
                DrawImage(target.VisibleShape, target.Location);
            }
        }

        private void RenderAgentsEnv1()
        {
            RenderSelectedAgentRays();

            foreach (ISmartObject smartObject in lsObjects)
            {
                if (smartObject.VisibleShape == null)
                    continue;

                SmartObject smart = (SmartObject)smartObject;
                smartObject.VisibleShape.Opacity = (2 * smartObject.HP / smart.EffectiveMaxHp);
                double anglFromVertical = Vector.AngleBetween(new Vector(0, -1), smart.FaceDirection);
                // Reuse the existing transform instead of allocating one per agent per frame.
                if (smartObject.VisibleShape.RenderTransform is RotateTransform rotate)
                    rotate.Angle = anglFromVertical;
                else
                    smartObject.VisibleShape.RenderTransform = new RotateTransform(anglFromVertical);

                // Animate species sprite
                if (smartObject.VisibleShape is Image img)
                {
                    ImageSource frame = smart.GetSpriteFrame();
                    if (frame != null)
                        img.Source = GetAgentFrameForRender(smart, frame);
                }
                else if (smart.IsGoldenAgent && smartObject.VisibleShape is Shape shape)
                {
                    shape.Fill = smart.IsGoldenMergeFlashActive ? Brushes.Red : Brushes.Gold;
                }

                DrawImage(smartObject.VisibleShape, smartObject.Location);
            }
        }

        private void RenderAgentsEnv2()
        {
            RenderSelectedAgentRays();

            foreach (ISmartObject smartObject in lsObjects)
            {
                if (smartObject.VisibleShape == null)
                    continue;

                SmartObject smart = (SmartObject)smartObject;
                smartObject.VisibleShape.Opacity = (2 * smartObject.HP / smart.EffectiveMaxHp);
                if (smartObject.VisibleShape is Polygon polygon)
                    polygon.Stroke = smartObject.IsGettingHP ? Brushes.GreenYellow : Brushes.OrangeRed;

                double anglFromVertical = Vector.AngleBetween(new Vector(0, -1), smart.FaceDirection);
                // Reuse the existing transform instead of allocating one per agent per frame.
                if (smartObject.VisibleShape.RenderTransform is RotateTransform rotate)
                    rotate.Angle = anglFromVertical;
                else
                    smartObject.VisibleShape.RenderTransform = new RotateTransform(anglFromVertical);

                // Animate species sprite
                if (smartObject.VisibleShape is Image img)
                {
                    ImageSource frame = smart.GetSpriteFrame();
                    if (frame != null)
                        img.Source = GetAgentFrameForRender(smart, frame);
                }
                else if (smart.IsGoldenAgent && smartObject.VisibleShape is Shape shape)
                {
                    shape.Fill = smart.IsGoldenMergeFlashActive ? Brushes.Red : Brushes.Gold;
                }

                DrawImage(smartObject.VisibleShape, smartObject.Location);
            }
        }

        private void RenderSelectedAgentRays()
        {
            if (TryGetRenderableSelectedSmartObject(out SmartObject selected))
            {
                rayVisualizer.Draw(selected.Location, selected.Perception);
                return;
            }

            rayVisualizer?.Hide();
        }

        private static ImageSource GetAgentFrameForRender(SmartObject smart, ImageSource frame)
        {
            if (!smart.IsGoldenAgent)
                return frame;

            return smart.IsGoldenMergeFlashActive
                ? GoldenTintCache.GetRedTinted(frame)
                : GoldenTintCache.GetTinted(frame);
        }

        private bool TryGetRenderableSelectedSmartObject(out SmartObject selected)
        {
            selected = null;

            if (!(SelectedObject is SmartObject smart))
                return false;

            if (smart.HP <= 0 || smart.NNetwork == null || smart.Perception == null)
                return false;

            if (!lsObjects.Contains(smart))
                return false;

            selected = smart;
            return true;
        }

        private void MoveTarget()
        {
            foreach (TargetObj target in Targets)
                target.BeginMovementTick();

            foreach (TargetObj target in Targets)
            {
                // Bounce of edges of screen
                if (target.Location.X < (dblTargetSize / 2)
                    || target.Location.X > canvasWidth)
                {
                    target.Intertia.X = NextRandomDouble() / 4 + 0.1;
                    if (target.Location.X > canvasWidth)
                        target.Intertia.X *= -1; // reverse direction
                    target.RotationDegPerSec = RandomRotationSpeed(); // bump → new spin speed
                }

                if (target.Location.Y < (dblTargetSize / 2)
                    || target.Location.Y > canvasHeight)
                {
                    target.Intertia.Y = NextRandomDouble() / 4 + 0.1;
                    if (target.Location.Y > canvasHeight)
                        target.Intertia.Y *= -1;
                    target.RotationDegPerSec = RandomRotationSpeed(); // bump → new spin speed
                }

                // Resize target , but keep between full size and 75%
                if (eEnvironmentType == EEnvironmentType.OneTarget)
                {
                    target.Size = target.Size + NextRandomDouble() * 2 - 1;
                    if (target.Size > dblTargetSize)
                        target.Size = dblTargetSize;
                    else if (target.Size < dblTargetSize * 0.75)
                        target.Size = dblTargetSize * 0.75;
                }


                if (ShouldRaftSink(target.FrogsOnTop, lsPopulations))
                    target.Underwater--;
                else
                    target.Underwater++;

                target.HpCharge = target.Underwater >= 0 ? 1 : 0;

                if (StopTargets == false)
                {
                    if (target.Trajectory is Path_spiral)
                    {
                        Point newLocation = target.Trajectory.GetNextLocation(target.Location);
                        target.SetLocation(newLocation);
                    }
                    else
                    {
                        target.SetLocation(target.Location.X + target.Intertia.X, target.Location.Y + target.Intertia.Y);
                    }
                }
            }

            // Resolve billiard-ball bounces between all targets (rafts)
            if (Targets.Count > 1)
            {
                for (int i = 0; i < Targets.Count; i++)
                    for (int j = i + 1; j < Targets.Count; j++)
                    {
                        if (Targets[i].IsCollidingWith(Targets[j]))
                        {
                            // bump → each raft picks a new spin speed
                            Targets[i].RotationDegPerSec = RandomRotationSpeed();
                            Targets[j].RotationDegPerSec = RandomRotationSpeed();
                        }
                        BasicObject.ResolveElasticBounce(Targets[i], Targets[j]);
                    }
            }

            foreach (TargetObj target in Targets)
                target.CompleteMovementTick();
        }

        /// <summary>
        /// Builds a value snapshot list of all sensable objects for raycasting.
        /// Call once per tick before the Parallel.ForEach.
        /// The returned list and its object state are read-only for the duration of that tick.
        /// </summary>
        private IReadOnlyList<SensableSnapshot> BuildSensableSnapshot()
        {
            _sensableSnapshot.Clear();

            // Targets
            foreach (TargetObj target in Targets)
            {
                if (eEnvironmentType == EEnvironmentType.OneTarget)
                    target.Category = ObjectCategory.Food;
                else
                    target.Category = target.Underwater >= 0 ? ObjectCategory.Raft : ObjectCategory.Raft_Sunk;
                _sensableSnapshot.Add(new SensableSnapshot(target.ID, target.Location, target.Size, target.Category));
            }

            // Agents — each species reports its own perception category
            foreach (Population pop in lsPopulations)
            {
                foreach (ISmartObject member in pop.Members)
                {
                    if (member is SmartObject smart)
                    {
                        smart.Category = smart is Frog && IsOnAnyRaft(smart)
                            ? ObjectCategory.Frog_OnRaft
                            : smart.SenseCategory;
                        _sensableSnapshot.Add(new SensableSnapshot(smart.ID, smart.Location, smart.Size, smart.Category));
                    }
                }

                if (pop.GoldenAgent is SmartObject goldenSmart)
                {
                    goldenSmart.Category = goldenSmart is Frog && IsOnAnyRaft(goldenSmart)
                        ? ObjectCategory.Frog_OnRaft
                        : goldenSmart.SenseCategory;
                    _sensableSnapshot.Add(new SensableSnapshot(goldenSmart.ID, goldenSmart.Location, goldenSmart.Size, goldenSmart.Category));
                }
            }

            return _sensableSnapshot;
        }

        private bool IsOnAnyRaft(SmartObject smart)
        {
            if (eEnvironmentType != EEnvironmentType.TwoTargets)
                return false;

            foreach (TargetObj raft in Targets)
            {
                double raftRadius = raft.Size / 2D;
                Vector toRaft = Point.Subtract(raft.Location, smart.Location);
                if (toRaft.LengthSquared <= raftRadius * raftRadius)
                    return true;
            }

            return false;
        }

        private void CarryAgentsOnRafts()
        {
            if (eEnvironmentType != EEnvironmentType.TwoTargets)
                return;

            foreach (ISmartObject smartObject in lsObjects)
                if (smartObject is SmartObject smart)
                    ApplyRaftCarryToAgent(smart, Targets);
        }

        internal static bool ApplyRaftCarryToAgent(SmartObject smart, IEnumerable<TargetObj> rafts)
        {
            if (!(smart is Frog) && !(smart is Bird))
                return false;

            foreach (TargetObj raft in rafts)
            {
                double raftRadius = raft.Size / 2D;
                Vector toPreviousRaftLocation = Point.Subtract(raft.PreviousLocation, smart.Location);
                if (toPreviousRaftLocation.LengthSquared <= raftRadius * raftRadius)
                {
                    smart.MoveTo(raft.MovementDelta);
                    return true;
                }
            }

            return false;
        }

        internal static bool ShouldRaftSink(int frogsOnTop, IEnumerable<Population> populations)
        {
            const int MinimumFrogsOnRaftToSink = 15;
            if (frogsOnTop < MinimumFrogsOnRaftToSink)
                return false;

            int spawnedFrogs = populations
                .Where(p => p.Being == PopulationBeing.Frog)
                .Sum(p => p.Members?.Count(m => m != null && m.HP > 0) ?? 0);

            if (spawnedFrogs <= 0)
                return false;

            int threshold = Math.Max(
                MinimumFrogsOnRaftToSink,
                (int)Math.Ceiling(spawnedFrogs * 0.5));
            return frogsOnTop >= threshold;
        }

        private ISmartObject GetTopFitnessObject()
        {
            ISmartObject top = null;
            for (int i = 0; i < lsObjects.Count; i++)
            {
                ISmartObject candidate = lsObjects[i];
                if (top == null || candidate.Fitness > top.Fitness)
                    top = candidate;
            }

            return top;
        }

        readonly List<PopulationCard> lsPopuCards = new List<PopulationCard>();
        readonly List<SensableSnapshot> _sensableSnapshot = new List<SensableSnapshot>();

        // Rolling history for the status-bar sparkline.
        private const int SparkSamples = 120;
        private readonly List<double> _sparkAlive = new List<double>();
        private readonly List<double> _sparkFitness = new List<double>();

        private DateTime _lastRaftVisualUpdate = DateTime.Now;

        /// <summary>Returns a random raft rotation speed in degrees/second within ±5.</summary>
        private static double RandomRotationSpeed() => NextRandomDouble() * 10.0 - 5.0;

        /// <summary>
        /// Updates raft visuals in real time (UI-only): a slow ±5°/s rotation, and a sprite-frame
        /// swap on a random 0.5–1.0 s cadence so the raft bobs gently rather than shaking.
        /// </summary>
        private void UpdateRaftAnimation()
        {
            if (eEnvironmentType != EEnvironmentType.TwoTargets)
                return;

            DateTime now = DateTime.Now;
            double dt = (now - _lastRaftVisualUpdate).TotalSeconds;
            _lastRaftVisualUpdate = now;
            if (dt <= 0 || dt > 1.0)
                dt = 0; // first tick or resumed after a pause — don't jump the rotation

            foreach (TargetObj raft in Targets)
            {
                if (!(raft.VisibleShape is Image raftImage))
                    continue;

                // Slow continuous rotation
                raft.RotationAngle += raft.RotationDegPerSec * dt;
                if (raft.RotationAngle > 360) raft.RotationAngle -= 360;
                else if (raft.RotationAngle < -360) raft.RotationAngle += 360;
                if (raftImage.RenderTransform is RotateTransform rt)
                    rt.Angle = raft.RotationAngle;
                else
                    raftImage.RenderTransform = new RotateTransform(raft.RotationAngle);

                // Sprite frame swap on a random real-time interval
                if (RaftSheetCache.FrameCount > 1 && now >= raft.NextSpriteChangeTime)
                {
                    raft.SpriteFrameIndex++;
                    raftImage.Source = RaftSheetCache.Frame(raft.SpriteFrameIndex);
                    raft.NextSpriteChangeTime = now.AddSeconds(0.5 + NextRandomDouble() * 0.5);
                }
            }
        }

        private static readonly SolidColorBrush HpGreen = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly SolidColorBrush HpOrange = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
        private static readonly SolidColorBrush HpRed = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));

        private static Brush HpBrush(double pct) => pct > 50 ? HpGreen : (pct > 20 ? HpOrange : HpRed);

        private static ImageSource SpeciesIcon(ISmartObject o)
        {
            if (o is Bird) return BirdSheetCache.Frame(0);
            if (o is Shark) return SharkSpriteCache.Frame(0);
            return FrogSheetCache.Frame(0);
        }

        private Population FindPopulationForObject(ISmartObject smartObject)
        {
            return lsPopulations.FirstOrDefault(p =>
                ReferenceEquals(p.GoldenAgent, smartObject) ||
                (p.Members != null && p.Members.Contains(smartObject)));
        }

        /// <summary>Updates the live HP bar and title text for the selected agent.</summary>
        private void UpdateSelectedAgentStats()
        {
            if (SelectedObject == null || SelectedObject.HP <= 0 || !(SelectedObject is SmartObject smart))
            {
                lblSelectedTitle.Text = "No agent selected";
                lblSelectedSub.Text = "click an agent to inspect it";
                pbSelectedHP.Value = 0;
                lblSelectedHP.Text = "";
                return;
            }

            int maxHp = smart.EffectiveMaxHp;
            double hpPct = maxHp > 0 ? SelectedObject.HP / maxHp * 100.0 : 0;
            hpPct = Math.Max(0, Math.Min(100, hpPct));

            lblSelectedTitle.Text = $"{GetObjectKindName(SelectedObject)} {SelectedObject.ID}";
            string sub = $"Gen {SelectedObject.Generation} · lived {SelectedObject.Cycles} cycles";
            if (SelectedObject is Bird b) sub += $" · ate {b.SharksEaten}";
            else if (SelectedObject is Shark s) sub += $" · ate {s.FrogsEaten}";
            Population selectedPopulation = FindPopulationForObject(SelectedObject);
            if (smart.IsGoldenAgent && selectedPopulation != null)
            {
                sub += $" · golden averages {selectedPopulation.GoldenAveragedNetworkCount}";
                sub += $" · GoldenThreshold {(int)Math.Ceiling(selectedPopulation.GoldenThreshold)}";
            }
            lblSelectedSub.Text = sub;

            pbSelectedHP.Value = hpPct;
            pbSelectedHP.Foreground = HpBrush(hpPct);
            lblSelectedHP.Text = $"{(int)SelectedObject.HP}/{maxHp}";
        }

        /// <summary>Sets the species icon and rebuilds the brain (neural-net) bar viz. Call on selection change.</summary>
        private void UpdateSelectedAgentVisual()
        {
            if (SelectedObject == null)
            {
                imgSelectedSpecies.Source = null;
                pnlBrainLayers.Children.Clear();
                lblBrainInfo.Text = "";
                return;
            }

            imgSelectedSpecies.Source = SpeciesIcon(SelectedObject);

            pnlBrainLayers.Children.Clear();
            var net = SelectedObject.NNetwork;
            if (net?.HiddenLayers == null || net.HiddenLayers.Count == 0)
            {
                lblBrainInfo.Text = "";
                return;
            }

            int maxN = 1;
            foreach (var layer in net.HiddenLayers)
                if (layer.NeuronsInLayer.Count > maxN) maxN = layer.NeuronsInLayer.Count;

            Population pop = FindPopulationForObject(SelectedObject);
            Brush barBrush = pop != null ? pop.PopulationColorBrush : Brushes.MediumPurple;

            foreach (var layer in net.HiddenLayers)
            {
                double h = 4 + 13.0 * layer.NeuronsInLayer.Count / maxN;
                pnlBrainLayers.Children.Add(new Rectangle
                {
                    Width = 5,
                    Height = h,
                    Fill = barBrush,
                    RadiusX = 1,
                    RadiusY = 1,
                    Margin = new Thickness(1, 0, 1, 0),
                    VerticalAlignment = VerticalAlignment.Bottom
                });
            }

            int firstN = net.HiddenLayers[0].NeuronsInLayer.Count;
            lblBrainInfo.Text = $"{net.HiddenLayers.Count} layers × {firstN}";
        }

        private void UpdateLabbels()
        {
            if (DateTime.Now.Subtract(dtLastLabelsUpdate) < new TimeSpan(0, 0, 0, 0, 200))
                return;
            dtLastLabelsUpdate = DateTime.Now;

            // Selected agent live stats (icon + brain are set on selection in UpdateSelectedAgentVisual)
            UpdateSelectedAgentStats();

            UpdateSparkline();

            // Status bar
            UpdateStatusBar();

            // Population info
            for (int i = 0; i < lsPopulations.Count; i++)
            {
                Population pop = lsPopulations[i];
                if (pop.LifeCycles == 0 || pop.Members.Count == 0)
                    continue;

                int liveMembers = 0;
                double totalFitness = 0;

                for (int memberIndex = 0; memberIndex < pop.Members.Count; memberIndex++)
                {
                    ISmartObject member = pop.Members[memberIndex];
                    if (member.Fitness > 0)
                        liveMembers++;

                    totalFitness += member.Fitness;
                }

                int lostMembers = pop.TotalMembersCount - liveMembers;
                int avgFitness = (int)(totalFitness / pop.Members.Count);

                if (i >= lsPopuCards.Count)
                    continue;
                PopulationCard card = lsPopuCards[i];

                card.Title.Text = $"{pop.SizeLimit} {GetPopulationBeingName(pop.Being)}";

                string golden = pop.GoldenAgentEnabled
                    ? $"  · {pop.GoldenAveragedNetworkCount} merged to golden | T{(int)Math.Ceiling(pop.GoldenThreshold)}"
                    : "  ·  golden off";
                card.Stats.Text =
                    $"{liveMembers} alive / {lostMembers}  + avg fitness { avgFitness}" + Environment.NewLine +
                    $"{golden}";

                string tip =
                    $"Population '{pop.Name}' ({GetPopulationBeingName(pop.Being)})" + Environment.NewLine +
                    $"Live agents: {liveMembers}  |  Lost agents: {lostMembers}" + Environment.NewLine +
                    $"Average fitness: {avgFitness}" + Environment.NewLine +
                    $"Golden agent: {(pop.GoldenAgentEnabled ? "enabled" : "disabled")} | Merged : {pop.GoldenAveragedNetworkCount} | GoldenThreshold: {(int)Math.Ceiling(pop.GoldenThreshold)}" + Environment.NewLine +
                    $"T = an agent must survive {(int)Math.Ceiling(pop.GoldenThreshold)} cycles before it can merge its brain into the golden agent.";
                card.Root.ToolTip = tip;

                if (_selectedPopulation == pop)
                {
                    lblPopulationInfo.Content = $"{pop.Name} ({GetPopulationBeingName(pop.Being)})  {card.Stats.Text}";
                    lblPopulationInfo.ToolTip = tip;
                }
            }
        }

        /// <summary>
        /// Recomputes cycles/second and refreshes the status-bar text (cycle count, throughput,
        /// live agent count, target occupancy). Cheap enough to run in headless mode, where it is
        /// the only UI work done — it must be called on the UI thread while holding simLock.
        /// </summary>
        private void UpdateStatusBar()
        {
            // Cycles per second (measured over ~1 second windows)
            DateTime now = DateTime.Now;
            double elapsedSeconds = (now - lastCpsCheckTime).TotalSeconds;
            if (elapsedSeconds >= 1.0)
            {
                int cyclesDelta = CycleCount - lastCpsCheckCycle;
                cyclesPerSecond = cyclesDelta / elapsedSeconds;
                lastCpsCheckCycle = CycleCount;
                lastCpsCheckTime = now;
            }

            string statusText = isHeadlessMode ? "[HEADLESS] " : "";
            statusText += $"Cycle {CycleCount}  |  Cycles/s: {cyclesPerSecond:F2}";
            if (Targets.Count > 0)
            {
                int onTarget = Targets[0].ObjectsOnTop;
                int alive = lsObjects.Count(o => !(o is SmartObject smart && smart.IsGoldenAgent));
                statusText +=
                    $"  |  Agents alive: {alive}  |  Target 1: {onTarget} on top, depth {Targets[0].Underwater:F0}";
            }
            if (Targets.Count > 1)
            {
                statusText +=
                    $"  |  Target 2: {Targets[1].ObjectsOnTop} on top, depth {Targets[1].Underwater:F0}";
            }
            lblStatusBar.Content = statusText;
            lblStatusBar.ToolTip = statusText;
        }

        /// <summary>
        /// Pushes the latest "agents alive" and "top fitness" samples into the rolling
        /// history and redraws the two status-bar sparklines. Each series auto-scales to
        /// its own running maximum so both stay visible regardless of magnitude.
        /// </summary>
        private void UpdateSparkline()
        {
            if (sparkCanvas == null)
                return;

            int aliveCount = 0;
            double topFitness = 0;
            for (int i = 0; i < lsObjects.Count; i++)
            {
                ISmartObject o = lsObjects[i];
                if (o is SmartObject s && s.IsGoldenAgent)
                    continue;

                aliveCount++;
                if (o.Fitness > topFitness)
                    topFitness = o.Fitness;
            }

            PushSparkSample(_sparkAlive, aliveCount);
            PushSparkSample(_sparkFitness, topFitness);

            BuildSparkline(sparkAlive, _sparkAlive);
            BuildSparkline(sparkFitness, _sparkFitness);
        }

        private static void PushSparkSample(List<double> buffer, double value)
        {
            buffer.Add(value);
            if (buffer.Count > SparkSamples)
                buffer.RemoveAt(0);
        }

        private void BuildSparkline(System.Windows.Shapes.Polyline line, List<double> buffer)
        {
            int n = buffer.Count;
            var points = new PointCollection();
            if (n >= 2)
            {
                double w = sparkCanvas.Width;
                double h = sparkCanvas.Height;

                double max = 0;
                for (int i = 0; i < n; i++)
                    if (buffer[i] > max) max = buffer[i];
                if (max <= 0) max = 1;

                double stepX = w / (SparkSamples - 1);
                for (int i = 0; i < n; i++)
                {
                    double x = i * stepX;
                    double y = h - 1 - (buffer[i] / max) * (h - 2);
                    points.Add(new Point(x, y));
                }
            }
            line.Points = points;
        }

        private void ApplyRaftEnvironmentEffects()
        {
            Targets.ForEach(t =>
            {
                t.ObjectsOnTop = 0;
                t.FrogsOnTop = 0;
            });

            RaftTickContext ctx = new RaftTickContext { Rafts = Targets };

            // Each species applies its own raft/water rules and registers as predator/prey
            foreach (ISmartObject smartObject in lsObjects)
                ((SmartObject)smartObject).InteractWithRafts(ctx);

            DisposeAll(ResolveLandedBirdHuntsForTick(ctx.HungryLandedBirds, ctx.FrogsOnRafts));
            DisposeAll(ResolveRaftFrogHuntsForTick(ctx.HungryFrogsOnRafts, ctx.LandedBirds));
            DisposeAll(ResolveWaterFrogHuntsForTick(ctx.HungryFrogsInWater, ctx.Sharks));
            List<Bird> birdsReachableBySharks = new List<Bird>(ctx.FlyingBirds);
            birdsReachableBySharks.AddRange(ctx.LandedBirds);
            SharkHuntResult sharkHunts = ResolveSharkHuntsForTick(ctx.HungrySharks, ctx.FrogsInWater, birdsReachableBySharks);
            DisposeAll(sharkHunts.FrogsToDispose);
            DisposeAll(sharkHunts.BirdsToDispose);
            DisposeAll(ResolveBirdHuntsForTick(ctx.HungryBirds, ctx.Sharks));
        }

        private void DisposeAll<T>(List<T> objectsToDispose)
            where T : ISmartObject
        {
            foreach (ISmartObject obj in objectsToDispose)
                DisposeObject(obj);
        }

        internal static List<Frog> ResolveSharkHuntsForTick(List<Shark> hungrySharks, List<Frog> frogsInWater)
        {
            return ResolveBitesForTick(
                hungrySharks,
                frogsInWater,
                CurrentMovementSettings().BiteHpAmount,
                Shark.HuntRange,
                shark => shark.IsHungry,
                frog => true,
                onBite: (shark, frog) => shark.TriggerBite(),
                onKill: (shark, frog) => shark.FrogsEaten++);
        }

        internal static SharkHuntResult ResolveSharkHuntsForTick(
            List<Shark> hungrySharks,
            List<Frog> frogsInWater,
            List<Bird> birds)
        {
            var result = new SharkHuntResult();
            if (hungrySharks.Count == 0 || (frogsInWater.Count == 0 && birds.Count == 0))
                return result;

            foreach (Shark shark in hungrySharks)
            {
                if (shark.HP <= 0 || shark.BiteCooldownTicksRemaining > 0 || !shark.IsHungry)
                    continue;

                Frog nearestFrog = null;
                Bird nearestBird = null;
                double nearestDistanceSq = double.MaxValue;

                foreach (Frog frog in frogsInWater)
                {
                    if (frog.HP <= 0 || result.FrogsToDispose.Contains(frog))
                        continue;

                    double distanceSq = Point.Subtract(frog.Location, shark.Location).LengthSquared;
                    double effectiveRange = Shark.HuntRange + (frog.Size / 2D);
                    if (distanceSq > effectiveRange * effectiveRange || distanceSq > nearestDistanceSq)
                        continue;

                    nearestDistanceSq = distanceSq;
                    nearestFrog = frog;
                    nearestBird = null;
                }

                foreach (Bird bird in birds)
                {
                    if (bird.HP <= 0 || result.BirdsToDispose.Contains(bird))
                        continue;

                    double distanceSq = Point.Subtract(bird.Location, shark.Location).LengthSquared;
                    double effectiveRange = Shark.HuntRange + (bird.Size / 2D);
                    if (distanceSq > effectiveRange * effectiveRange || distanceSq > nearestDistanceSq)
                        continue;

                    nearestDistanceSq = distanceSq;
                    nearestFrog = null;
                    nearestBird = bird;
                }

                if (nearestFrog == null && nearestBird == null)
                    continue;

                if (nearestFrog != null)
                {
                    TransferBiteHp(shark, nearestFrog, CurrentMovementSettings().BiteHpAmount);
                    if (nearestFrog.HP <= 0)
                    {
                        shark.FrogsEaten++;
                        result.FrogsToDispose.Add(nearestFrog);
                    }
                }
                else
                {
                    TransferBiteHp(shark, nearestBird, 30);
                    if (nearestBird.HP <= 0)
                        result.BirdsToDispose.Add(nearestBird);
                }

                shark.BiteCooldownTicksRemaining = CurrentMovementSettings().BiteCooldownTicks;
                shark.TriggerBite();
            }

            return result;
        }

        internal static List<Frog> ResolveLandedBirdHuntsForTick(List<Bird> hungryLandedBirds, List<Frog> frogsOnRafts)
        {
            return ResolveBitesForTick(
                hungryLandedBirds,
                frogsOnRafts,
                CurrentMovementSettings().BiteHpAmount,
                Bird.HuntRange,
                bird => bird.IsLanded && bird.IsHungry,
                frog => true,
                onBite: null,
                onKill: null);
        }

        internal static List<Bird> ResolveRaftFrogHuntsForTick(List<Frog> hungryFrogsOnRafts, List<Bird> landedBirds)
        {
            return ResolveBitesForTick(
                hungryFrogsOnRafts,
                landedBirds,
                Frog.BiteHp,
                Frog.BiteRange,
                frog => frog.IsHungry,
                bird => bird.IsLanded,
                onBite: null,
                onKill: null);
        }

        internal static List<Shark> ResolveWaterFrogHuntsForTick(List<Frog> hungryFrogsInWater, List<Shark> sharks)
        {
            return ResolveBitesForTick(
                hungryFrogsInWater,
                sharks,
                Frog.BiteHp,
                Frog.BiteRange,
                frog => frog.IsHungry,
                shark => true,
                onBite: null,
                onKill: null);
        }

        internal static List<Bird> ResolveSharkHuntsForTick(List<Shark> hungrySharks, List<Bird> flyingBirds)
        {
            return ResolveBitesForTick(
                hungrySharks,
                flyingBirds,
                30,
                Shark.HuntRange,
                shark => shark.IsHungry,
                bird => true,
                onBite: (shark, bird) => shark.TriggerBite(),
                onKill: null);
        }

        internal static List<Shark> ResolveBirdHuntsForTick(List<Bird> hungryBirds, List<Shark> sharks)
        {
            return ResolveBitesForTick(
                hungryBirds,
                sharks,
                CurrentMovementSettings().BiteHpAmount,
                Bird.HuntRange,
                bird => !bird.IsLanded && bird.IsHungry,
                shark => true,
                onBite: null,
                onKill: (bird, shark) => bird.SharksEaten++);
        }

        private static List<TPrey> ResolveBitesForTick<TPredator, TPrey>(
            List<TPredator> predators,
            List<TPrey> prey,
            double biteHp,
            double biteRange,
            Func<TPredator, bool> canPredatorBite,
            Func<TPrey, bool> canPreyBeBitten,
            Action<TPredator, TPrey> onBite,
            Action<TPredator, TPrey> onKill)
            where TPredator : SmartObject
            where TPrey : SmartObject
        {
            List<TPrey> preyToDispose = new List<TPrey>();
            if (predators.Count == 0 || prey.Count == 0)
                return preyToDispose;

            foreach (TPredator predator in predators)
            {
                if (predator.HP <= 0 || predator.BiteCooldownTicksRemaining > 0 || !canPredatorBite(predator))
                    continue;

                TPrey nearestPrey = null;
                double nearestDistanceSq = double.MaxValue;

                foreach (TPrey candidate in prey)
                {
                    if (candidate.HP <= 0 || preyToDispose.Contains(candidate) || !canPreyBeBitten(candidate))
                        continue;

                    Vector distanceVector = Point.Subtract(candidate.Location, predator.Location);
                    double distanceSq = distanceVector.LengthSquared;
                    double effectiveRange = biteRange + (candidate.Size / 2D);
                    if (distanceSq > effectiveRange * effectiveRange || distanceSq > nearestDistanceSq)
                        continue;

                    nearestDistanceSq = distanceSq;
                    nearestPrey = candidate;
                }

                if (nearestPrey == null)
                    continue;

                TransferBiteHp(predator, nearestPrey, biteHp);
                predator.BiteCooldownTicksRemaining = CurrentMovementSettings().BiteCooldownTicks;
                onBite?.Invoke(predator, nearestPrey);

                if (nearestPrey.HP <= 0)
                {
                    onKill?.Invoke(predator, nearestPrey);
                    preyToDispose.Add(nearestPrey);
                }
            }

            return preyToDispose;
        }

        private static MovementSettings CurrentMovementSettings()
        {
            MovementSettings settings = SmartObject.MovementSettings ?? new MovementSettings();
            settings.Normalize();
            return settings;
        }

        private static void TransferBiteHp(SmartObject predator, SmartObject prey, double biteHp)
        {
            double transferredHp = Math.Min(biteHp, prey.HP);
            prey.HP -= transferredHp;
            predator.HP += transferredHp;
        }

        internal sealed class SharkHuntResult
        {
            public List<Frog> FrogsToDispose { get; } = new List<Frog>();
            public List<Bird> BirdsToDispose { get; } = new List<Bird>();
        }

        private static string GetObjectKindName(ISmartObject smartObject)
        {
            if (smartObject is Bird)
                return "Bird";
            if (smartObject is Shark)
                return "Shark";

            return smartObject is Frog ? "Frog" : "Agent";
        }
    }
}
