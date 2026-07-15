using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AI_Evlo_Test.ConfigLib;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using AI_Evlo_Test.Enumerators;

namespace AI_Evlo_Test
{
    // Agent creation, shape factories, offspring, disposal, and population regrowth.
    public partial class MainWindow
    {
        // Canvas Z-ordering (low draws first / underneath):
        //   sharks (underwater) < rafts < frogs (on rafts) < birds (flying)
        private const int ZIndexShark = -10;
        private const int ZIndexRaft = 0;
        private const int ZIndexFrog = 1;
        private const int ZIndexBird = 10;

        // Visuals whose agent was disposed by the model step, awaiting removal on the UI thread.
        private readonly List<FrameworkElement> _visualsToRemove = new List<FrameworkElement>();

        /// <summary>Queues an agent's shape for removal. Safe to call from the simulation thread.</summary>
        private void QueueVisualRemoval(FrameworkElement shape)
        {
            if (shape != null)
                _visualsToRemove.Add(shape);
        }

        /// <summary>
        /// UI-thread visual reconciliation: removes shapes of disposed agents and creates shapes
        /// for newly spawned ones. Lets the model step stay free of any WPF calls.
        /// </summary>
        private void ReconcileVisuals()
        {
            for (int i = 0; i < _visualsToRemove.Count; i++)
            {
                FrameworkElement shape = _visualsToRemove[i];
                shape.MouseDown -= ObjectInterface_MouseDown;
                shapeToObjectMap.Remove(shape);
                panlUniverseView.Children.Remove(shape);
            }
            _visualsToRemove.Clear();

            for (int i = 0; i < lsObjects.Count; i++)
            {
                ISmartObject o = lsObjects[i];
                if (o.VisibleShape != null)
                    continue;

                EnsureVisualForObject(o, FindPopulationForObject(o));
                if (o is SmartObject s && s.IsGoldenAgent)
                    ApplyGoldenVisual(o);
                if (o.VisibleShape != null)
                    DrawImage(o.VisibleShape, o.Location);
            }
        }

        private Population CreatePopulation(int PopulationSize, string PopulationName, string nnType, PopulationBeing being)
        {
            if (PopulationSize < 1)
                return null;
            Population newPopulation = new Population();
            newPopulation.Name = PopulationName;
            newPopulation.SizeLimit = PopulationSize;
            newPopulation.Being = being;

            // Set NNet Size based on the captured NN type
            if (nnType == "Small")
                newPopulation.NeuroNetTemplate = NeuroNetStructure.Small_1Lx9N();
            else if (nnType == "Medium")
                newPopulation.NeuroNetTemplate = NeuroNetStructure.Mid_3Lx10N();
            else if (nnType == "Large")
                newPopulation.NeuroNetTemplate = NeuroNetStructure.Big_5Lx20N();

            // give a collor
            switch (lsPopulations.Count)
            {
                case 0: newPopulation.PopulationColor = Colors.SteelBlue; break;
                case 1: newPopulation.PopulationColor = Colors.Orange; break;
                case 2: newPopulation.PopulationColor = Colors.Aqua; break;
                case 3: newPopulation.PopulationColor = Colors.BlueViolet; break;
                case 4: newPopulation.PopulationColor = Colors.BurlyWood; break;
                case 5: newPopulation.PopulationColor = Colors.Gold; break;
                default: newPopulation.PopulationColor = Colors.Aquamarine; break;
            }
            newPopulation.ObjectType = GetObjectTypeForBeing(being);

            // Create and configure objects
            for (int i = 0; i < PopulationSize; i++)
            {
                ISmartObject newObj = CreatePopulationMember(newPopulation);
                newPopulation.Add(newObj);
                lsObjects.Add(newObj);
                newObj.ID = newPopulation.GenerateMemberId();

            }
            return newPopulation;
        }

        private void ReGrowPopulation(Population objPopulation)
        {
            TryRegrowPopulation(objPopulation, CycleCount);
        }

