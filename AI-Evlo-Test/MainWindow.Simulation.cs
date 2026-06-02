using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AI_Evlo_Test.Objects;
using AI_Evlo_Test.Enumerators;

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
            int totalObj = lsObjects?.Count ?? 0;

            // remove the unsuccessful by looping backwards
            for (int i = totalObj - 1; i >= 0; i--)
            {
                if (lsObjects[i].HP <= 0)
                    DisposeObject(lsObjects[i]);
            }
            // if removing the selected obj, then select new one
            if (!isHeadlessMode && (SelectedObject == null || SelectedObject.HP < 1))
                SelectObject(GetTopFitnessObject());

            // Remove from populations
            foreach (Population Popul in lsPopulations)
            {
                Popul.LifeCycles++;
                Popul.Members.RemoveAll(o => o.HP <= 0);
                if (Popul.Members.Count < Popul.SizeLimit)
                    ReGrowPopulation(Popul);
            }

            CycleCount++;
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
            IList<ISensable> snapshot = BuildSensableSnapshot();
            double targetRadius = Target.Size / 2;

            Parallel.ForEach(lsObjects, smartObject =>
            {
                SmartObject smart = smartObject as SmartObject;

                // Each species declares what it can and cannot perceive
                smart.Perception.Update(smart.Location, smart.FaceDirection, snapshot, smart.ID, smart.IgnoredCategories);

                // Build NN inputs: 1 scalar (HP deficit) + 24 ray signals = 25
                double hpDeficit = 1.0 - (smartObject.HP / smart.EffectiveMaxHp);
                smart.Perception.FillInputs(smart.CachedInputs, hpDeficit);

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

            if (!isHeadlessMode)
            {
                // Visualize rays for the selected agent only
                if (SelectedObject != null && SelectedObject.HP > 0 && SelectedObject is SmartObject selSmart)
                    rayVisualizer.Draw(selSmart.Location, selSmart.Perception);

                foreach (ISmartObject smartObject in lsObjects)
                {
                    if (smartObject.VisibleShape == null)
                        continue;

                    // Update UI
                    SmartObject smart = (SmartObject)smartObject;
                    smartObject.VisibleShape.Opacity = (2 * smartObject.HP / smart.EffectiveMaxHp);
                    double anglFromVertical = Vector.AngleBetween(new Vector(0, -1), smart.FaceDirection);
                    smartObject.VisibleShape.RenderTransform = new RotateTransform(anglFromVertical);

                    // Animate species sprite
                    if (smartObject.VisibleShape is Image img)
                    {
                        ImageSource frame = smart.GetSpriteFrame();
                        if (frame != null)
                            img.Source = frame;
                    }

                    DrawImage(smartObject.VisibleShape, smartObject.Location);
                }
            }
        }

        private void MoveAgentsEnvirnoment2()
        {
            IList<ISensable> snapshot = BuildSensableSnapshot();

            Parallel.ForEach(lsObjects, smartObject =>
            {
                SmartObject smart = smartObject as SmartObject;

                // Each species declares what it can and cannot perceive
                smart.Perception.Update(smart.Location, smart.FaceDirection, snapshot, smart.ID, smart.IgnoredCategories);

                // Build NN inputs: 1 scalar (HP deficit) + 24 ray signals = 25
                double hpDeficit = 1.0 - (smartObject.HP / smart.EffectiveMaxHp);
                smart.Perception.FillInputs(smart.CachedInputs, hpDeficit);

                smartObject.Act(smart.CachedInputs);
            });

            ApplyRaftEnvironmentEffects();

            if (!isHeadlessMode)
            {
                // Visualize rays for the selected agent only
                if (SelectedObject != null && SelectedObject.HP > 0 && SelectedObject is SmartObject selSmart)
                    rayVisualizer.Draw(selSmart.Location, selSmart.Perception);

                foreach (ISmartObject smartObject in lsObjects)
                {
                    if (smartObject.VisibleShape == null)
                        continue;

                    // Update UI
                    SmartObject smart = (SmartObject)smartObject;
                    smartObject.VisibleShape.Opacity = (2 * smartObject.HP / smart.EffectiveMaxHp);
                    if (smartObject.VisibleShape is Polygon polygon)
                        polygon.Stroke = smartObject.IsGettingHP ? Brushes.GreenYellow : Brushes.OrangeRed;

                    double anglFromVertical = Vector.AngleBetween(new Vector(0, -1), smart.FaceDirection);
                    smartObject.VisibleShape.RenderTransform = new RotateTransform(anglFromVertical);

                    // Animate species sprite
                    if (smartObject.VisibleShape is Image img)
                    {
                        ImageSource frame = smart.GetSpriteFrame();
                        if (frame != null)
                            img.Source = frame;
                    }

                    DrawImage(smartObject.VisibleShape, smartObject.Location);
                }
            }
        }

        private void MoveTarget()
        {
            foreach (TargetObj target in Targets)
            {
                // Bounce of edges of screen
                if (target.Location.X < (dblTargetSize / 2)
                    || target.Location.X > panlUniverseView.ActualWidth)
                {
                    target.Intertia.X = NextRandomDouble() / 4 + 0.1;
                    if (target.Location.X > panlUniverseView.ActualWidth)
                        target.Intertia.X *= -1; // reverse direction
                    target.RotationDegPerSec = RandomRotationSpeed(); // bump → new spin speed
                }

                if (target.Location.Y < (dblTargetSize / 2)
                    || target.Location.Y > panlUniverseView.ActualHeight)
                {
                    target.Intertia.Y = NextRandomDouble() / 4 + 0.1;
                    if (target.Location.Y > panlUniverseView.ActualHeight)
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

                    if (!isHeadlessMode && target.VisibleShape != null)
                    {
                        target.VisibleShape.Width = target.Size;
                        target.VisibleShape.Height = target.Size;
                    }
                }


                int halfPopulationSize = lsPopulations.Sum(p => p.SizeLimit) / 2;
                if (target.ObjectsOnTop <= halfPopulationSize)
                    target.Underwater++;
                else
                    target.Underwater--;

                if (target.Underwater >= 0)
                {
                    target.HpCharge = 1;
                    if (!isHeadlessMode && target.VisibleShape != null)
                        target.VisibleShape.Opacity = 0.6;
                }
                else
                {
                    if (!isHeadlessMode && target.VisibleShape != null)
                        target.VisibleShape.Opacity = 0.3;
                    target.HpCharge = 0;
                }
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
                if (!isHeadlessMode && target.VisibleShape != null)
                    DrawImage(target.VisibleShape, target.Location);
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

                // Redraw targets at corrected positions after bounce separation
                if (!isHeadlessMode)
                    foreach (TargetObj target in Targets)
                        if (target.VisibleShape != null)
                            DrawImage(target.VisibleShape, target.Location);
            }
        }

        /// <summary>
        /// Builds a snapshot list of all ISensable objects for raycasting.
        /// Call once per tick before the Parallel.ForEach.
        /// The returned list is read-only for the duration of that tick.
        /// </summary>
        private IList<ISensable> BuildSensableSnapshot()
        {
            _sensableSnapshot.Clear();

            // Targets
            foreach (TargetObj target in Targets)
            {
                if (eEnvironmentType == EEnvironmentType.OneTarget)
                    target.Category = ObjectCategory.Food;
                else
                    target.Category = target.Underwater >= 0 ? ObjectCategory.Raft : ObjectCategory.Raft_Sunk;
                _sensableSnapshot.Add(target);
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
                        _sensableSnapshot.Add(smart);
                    }
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
        readonly List<ISensable> _sensableSnapshot = new List<ISensable>();

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

            Population pop = lsPopulations.FirstOrDefault(p => p.Members.Contains(SelectedObject));
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

            // Status bar
            string statusText = $"Cycle {CycleCount}  |  Cycles/s: {cyclesPerSecond:F2}";
            if (Targets.Count > 0)
            {
                int onTarget = Targets[0].ObjectsOnTop;
                int alive = lsObjects.Count;
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

            // Population info
            for (int i = 0; i < lsPopulations.Count; i++)
            {
                Population pop = lsPopulations[i];
                if (pop.LifeCycles == 0 || pop.Members.Count == 0)
                    continue;

                int liveMembers = 0;
                int genMin = int.MaxValue;
                int genMax = int.MinValue;
                double totalFitness = 0;

                for (int memberIndex = 0; memberIndex < pop.Members.Count; memberIndex++)
                {
                    ISmartObject member = pop.Members[memberIndex];
                    if (member.Fitness > 0)
                        liveMembers++;

                    if (member.Generation < genMin)
                        genMin = member.Generation;
                    if (member.Generation > genMax)
                        genMax = member.Generation;

                    totalFitness += member.Fitness;
                }

                int lostMembers = pop.TotalMembersCount - liveMembers;
                int avgFitness = (int)(totalFitness / pop.Members.Count);

                if (i >= lsPopuCards.Count)
                    continue;
                PopulationCard card = lsPopuCards[i];

                card.Title.Text = $"{pop.Name}  ·  {GetPopulationBeingName(pop.Being)}";

                string genes = pop.lsBestGenes.Count > 0
                    ? $"  ·  top genes {(int)pop.lsBestGenes[pop.lsBestGenes.Count - 1].Fitness}–{(int)pop.lsBestGenes[0].Fitness}"
                    : "";
                card.Stats.Text =
                    $"{liveMembers} alive / {lostMembers} lost  ·  Gen {genMin}–{genMax}" + Environment.NewLine +
                    $"avg fitness {avgFitness}{genes}";

                string tip =
                    $"Population '{pop.Name}' ({GetPopulationBeingName(pop.Being)})" + Environment.NewLine +
                    $"Live agents: {liveMembers}  |  Lost agents: {lostMembers}" + Environment.NewLine +
                    $"Generations with surviving agents: {genMin} to {genMax}" + Environment.NewLine +
                    $"Average fitness: {avgFitness}";
                card.Root.ToolTip = tip;

                if (_selectedPopulation == pop)
                {
                    lblPopulationInfo.Content = $"{pop.Name} ({GetPopulationBeingName(pop.Being)})  {card.Stats.Text}";
                    lblPopulationInfo.ToolTip = tip;
                }
            }
        }

        private void ApplyRaftEnvironmentEffects()
        {
            Targets.ForEach(t => t.ObjectsOnTop = 0);

            RaftTickContext ctx = new RaftTickContext { Rafts = Targets };

            // Each species applies its own raft/water rules and registers as predator/prey
            foreach (ISmartObject smartObject in lsObjects)
                ((SmartObject)smartObject).InteractWithRafts(ctx);

            ResolveBirdHunts(ctx.HungryBirds, ctx.Sharks);
            ResolveSharkHunts(ctx.HungrySharks, ctx.FrogsInWater);
        }

        private void ResolveSharkHunts(List<Shark> hungrySharks, List<Frog> frogsInWater)
        {
            if (hungrySharks.Count == 0 || frogsInWater.Count == 0)
                return;

            HashSet<ISmartObject> frogsToDispose = new HashSet<ISmartObject>();

            foreach (Shark shark in hungrySharks)
            {
                if (shark.HP <= 0)
                    continue;

                Frog nearestFrog = null;
                double nearestDistanceSq = Shark.HuntRange * Shark.HuntRange;

                foreach (Frog frog in frogsInWater)
                {
                    if (frogsToDispose.Contains(frog))
                        continue;

                    Vector distanceVector = Point.Subtract(frog.Location, shark.Location);
                    double distanceSq = distanceVector.LengthSquared;
                    if (distanceSq > nearestDistanceSq)
                        continue;

                    nearestDistanceSq = distanceSq;
                    nearestFrog = frog;
                }

                if (nearestFrog == null)
                    continue;

                shark.HP += nearestFrog.HP;
                shark.FrogsEaten++;
                shark.TriggerBite();
                frogsToDispose.Add(nearestFrog);
            }

            foreach (ISmartObject frog in frogsToDispose)
                DisposeObject(frog);
        }

        private void ResolveBirdHunts(List<Bird> hungryBirds, List<Shark> sharks)
        {
            List<Shark> sharksToDispose = ResolveBirdHuntsForTick(hungryBirds, sharks);
            foreach (ISmartObject shark in sharksToDispose)
                DisposeObject(shark);
        }

        internal static List<Shark> ResolveBirdHuntsForTick(List<Bird> hungryBirds, List<Shark> sharks)
        {
            List<Shark> sharksToDispose = new List<Shark>();
            if (hungryBirds.Count == 0 || sharks.Count == 0)
                return sharksToDispose;

            foreach (Bird bird in hungryBirds)
            {
                if (!bird.IsHungry)
                    continue;

                Shark nearestShark = null;
                double nearestDistanceSq = double.MaxValue;

                foreach (Shark shark in sharks)
                {
                    if (shark.HP <= 0 || sharksToDispose.Contains(shark))
                        continue;

                    Vector distanceVector = Point.Subtract(shark.Location, bird.Location);
                    double distanceSq = distanceVector.LengthSquared;
                    double effectiveRange = Bird.HuntRange + (shark.Size / 2D);
                    if (distanceSq > effectiveRange * effectiveRange || distanceSq > nearestDistanceSq)
                        continue;

                    nearestDistanceSq = distanceSq;
                    nearestShark = shark;
                }

                if (nearestShark == null)
                    continue;

                bird.HP += nearestShark.HP;
                bird.SharksEaten++;
                nearestShark.HP = 0;
                sharksToDispose.Add(nearestShark);
            }

            return sharksToDispose;
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
