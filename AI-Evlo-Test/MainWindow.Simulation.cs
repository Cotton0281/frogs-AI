using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

            if (lsObjects == null || lsObjects.Count == 0)
                return;

            if (eEnvironmentType == EEnvironmentType.OneTarget)
                MoveAgentsEnvirnoment1();
            else if (eEnvironmentType == EEnvironmentType.TwoTargets)
                MoveAgentsEnvirnoment2();


            // New GENRATiON
            ///Remove low quality and repopulate with 20% above population size
            int totalObj = lsObjects.Count;

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

            if (targLocation == null || (targLocation.X == 0 && targLocation.Y == 0))
            {
                targLocation = new Point(panlUniverseView.ActualWidth / 2, panlUniverseView.ActualHeight / 2);
            }
            //create target
            foreach (BasicObject target in Targets)
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
                    target.VisibleShape = CreateNewImage("raft.png");
                }
                targLocation.X += target.VisibleShape.Width;
                target.Intertia.X = NextRandomDouble() / 4 + 0.1;
                target.Intertia.Y = NextRandomDouble() / 4 + 0.1;
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

                // Frogs see only birds and rafts; birds see everything
                ObjectCategory[] ignored = smart is Bird ? null : _frogIgnoredCategories;
                smart.Perception.Update(smart.Location, smart.FaceDirection, snapshot, smart.ID, ignored);

                // Build NN inputs: 2 scalars (HP deficit, stamina deficit) + 24 ray signals = 26
                int effectiveMaxHp = smart is Bird ? Bird.BirdMaxHp : SmartObject.MaxHp;
                double hpDeficit = 1.0 - (smartObject.HP / effectiveMaxHp);
                double staminaDeficit = 1.0 - (smartObject.Stamina / SmartObject.MaxStamina);
                smart.Perception.FillInputs(smart.CachedInputs, hpDeficit, staminaDeficit);

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
                    int maxHp1 = smartObject is Bird ? Bird.BirdMaxHp : SmartObject.MaxHp;
                    smartObject.VisibleShape.Opacity = (2 * smartObject.HP / maxHp1);
                    double anglFromVertical = Vector.AngleBetween(new Vector(0, -1), (smartObject as SmartObject).FaceDirection);
                    smartObject.VisibleShape.RenderTransform = new RotateTransform(anglFromVertical);

                    // Animate frog sprite
                    if (smartObject is Frog frog && smartObject.VisibleShape is Image img)
                        img.Source = frog.GetNextSpriteFrame();
                    else if (smartObject is Bird bird && smartObject.VisibleShape is Image birdImage)
                        birdImage.Source = bird.GetCurrentSpriteFrame();

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

                // Frogs see only birds and rafts; birds see everything
                ObjectCategory[] ignored = smart is Bird ? null : _frogIgnoredCategories;
                smart.Perception.Update(smart.Location, smart.FaceDirection, snapshot, smart.ID, ignored);

                // Build NN inputs: 2 scalars (HP deficit, stamina deficit) + 24 ray signals = 26
                int effectiveMaxHp = smart is Bird ? Bird.BirdMaxHp : SmartObject.MaxHp;
                double hpDeficit = 1.0 - (smartObject.HP / effectiveMaxHp);
                double staminaDeficit = 1.0 - (smartObject.Stamina / SmartObject.MaxStamina);
                smart.Perception.FillInputs(smart.CachedInputs, hpDeficit, staminaDeficit);

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
                    int maxHp = smartObject is Bird ? Bird.BirdMaxHp : SmartObject.MaxHp;
                    smartObject.VisibleShape.Opacity = (2 * smartObject.HP / maxHp);
                    if (smartObject.VisibleShape is Polygon polygon)
                        polygon.Stroke = smartObject.IsGettingHP ? Brushes.GreenYellow : Brushes.OrangeRed;

                    double anglFromVertical = Vector.AngleBetween(new Vector(0, -1), (smartObject as SmartObject).FaceDirection);
                    smartObject.VisibleShape.RenderTransform = new RotateTransform(anglFromVertical);

                    // Animate frog sprite
                    if (smartObject is Frog frog && smartObject.VisibleShape is Image img)
                        img.Source = frog.GetNextSpriteFrame();
                    else if (smartObject is Bird bird && smartObject.VisibleShape is Image birdImage)
                        birdImage.Source = bird.GetCurrentSpriteFrame();

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
                }

                if (target.Location.Y < (dblTargetSize / 2)
                    || target.Location.Y > panlUniverseView.ActualHeight)
                {
                    target.Intertia.Y = NextRandomDouble() / 4 + 0.1;
                    if (target.Location.Y > panlUniverseView.ActualHeight)
                        target.Intertia.Y *= -1;
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
                        BasicObject.ResolveElasticBounce(Targets[i], Targets[j]);

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

            // Agents — tag category based on species and state
            foreach (Population pop in lsPopulations)
            {
                foreach (ISmartObject member in pop.Members)
                {
                    if (member is SmartObject smart)
                    {
                        if (smart is Bird bird)
                            smart.Category = bird.IsLanded ? ObjectCategory.Bird_Landed : ObjectCategory.Bird;
                        else
                            smart.Category = ObjectCategory.Frog;
                        _sensableSnapshot.Add(smart);
                    }
                }
            }

            return _sensableSnapshot;
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

        List<Label> lsPopuLabels = new List<Label>();
        readonly List<ISensable> _sensableSnapshot = new List<ISensable>();

        /// <summary>Frogs cannot see other frogs — only birds and rafts.</summary>
        private static readonly ObjectCategory[] _frogIgnoredCategories = new[]
        {
            ObjectCategory.Frog
        };
        private void UpdateLabbels()
        {
            if (DateTime.Now.Subtract(dtLastLabelsUpdate) < new TimeSpan(0, 0, 0, 0, 200))
                return;
            dtLastLabelsUpdate = DateTime.Now;

            // Selected agent info
            if (SelectedObject != null && SelectedObject.Location != null)
            {
                int staminaPct = (int)(SelectedObject.Stamina / SmartObject.MaxStamina * 100);
                int selectedMaxHp = SelectedObject is Bird ? Bird.BirdMaxHp : SmartObject.MaxHp;

                string agentInfo =
                    $"{GetObjectKindName(SelectedObject)} {SelectedObject.ID}  |  Gen {SelectedObject.Generation} | Lived {SelectedObject.Cycles} cycles"
                    + Environment.NewLine +
                    $"HP: {(int)SelectedObject.HP}/{selectedMaxHp}  |  Stamina: {staminaPct}%"
                    + (SelectedObject is Bird selectedBird ? $"  |  Frogs eaten: {selectedBird.FrogsEaten}" : "");
                lblID.Content = agentInfo;
                lblID.ToolTip = agentInfo;
            }
            else
            {
                lblID.Content = "No agent selected — click an agent to inspect it";
                lblID.ToolTip = "No agent selected — click an agent to inspect it";
            }

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
                double totalStamina = 0;

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
                    totalStamina += member.Stamina;
                }

                int lostMembers = pop.TotalMembersCount - liveMembers;
                int avgFitness = (int)(totalFitness / pop.Members.Count);
                int avgStamina = (int)(totalStamina / pop.Members.Count);

                lsPopuLabels[i].Content =
                    $"{pop.Name} ({GetPopulationBeingName(pop.Being)})  [ {liveMembers} alive / {lostMembers} lost ]  Gen {genMin}–{genMax}"
                    + Environment.NewLine +
                    $"Avg fitness: {avgFitness}  |  Avg stamina: {avgStamina}";

                lsPopuLabels[i].ToolTip =
                    $"Population '{pop.Name}' ({GetPopulationBeingName(pop.Being)})" + Environment.NewLine +
                    $"Live agents: {liveMembers}  |  Lost agents: {lostMembers}" + Environment.NewLine +
                    $"Generations with surviving agents: {genMin} to {genMax}" + Environment.NewLine +
                    $"Average fitness: {avgFitness}  |  Average stamina: {avgStamina}";

                if (pop.lsBestGenes.Count > 0)
                {
                    double minBestFitness = pop.lsBestGenes[pop.lsBestGenes.Count - 1].Fitness;
                    double maxBestFitness = pop.lsBestGenes[0].Fitness;
                    lsPopuLabels[i].Content +=
                        $"  |  Top {pop.lsBestGenes.Count} genes: fitness {minBestFitness}–{maxBestFitness}";
                }

                if (_selectedPopulation == pop)
                {
                    lblPopulationInfo.Content = lsPopuLabels[i].Content;
                    lblPopulationInfo.ToolTip = lsPopuLabels[i].ToolTip;
                }
            }
        }

        private void ApplyRaftEnvironmentEffects()
        {
            Targets.ForEach(t => t.ObjectsOnTop = 0);

            List<Tuple<Bird, TargetObj>> landedBirds = new List<Tuple<Bird, TargetObj>>();
            List<Tuple<Frog, TargetObj>> frogsOnRafts = new List<Tuple<Frog, TargetObj>>();

            foreach (ISmartObject smartObject in lsObjects)
            {
                if (smartObject is Bird bird)
                {
                    // Birds: check each raft for landing, but ignore water/raft HP modifiers
                    TargetObj landedRaft = null;
                    foreach (TargetObj target in Targets)
                    {
                        double raftRadius = target.Size / 2D;
                        Vector toTarget = Point.Subtract(target.Location, smartObject.Location);
                        if (toTarget.LengthSquared <= raftRadius * raftRadius)
                        {
                            target.ObjectsOnTop++;
                            if (target.HpCharge > 0 && landedRaft == null)
                                landedRaft = target;
                        }
                    }

                    bird.IsLanded = landedRaft != null;
                    smartObject.IsGettingHP = false;
                    smartObject.HP -= bird.IsLanded ? Bird.LandedHpDrain : Bird.FlightHpDrain;
                    if (bird.IsLanded && bird.IsHungry)
                        landedBirds.Add(Tuple.Create(bird, landedRaft));

                    continue;
                }

                // Frogs / other agents: check every target independently (original behavior)
                smartObject.IsGettingHP = false;
                foreach (TargetObj target in Targets)
                {
                    double raftRadius = target.Size / 2D;
                    Vector toTarget = Point.Subtract(target.Location, smartObject.Location);
                    if (toTarget.LengthSquared <= raftRadius * raftRadius)
                    {
                        target.ObjectsOnTop++;
                        smartObject.HP += target.HpCharge;
                        if (target.HpCharge > 0)
                        {
                            smartObject.IsGettingHP = true;
                            if (smartObject is Frog frog)
                                frogsOnRafts.Add(Tuple.Create(frog, target));
                        }
                    }
                }

                // The environment takes HP from all non-bird agents
                smartObject.HP -= 0.35;
            }

            ResolveBirdHunts(landedBirds, frogsOnRafts);
        }

        private void ResolveBirdHunts(List<Tuple<Bird, TargetObj>> landedBirds, List<Tuple<Frog, TargetObj>> frogsOnRafts)
        {
            HashSet<ISmartObject> frogsToDispose = new HashSet<ISmartObject>();

            foreach (Tuple<Bird, TargetObj> landedBird in landedBirds)
            {
                Bird bird = landedBird.Item1;

                // Birds only hunt when hungry (below 90% of max HP)
                if (!bird.IsHungry)
                    continue;

                TargetObj raft = landedBird.Item2;
                Frog nearestFrog = null;
                double nearestDistanceSq = Bird.HuntRange * Bird.HuntRange;

                foreach (Tuple<Frog, TargetObj> frogOnRaft in frogsOnRafts)
                {
                    if (!ReferenceEquals(frogOnRaft.Item2, raft) || frogsToDispose.Contains(frogOnRaft.Item1))
                        continue;

                    Vector distanceVector = Point.Subtract(frogOnRaft.Item1.Location, bird.Location);
                    double distanceSq = distanceVector.LengthSquared;
                    if (distanceSq > nearestDistanceSq)
                        continue;

                    nearestDistanceSq = distanceSq;
                    nearestFrog = frogOnRaft.Item1;
                }

                if (nearestFrog == null)
                    continue;

                bird.HP += Bird.HuntHpGain;
                bird.FrogsEaten++;
                frogsToDispose.Add(nearestFrog);
            }

            foreach (ISmartObject frog in frogsToDispose)
                DisposeObject(frog);
        }

        private static string GetObjectKindName(ISmartObject smartObject)
        {
            if (smartObject is Bird)
                return "Bird";

            return smartObject is Frog ? "Frog" : "Agent";
        }
    }
}