        internal bool TryRegrowPopulation(Population population, int currentCycle)
        {
            if (!PopulationRegrowthPolicy.NeedsRegrowth(population))
            {
                PopulationRegrowthPolicy.ClearSchedule(population);
                return false;
            }

            if (!population.SpawnDelay)
            {
                RegrowthBrainSource immediateSource = PopulationRegrowthPolicy.SelectSource(population);
                ISmartObject immediateMember = CreateRegrowthMember(population, immediateSource);
                ApplyImmediateSpawnLocation(immediateMember, population);
                AddPopulationMember(population, immediateMember);
                PopulationRegrowthPolicy.MarkSpawned(population, currentCycle);
                return true;
            }

            if (population.NextRegrowCycle < 0)
            {
                PopulationRegrowthPolicy.ScheduleNextSpawn(population, currentCycle);
                return false;
            }

            if (!PopulationRegrowthPolicy.ShouldSpawn(population, currentCycle))
                return false;

            RegrowthBrainSource source = PopulationRegrowthPolicy.SelectSource(population);
            ISmartObject newObj = CreateRegrowthMember(population, source);
            AddPopulationMember(population, newObj);
            PopulationRegrowthPolicy.MarkSpawned(population, currentCycle);
            if (!PopulationRegrowthPolicy.NeedsRegrowth(population))
                PopulationRegrowthPolicy.ClearSchedule(population);

            return true;
        }

        private void FillPopulationImmediate(Population population, bool useArchive)
        {
            while (population.Members.Count < population.SizeLimit)
            {
                ISmartObject newObj = null;
                int archiveIndex = population.Members.Count;
                if (useArchive && archiveIndex < population.lsBestGenes.Count)
                    newObj = CreateFromArchivedGene(population, population.lsBestGenes[archiveIndex], mutate: false);

                if (newObj == null)
                    newObj = CreatePopulationMember(population);

                AddPopulationMember(population, newObj);
            }

            PopulationRegrowthPolicy.ClearSchedule(population);
        }

        private ISmartObject CreateRegrowthMember(Population population, RegrowthBrainSource source)
        {
            switch (source.Kind)
            {
                case RegrowthBrainSourceKind.ArchivedBestExact:
                    return CreateFromArchivedGene(population, source.ArchivedParent, mutate: false);
                case RegrowthBrainSourceKind.ArchivedBestMutated:
                    return CreateFromArchivedGene(population, source.ArchivedParent, mutate: true);
                case RegrowthBrainSourceKind.AliveBestExact:
                    return CreateFromAliveParent(population, source.AliveParent, mutate: false);
                case RegrowthBrainSourceKind.AliveBestMutated:
                    return CreateFromAliveParent(population, source.AliveParent, mutate: true);
                case RegrowthBrainSourceKind.GoldenAgentMutated:
                    return CreateFromAliveParent(population, source.AliveParent, mutate: true);
                default:
                    return CreatePopulationMember(population);
            }
        }

        private static void ApplyImmediateSpawnLocation(ISmartObject newObj, Population population)
        {
            ISmartObject locationSource = PopulationRegrowthPolicy.LongestLivedMember(population);
            if (newObj == null || locationSource == null)
                return;

            newObj.SetLocation(locationSource.Location.X, locationSource.Location.Y);
        }

        private ISmartObject CreateFromArchivedGene(Population population, GenomeRecord parent, bool mutate)
        {
            if (parent == null || parent.Gene == null)
                return CreatePopulationMember(population);

            if (!Utils.MatchesStructure(parent.Gene, population.NeuroNetTemplate))
                return CreatePopulationMember(population);

            NeuralNetworkFactory nNetworkFactory = NeuralNetworkFactory.GetInstance();
            INeuralNetwork network = nNetworkFactory.Create(Utils.CloneGene(parent.Gene));
            if (PopulationRegrowthPolicy.ShouldMutate(population, mutate))
                network = evoChember.MutateNN(network, 1, false);

            ISmartObject newObj = CreatePopulationMember(population);
            newObj.NNetwork = network;
            newObj.Generation = parent.Generation + 1;
            newObj.ParentId = parent.ID;
            newObj.ID = population.GenerateMemberId(parent);
            parent.Ofsprings++;
            return newObj;
        }

        private ISmartObject CreateFromAliveParent(Population population, ISmartObject parent, bool mutate)
        {
            return CreateFromAliveParent(population, parent, mutate, spawnAtParentLocation: false);
        }

        private ISmartObject CreateFromAliveParent(
            Population population,
            ISmartObject parent,
            bool mutate,
            bool spawnAtParentLocation)
        {
            if (parent == null || parent.NNetwork == null)
                return CreatePopulationMember(population);

            INeuralNetwork network = Utils.CloneNeuroNet(parent.NNetwork);
            if (PopulationRegrowthPolicy.ShouldMutate(population, mutate))
                network = evoChember.MutateNN(network, 1, false);

            ISmartObject newObj = CreatePopulationMember(population);
            newObj.NNetwork = network;
            newObj.SetLocation(
                spawnAtParentLocation ? parent.Location.X : parent.Location.X + 1,
                spawnAtParentLocation ? parent.Location.Y : parent.Location.Y + 1);
            // Visual is created lazily by ReconcileVisuals on the UI thread.
            newObj.Generation = parent.Generation + 1;
            newObj.ParentId = parent.ID;
            newObj.ID = population.GenerateMemberId(parent);
            parent.Ofsprings++;
            return newObj;
        }

        private void AddPopulationMember(Population population, ISmartObject newObj)
        {
            lsObjects.Add(newObj);
            population.Add(newObj);
            if (string.IsNullOrEmpty(newObj.ID) || newObj.ID == "0")
                newObj.ID = population.GenerateMemberId();
        }

        private void EnsureGoldenAgent(Population population)
        {
            if (population == null || !population.GoldenAgentEnabled)
                return;

            if (population.GoldenAgent != null && lsObjects.Contains(population.GoldenAgent))
                return;

            SpawnGoldenAgent(population);
        }

        private void SpawnGoldenAgent(Population population)
        {
            ISmartObject goldenAgent = CreatePopulationMember(population);
            if (Utils.MatchesStructure(population.GoldenAgentGene, population.NeuroNetTemplate))
                goldenAgent.NNetwork = NeuralNetworkFactory.GetInstance().Create(Utils.CloneGene(population.GoldenAgentGene));

            goldenAgent.ID = population.Name + "::Golden";
            goldenAgent.ParentId = "Golden";
            goldenAgent.Generation = population.GoldenAveragedNetworkCount;
            if (goldenAgent is SmartObject smart)
                smart.IsGoldenAgent = true;

            // The golden tint/glow is applied by ReconcileVisuals when its visual is created.
            population.GoldenAgent = goldenAgent;
            lsObjects.Add(goldenAgent);
        }

        private void RemoveGoldenAgent(Population population)
        {
            ISmartObject goldenAgent = population?.GoldenAgent;
            if (goldenAgent == null)
                return;

            QueueVisualRemoval(goldenAgent.VisibleShape);
            goldenAgent.VisibleShape = null;

            if (ReferenceEquals(SelectedObject, goldenAgent))
                SelectedObject = null;

            lsObjects.Remove(goldenAgent);
            goldenAgent.Dispose();
            population.GoldenAgent = null;
        }

        private void RefreshGoldenAgentNetwork(Population population)
        {
            if (population?.GoldenAgent == null || population.GoldenAgentGene == null)
                return;

            if (!Utils.MatchesStructure(population.GoldenAgentGene, population.NeuroNetTemplate))
            {
                population.ResetGoldenBrain();
                return;
            }

            population.GoldenAgent.NNetwork = NeuralNetworkFactory.GetInstance().Create(Utils.CloneGene(population.GoldenAgentGene));
            population.GoldenAgent.Generation = population.GoldenAveragedNetworkCount;
        }

        private bool TryUpdateGoldenAverage(Population population, ISmartObject source)
        {
            if (population == null || source == null || !population.TryAverageGoldenBrain(source))
                return false;

            RefreshGoldenAgentNetwork(population);
            if (population.GoldenAgent is SmartObject goldenSmart)
                goldenSmart.TriggerGoldenMergeFlash();

            population.Stats?.RecordGoldenAverage(
                CycleCount, population.GoldenAveragedNetworkCount, source.ID, source.Cycles);
            return true;
        }

        private void ApplyGoldenVisual(ISmartObject goldenAgent)
        {
            if (goldenAgent?.VisibleShape == null)
                return;

            goldenAgent.VisibleShape.ToolTip = "Golden agent";
            if (goldenAgent.VisibleShape is Image image && image.Source != null)
            {
                image.Source = GoldenTintCache.GetTinted(image.Source);
                image.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gold,
                    BlurRadius = 8,
                    ShadowDepth = 0,
                    Opacity = 0.75
                };
            }
            else if (goldenAgent.VisibleShape is Shape shape)
            {
                shape.Fill = Brushes.Gold;
                shape.Stroke = Brushes.Yellow;
                shape.StrokeThickness = 2;
            }
        }

        // The factory methods below build model-only agents (no WPF). The visual is created
        // lazily on the UI thread by ReconcileVisuals so these can run on the simulation thread.
        private Bird NewBird(NeuroNetStructure NeuroNetTemplate, SolidColorBrush ColorBrush)
        {
            Bird newObj = new Bird(NeuroNetTemplate, ref randomInit);
            SetInitialAgentLocation(newObj);
            return newObj;
        }

        private Shark NewShark(NeuroNetStructure NeuroNetTemplate, SolidColorBrush ColorBrush)
        {
            Shark newObj = new Shark(NeuroNetTemplate, ref randomInit);
            SetInitialAgentLocation(newObj);
            return newObj;
        }

        private Frog NewFrog(NeuroNetStructure NeuroNetTemplate, SolidColorBrush ColorBrush)
        {
            Frog newObj = new Frog(NeuroNetTemplate, ref randomInit);
            SetInitialAgentLocation(newObj);
            return newObj;
        }

        /// <summary>Random spawn position near the target, using the cached canvas size (no WPF reads).</summary>
        private void SetInitialAgentLocation(ISmartObject newObj)
        {
            double size = Target?.Size ?? dblTargetSize;
            double initLocationX = (canvasWidth / 2) + NextRandom(0, (int)size) - (size / 2);
            double initLocationY = (canvasHeight / 2) + NextRandom(0, (int)size) - (size / 2);
            newObj.SetLocation(initLocationX, initLocationY);
        }

        private void DisposeObject(ISmartObject obj)
        {
            if (obj == null)
                return;

            Population goldenPopulation = lsPopulations.FirstOrDefault(pop => ReferenceEquals(pop.GoldenAgent, obj));
            if (goldenPopulation != null)
            {
                goldenPopulation.Stats?.RecordGoldenDeath(CycleCount, obj.Cycles);
                RemoveGoldenAgent(goldenPopulation);
                if (goldenPopulation.GoldenAgentEnabled)
                    EnsureGoldenAgent(goldenPopulation);
                return;
            }

            {
                QueueVisualRemoval(obj.VisibleShape);
                obj.VisibleShape = null;


                /// Add gene to the list of the best genes in the population if it is good.
                foreach (Population pop in lsPopulations)
                {
                    if (!pop.Members.Contains(obj))
                        continue;

                    TryUpdateGoldenAverage(pop, obj);

                    PopulationArchive.Add(pop, obj);

                        // Binary search insert to keep list sorted descending — avoids full re-sort

                    //remove the worst of the best. This also will shrink lsBestGenes after population resizing.
                    // List is kept sorted descending, so worst entries are at the end
                }

                //remove from lists
                lsObjects.Remove(obj);
                foreach (Population pop in lsPopulations)
                    pop.Members.Remove(obj);
                obj.Dispose();
                obj = null;
            }
        }

        private void EnsureVisualForObject(ISmartObject smartObject, Population populationHint = null)
        {
            if (smartObject == null || smartObject.VisibleShape != null)
                return;

            if (smartObject is Frog)
            {
                smartObject.VisibleShape = CreateNewFrogImage("frog9_64.png");
            }
            else if (smartObject is Bird)
            {
                smartObject.VisibleShape = CreateNewBirdImage();
            }
            else if (smartObject is Shark)
            {
                smartObject.VisibleShape = CreateNewSharkImage();
            }
            else
            {
                Population population = populationHint ?? lsPopulations.FirstOrDefault(p => p.Members.Contains(smartObject));
                SolidColorBrush brush = population != null
                    ? population.PopulationColorBrush
                    : Brushes.SteelBlue;
                smartObject.VisibleShape = CreateNewTrianglePolygon(brush);
            }

            if (smartObject.VisibleShape != null)
            {
                smartObject.VisibleShape.MouseDown += ObjectInterface_MouseDown;
                shapeToObjectMap[smartObject.VisibleShape] = smartObject;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="FillBrush">Poplations use shared brush</param>
        /// <returns></returns>
        private FrameworkElement CreateNewShape(SolidColorBrush FillBrush)
        {
            // Create a   Ellipse.
            Ellipse myEllipse = new Ellipse();
            myEllipse.Fill = FillBrush;
            myEllipse.StrokeThickness = 1;
            myEllipse.Stroke = Brushes.WhiteSmoke;

            // Set the width and height of the Ellipse.
            myEllipse.Width = 20;
            myEllipse.Height = 15;

            panlUniverseView.Children.Add(myEllipse);
            return myEllipse;
        }

        private FrameworkElement CreateNewBirdImage()
        {
            Image dynamicImage = new Image();
            dynamicImage.Source = BirdSheetCache.Frame(0);
            dynamicImage.Width = 40;
            dynamicImage.Height = 40;
            dynamicImage.RenderTransformOrigin = new Point(0.5, 0.5);

            panlUniverseView.Children.Add(dynamicImage);
            Canvas.SetZIndex(dynamicImage, ZIndexBird);

            Canvas.SetTop(dynamicImage, 67);
            Canvas.SetLeft(dynamicImage, 66);

            return dynamicImage;
        }

        private FrameworkElement CreateNewSharkImage()
        {
            Image dynamicImage = new Image();
            dynamicImage.Source = SharkSpriteCache.Frame(0);
            dynamicImage.Width = 50;
            dynamicImage.Height = 50;
            dynamicImage.Opacity = 0.85;
            dynamicImage.RenderTransformOrigin = new Point(0.5, 0.5);

            panlUniverseView.Children.Add(dynamicImage);
            Canvas.SetZIndex(dynamicImage, ZIndexShark); // under water — beneath rafts and birds

            Canvas.SetTop(dynamicImage, 67);
            Canvas.SetLeft(dynamicImage, 66);

            return dynamicImage;
        }

        /// <summary>
        /// Creates the animated raft visual from the raft sprite sheet, drawn above sharks but
        /// below the frogs/birds that ride on it.
        /// </summary>
        private FrameworkElement CreateRaftImage()
        {
            Image dynamicImage = new Image();
            dynamicImage.Source = RaftSheetCache.Frame(0);
            dynamicImage.Width = 200;
            dynamicImage.Height = 200;
            dynamicImage.RenderTransformOrigin = new Point(0.5, 0.5);

            panlUniverseView.Children.Add(dynamicImage);
            Canvas.SetZIndex(dynamicImage, ZIndexRaft);

            Canvas.SetTop(dynamicImage, 67);
            Canvas.SetLeft(dynamicImage, 66);

            return dynamicImage;
        }

        private FrameworkElement CreateNewFrogImage(String imageFile = "frog9_64.png")
        {
            // Create Image and set its width and height  
            Image dynamicImage = new Image();

            // Set Image.Source
            dynamicImage.Source = FrogSheetCache.Frame(0);
            // scale dynamicImage to half size
            dynamicImage.Width = 32;
            dynamicImage.Height = 32;
            dynamicImage.RenderTransformOrigin = new Point(0.5, 0.5);

            // Add Image to Window
            panlUniverseView.Children.Add(dynamicImage);
            Canvas.SetZIndex(dynamicImage, ZIndexFrog);

            Canvas.SetTop(dynamicImage, 67);
            Canvas.SetLeft(dynamicImage, 66);

            return dynamicImage;
        }

        private Polygon CreateNewTrianglePolygon(SolidColorBrush FillBrush)
        {
            // Add the Polygon Element
            Polygon myPolygon = new Polygon();
            myPolygon.Stroke = System.Windows.Media.Brushes.Gold;
            myPolygon.Fill = FillBrush;
            myPolygon.StrokeThickness = 1;
            myPolygon.HorizontalAlignment = HorizontalAlignment.Center;
            myPolygon.VerticalAlignment = VerticalAlignment.Center;
            System.Windows.Point Point1 = new System.Windows.Point(0, -15);
            System.Windows.Point Point2 = new System.Windows.Point(8, 8);
            System.Windows.Point Point3 = new System.Windows.Point(-8, 8);
            PointCollection myPointCollection = new PointCollection();
            myPointCollection.Add(Point1);
            myPointCollection.Add(Point2);
            myPointCollection.Add(Point3);
            myPolygon.Points = myPointCollection;
            panlUniverseView.Children.Add(myPolygon);
            return myPolygon;
        }

        private static Type GetObjectTypeForBeing(PopulationBeing being)
        {
            switch (being)
            {
                case PopulationBeing.Bird: return typeof(Bird);
                case PopulationBeing.Shark: return typeof(Shark);
                default: return typeof(Frog);
            }
        }

        private ISmartObject CreatePopulationMember(Population population)
        {
            switch (population.Being)
            {
                case PopulationBeing.Bird:
                    return NewBird(population.NeuroNetTemplate, population.PopulationColorBrush);
                case PopulationBeing.Shark:
                    return NewShark(population.NeuroNetTemplate, population.PopulationColorBrush);
                default:
                    return NewFrog(population.NeuroNetTemplate, population.PopulationColorBrush);
            }
        }
    }
}
