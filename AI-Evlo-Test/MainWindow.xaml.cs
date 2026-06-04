using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using ArtificialNeuralNetwork.Factories;
using ArtificialNeuralNetwork.Genes;
using ArtificialNeuralNetwork.WeightInitializer;
using AI_Evlo_Test.Extentions;
using AI_Evlo_Test.ConfigLib;
using System.Collections.ObjectModel;
using AI_Evlo_Test.Enumerators;
using System.IO;
using Newtonsoft.Json;

namespace AI_Evlo_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly Random Rnd = new Random(DateTime.Now.DayOfYear * 1000 + DateTime.Now.Millisecond);
        static readonly object RndLock = new object();
        RandomWeightInitializer randomInit = new RandomWeightInitializer(Rnd);
        DispatcherTimer Clock = new DispatcherTimer();
        DateTime dtLastLabelsUpdate = DateTime.Now;

        //UI definitions
        Line lineToTarget;
        RayVisualizer rayVisualizer;

        //Environment definitions
        EEnvironmentType eEnvironmentType = EEnvironmentType.TwoTargets;
        int ObjectsIdCounter = 1; //counts created objects and stores the last used onject ID
        int CycleCount = 0;
        int lastCpsCheckCycle = 0;
        DateTime lastCpsCheckTime = DateTime.Now;
        double cyclesPerSecond = 0;
        const double dblTargetSize = 200;
        ISmartObject SelectedObject = null;
        Population _selectedPopulation = null;
        List<ISmartObject> lsObjects = new List<ISmartObject>();
        Dictionary<FrameworkElement, ISmartObject> shapeToObjectMap = new Dictionary<FrameworkElement, ISmartObject>();
        public List<Population> lsPopulations { get; set; } = new List<Population>();
        EvolutionChember evoChember = new EvolutionChember();
        TargetObj Target { get => Targets.FirstOrDefault(); }
        List<TargetObj> Targets = new List<TargetObj>();

        public Population SelectedPopulation
        {
            get => _selectedPopulation;
            set
            {
                _selectedPopulation = value;
                btnPopulationUpdate.IsEnabled = _selectedPopulation != null;
                if (_selectedPopulation == null)
                    return;

                if (_selectedPopulation.ObjectType == null)
                    _selectedPopulation.ObjectType = GetObjectTypeForBeing(_selectedPopulation.Being);

                // Always reflect the selected population's own size and brain in the editor controls,
                // so clicking Update applies the right values to the right population.
                txtPopulationSize.Text = _selectedPopulation.SizeLimit.ToString();

                string templateId = _selectedPopulation.NeuroNetTemplate?.Id;
                ListBoxItem brainItem = ddlPopulationNeuroNetType.Items
                    .OfType<ListBoxItem>()
                    .FirstOrDefault(it => it.Content.ToString() == templateId)
                    ?? ddlPopulationNeuroNetType.Items.OfType<ListBoxItem>().FirstOrDefault();
                ddlPopulationNeuroNetType.SelectedItem = brainItem;
                ddlPopulationNeuroNetType.UpdateLayout();

                SelectPopulationBeing(_selectedPopulation.Being);
                rectanglePopulationColor.Fill = new SolidColorBrush()
                { Color = _selectedPopulation.PopulationColor };
                groupBoxPopulation.BorderBrush = rectanglePopulationColor.Fill;
            }
        }

        bool boolTickCompleted = true;
        bool isHeadlessMode = false;
        int headlessBatchSize = 50;

        bool simulationRunning = false;
        // Max-speed mode: when the Speed slider is at the far right we drop the
        // DispatcherTimer (capped at the ~60 Hz system timer resolution) and pump the
        // simulation continuously through the dispatcher instead — no artificial cap.
        bool runAtMaxSpeed = false;
        bool maxPumpScheduled = false;
        // When true, agents skip rendering this tick. Used to fast-forward the
        // intermediate ticks of a max-speed batch and paint only the final state.
        bool suppressAgentRender = false;
        const int MaxSpeedVisibleBatch = 12;

        // Single reusable instance of the population list window — never open two.
        private PopulationList _populationListForm;
        public MainWindow()
        {

            InitializeComponent();
            LoadMovementSettings();
            Clock.Tick += new EventHandler(Clock_Tick);
            ApplyClockSpeed();
            lineToTarget = new Line
            {
                Stroke = Brushes.LightSteelBlue,
                StrokeThickness = 1
            };
            panlUniverseView.Children.Add(lineToTarget);

            evoChember.NewMessage += EvoChember_NewMessage;
            ddlPopulationNeuroNetType.SelectedIndex = 0;
            ddlPopulationBeing.SelectedIndex = 0;

            ddlPopulationName.ItemsSource = lsPopulations;
            ddlPopulationName.DisplayMemberPath = "Name";
            ddlPopulationName.SelectedValuePath = "ID";
            ddlPopulationName.UpdateLayout();

            // Restore last window size
            if (Objects.WindowBoundsStore.TryGet("MainWindow", out double w, out double h))
            {
                Width = w;
                Height = h;
            }
            //this.Hide();
        }

        // Timer-driven loop for every speed except the far-right "max" setting.
        private void Clock_Tick(object sender, EventArgs e)
        {
            if (boolTickCompleted == false)
                return;

            boolTickCompleted = false;
            RunSimulationBatch(isHeadlessMode ? headlessBatchSize : 1, renderOnlyLast: false);
            boolTickCompleted = true;
        }

        // Continuous max-speed pump. Re-posts itself at Background priority so it runs
        // flat-out when the dispatcher is idle but still yields to input/painting, and is
        // not throttled by the DispatcherTimer's ~60 Hz resolution.
        private void PumpMaxSpeed()
        {
            if (!simulationRunning || !runAtMaxSpeed)
            {
                maxPumpScheduled = false;
                return;
            }

            int iterations = isHeadlessMode ? headlessBatchSize : MaxSpeedVisibleBatch;
            RunSimulationBatch(iterations, renderOnlyLast: !isHeadlessMode);
            Dispatcher.BeginInvoke(new Action(PumpMaxSpeed), DispatcherPriority.Background);
        }

        private void StartMaxPump()
        {
            if (maxPumpScheduled)
                return;

            maxPumpScheduled = true;
            Dispatcher.BeginInvoke(new Action(PumpMaxSpeed), DispatcherPriority.Background);
        }

        /// <summary>
        /// Runs <paramref name="iterations"/> simulation ticks then refreshes UI once.
        /// When <paramref name="renderOnlyLast"/> is set, agents skip rendering on all but
        /// the final tick so a batch fast-forwards rather than painting every step.
        /// </summary>
        private void RunSimulationBatch(int iterations, bool renderOnlyLast)
        {
            for (int tick = 0; tick < iterations; tick++)
            {
                suppressAgentRender = renderOnlyLast && tick < iterations - 1;
                SimulationTick();
            }
            suppressAgentRender = false;

            UpdateLabbels();

            if (!isHeadlessMode)
            {
                UpdateRaftAnimation();

                // Draw line to selected agent
                if (eEnvironmentType == EEnvironmentType.OneTarget)
                {
                    if (SelectedObject != null && SelectedObject.HP > 0)
                        drawLine(SelectedObject.Location, Target.Location);
                    else
                        drawLine(Target.Location, Target.Location);
                }
            }
        }

        private void EvoChember_NewMessage(string Message)
        {
            Log(Message);
        }

        /// <summary>
        /// Handler of Object event changing location
        /// </summary>
        /// <param name="objWithNeuroNet"></param>
        /// <param name="ObjectLocation"></param>
        private void ObjectMoved(IBasicObject objWithNeuroNet, Point ObjectLocation)
        {
            DrawImage(objWithNeuroNet.VisibleShape, ObjectLocation);
        }

        private static void DrawImage(FrameworkElement ShapeOfObject, Point ObjecLocation)
        {
            double shapeWidth = ShapeOfObject.ActualWidth > 0 ? ShapeOfObject.ActualWidth : ShapeOfObject.Width;
            double shapeHeight = ShapeOfObject.ActualHeight > 0 ? ShapeOfObject.ActualHeight : ShapeOfObject.Height;

            if (double.IsNaN(shapeWidth) || shapeWidth < 0)
                shapeWidth = 0;
            if (double.IsNaN(shapeHeight) || shapeHeight < 0)
                shapeHeight = 0;

            double ImageLocationX = ObjecLocation.X - (shapeWidth / 2);
            double ImageLocationY = ObjecLocation.Y - (shapeHeight / 2);
            Canvas.SetTop(ShapeOfObject, ImageLocationY);
            Canvas.SetLeft(ShapeOfObject, ImageLocationX);
        }

        void drawLine(Point Location1, Point Location2)
        {
            lineToTarget.X1 = Location1.X;
            lineToTarget.Y1 = Location1.Y;
            lineToTarget.X2 = Location2.X;
            lineToTarget.Y2 = Location2.Y;
        }

        private void btnMutate_Click_1(object sender, RoutedEventArgs e)
        {
            MutateNN();
        }

        private void MutateNN()
        {
            if (SelectedObject == null || SelectedObject.NNetwork == null)
            {
                Log("No agent is selected.");
                return;
            }

            SelectedObject.NNetwork = evoChember.MutateNN(SelectedObject.NNetwork, 1);

            SelectedObject.NNetwork.Process();
            double[] dblOutputs = SelectedObject.NNetwork.GetOutputs();
            string strOutpust = String.Join(",", dblOutputs);

            Log($"NN mutated. New Ouput: {strOutpust}");
            Log($"Hiden layers: {SelectedObject.NNetwork.HiddenLayers.Count}" +
                $" Neurons: {SelectedObject.NNetwork.HiddenLayers[0].NeuronsInLayer.Count}");
        }

        private void TxtLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtLog.CaretIndex = txtLog.Text.Length;
            txtLog.ScrollToEnd();
        }

        private void BtnNewPopulation_Click(object sender, RoutedEventArgs e)
        {
            // Capture user choices before any event-driven overwriting
            string chosenNNType = (ddlPopulationNeuroNetType.SelectedItem as ListBoxItem)?.Content.ToString() ?? "Small";
            PopulationBeing chosenBeing = GetSelectedPopulationBeing();
            int chosenSize = int.TryParse(txtPopulationSize.Text, out int parsed) ? parsed : 1;

            //Prepare the name of the population
            if (ddlPopulationName.Text.Trim() == "")
                ddlPopulationName.Text = "Ppl_" + (lsPopulations.Count + 1).ToString();
            int iCnt = 1;
            while (lsPopulations.Exists(p => p.Name == ddlPopulationName.Text))
            {
                ddlPopulationName.Text = "Ppl_" + (iCnt++).ToString();
            }
            // create and add members
            Population newPopulation = CreatePopulation(chosenSize, ddlPopulationName.Text.Trim(), chosenNNType, chosenBeing);
            RegisterPopulation(newPopulation);
        }

        /// <summary>
        /// Wires a freshly created (or restored) population into the UI: list, dropdown,
        /// info label, and selection. Shared by the "Create New" button and the auto-seeder.
        /// </summary>
        private void RegisterPopulation(Population newPopulation)
        {
            if (newPopulation == null)
                return;

            newPopulation.StartingCycle = CycleCount;
            lsPopulations.Add(newPopulation);
            if (lsObjects.Count > 0)
                SelectObject(lsObjects.Last());
            Log($"{GetPopulationBeingName(newPopulation.Being)} population of {newPopulation.SizeLimit} created. Total:{lsObjects.Count} " +
                $" Hiddden Layers:{newPopulation.NeuroNetTemplate.HiddenLayers}" +
                $" Neurons per layer:{newPopulation.NeuroNetTemplate.NeuronsInHiddenLayer}");

            // Update ComboBox
            UpdatePopulationsDDL(newPopulation);

            // create the population card
            PopulationCard card = BuildPopulationCard(newPopulation);
            StackPnlPopulations.Children.Add(card.Root);
            lsPopuCards.Add(card);

            EnsureGoldenAgent(newPopulation);
        }

        /// <summary>Holds the visual elements of one population card so they can be updated cheaply.</summary>
        private class PopulationCard
        {
            public Border Root;
            public System.Windows.Shapes.Rectangle ColorStripe;
            public Image Icon;
            public TextBlock Title;
            public TextBlock Stats;
        }

        private ImageSource SpeciesIconForBeing(PopulationBeing being)
        {
            switch (being)
            {
                case PopulationBeing.Bird: return BirdSheetCache.Frame(0);
                case PopulationBeing.Shark: return SharkSpriteCache.Frame(0);
                default: return FrogSheetCache.Frame(0);
            }
        }

        private PopulationCard BuildPopulationCard(Population pop)
        {
            var stripe = new System.Windows.Shapes.Rectangle
            {
                Width = 6,
                Fill = new SolidColorBrush(pop.PopulationColor),
                RadiusX = 2,
                RadiusY = 2
            };

            var icon = new Image
            {
                Width = 38,
                Height = 38,
                Source = SpeciesIconForBeing(pop.Being),
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);

            var iconHolder = new Border
            {
                Width = 42,
                Height = 42,
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x24, 0x3A)), // dark navy — same tone as canvas, makes light birds visible
                Margin = new Thickness(4, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = icon
            };
            Grid.SetColumn(iconHolder, 1);

            var title = new TextBlock { FontWeight = FontWeights.Bold, FontSize = 11.5, Text = pop.Name };
            var stats = new TextBlock { FontSize = 9.5, Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x52, 0x60)), Text = "collecting data…", TextWrapping = TextWrapping.NoWrap };
            var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            textPanel.Children.Add(title);
            textPanel.Children.Add(stats);
            Grid.SetColumn(textPanel, 2);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.Children.Add(stripe);
            grid.Children.Add(iconHolder);
            grid.Children.Add(textPanel);

            var root = new Border
            {
                Width = 272,
                CornerRadius = new CornerRadius(5),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Background = Brushes.White,
                Margin = new Thickness(1, 2, 0, 0),
                Padding = new Thickness(0, 3, 3, 3),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = pop,
                Child = grid
            };

            root.MouseLeftButtonDown += PopulationCard_MouseLeftButtonDown;
            root.MouseRightButtonDown += PopulationCard_MouseRightButtonDown;
            root.MouseEnter += PopulationCard_MouseEnter;
            root.MouseLeave += PopulationCard_MouseLeave;

            return new PopulationCard { Root = root, ColorStripe = stripe, Icon = icon, Title = title, Stats = stats };
        }

        private static Population PopulationFromSender(object sender)
            => (sender as FrameworkElement)?.Tag as Population;

        private void PopulationCard_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border b) { b.BorderThickness = new Thickness(1); b.BorderBrush = Brushes.Gray; }
        }

        private void PopulationCard_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border b) { b.BorderThickness = new Thickness(2); b.BorderBrush = Brushes.DarkRed; }
        }

        private void PopulationCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Population selPopul = PopulationFromSender(sender);
            if (selPopul != null)
            {
                SelectedPopulation = selPopul;
                ddlPopulationName.SelectedValue = selPopul.ID;
            }
        }

        private void PopulationCard_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Population selPopul = PopulationFromSender(sender);
            if (selPopul == null)
                return;

            SelectedPopulation = selPopul;
            ddlPopulationName.SelectedValue = selPopul.ID;

            var menu = new System.Windows.Controls.ContextMenu();

            var itemInfo = new System.Windows.Controls.MenuItem { Header = "Population Info" };
            itemInfo.Click += (s, args) => ShowPopulationListForm(selPopul);
            menu.Items.Add(itemInfo);

            var itemGolden = new System.Windows.Controls.MenuItem
            {
                Header = selPopul.GoldenAgentEnabled ? "Disable Golden agent" : "Enable Golden agent"
            };
            itemGolden.Click += (s, args) => ToggleGoldenAgent(selPopul);
            menu.Items.Add(itemGolden);

            menu.Items.Add(new System.Windows.Controls.Separator());

            var itemDelete = new System.Windows.Controls.MenuItem { Header = "Delete" };
            itemDelete.Click += (s, args) => DeletePopulation(selPopul);
            menu.Items.Add(itemDelete);

            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;
            e.Handled = true;
        }

        private void ToggleGoldenAgent(Population population)
        {
            if (population == null)
                return;

            population.GoldenAgentEnabled = !population.GoldenAgentEnabled;
            if (population.GoldenAgentEnabled)
                EnsureGoldenAgent(population);
            else
                RemoveGoldenAgent(population);

            SaveSession();
            UpdateLabbels();
        }

        private void ShowPopulationListForm(Population population)
        {
            if (_populationListForm == null || !_populationListForm.Visible)
            {
                _populationListForm = new PopulationList();
                _populationListForm.FormClosed += (s, args) => _populationListForm = null;
            }
            _populationListForm.SetDataSource(population);
            _populationListForm.Show();
            _populationListForm.BringToFront();
        }

        private void DeletePopulation(Population population)
        {
            if (population == null)
                return;

            // Select this population so BtnNewObject_Copy_Click operates on it
            SelectedPopulation = population;
            BtnNewObject_Copy_Click(null, null);
        }

        private void UpdatePopulationsDDL(Population newPopulation)
        {
            ddlPopulationName.ItemsSource = null;
            ddlPopulationName.ItemsSource = lsPopulations;
            ddlPopulationName.DisplayMemberPath = "Name";
            ddlPopulationName.SelectedValuePath = "ID";
            ddlPopulationName.UpdateLayout();
            if (newPopulation != null)
                SelectedPopulation = newPopulation;
        }

        private void SelectObject(ISmartObject objNN)
        {
            if (objNN == null || objNN.NNetwork == null)
                return;
            ISmartObject OldSelected = SelectedObject;
            SelectedObject = objNN;

            // Reset ray visualizer when switching agents
            if (rayVisualizer != null)
                rayVisualizer.Clear();

            // Update selected visuals. TODO  move it soemthing else
            Shape objEllipse;

            //restore unselected object
            if (OldSelected != null && OldSelected.VisibleShape is Shape)
            {
                objEllipse = (Shape)OldSelected.VisibleShape;
                if (objEllipse != null)
                {
                    objEllipse.Stroke = Brushes.SteelBlue;
                    objEllipse.StrokeThickness = 1;
                }
            }
            if (objNN.VisibleShape is Shape)
            {
                objEllipse = (Shape)objNN.VisibleShape;
                objEllipse.StrokeThickness = 3;
                objEllipse.Stroke = Brushes.White;
            }

            // Refresh the Selected Agent panel (icon + brain + stats)
            UpdateSelectedAgentVisual();
            UpdateSelectedAgentStats();
        }

        private void ObjectInterface_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var senderElement = sender as FrameworkElement;
            if (senderElement == null)
                return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (shapeToObjectMap.TryGetValue(senderElement, out ISmartObject newSelectedObject))
                    SelectObject(newSelectedObject);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if (shapeToObjectMap.TryGetValue(senderElement, out ISmartObject objClicked) && objClicked != null)
                {
                    string dif = Utils.GetDifferences(objClicked.NNetwork.GetGenes(), SelectedObject.NNetwork.GetGenes());
                    dif = $"Comparing obj{objClicked.ID} to selected obj{SelectedObject.ID} " + Environment.NewLine + dif;
                    Log(dif);
                }
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!simulationRunning)
                StartSimulation();
            else
                StopSimulation();
        }

        private void Log(string msg)
        {
            txtLog.AppendText(Environment.NewLine + msg);
        }

        private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ApplyClockSpeed();
        }

        /// <summary>
        /// Maps the "Speed" slider to the run mode. Left = slow, right = fast, so the timer
        /// interval is the inverse of the value (1 ms floor). At the far-right ("max") the
        /// timer is dropped entirely in favour of the continuous pump.
        /// </summary>
        private void ApplyClockSpeed()
        {
            if (sliderSpeed == null)
                return;

            int interval = (int)Math.Max(1, sliderSpeed.Maximum - sliderSpeed.Value);
            Clock.Interval = new TimeSpan(0, 0, 0, 0, interval);

            bool atMax = sliderSpeed.Value >= sliderSpeed.Maximum;
            if (atMax == runAtMaxSpeed)
                return;

            runAtMaxSpeed = atMax;

            // If running, hand the live simulation over to the other driver immediately.
            if (simulationRunning)
            {
                Clock.Stop();           // the pump self-stops via the runAtMaxSpeed flag
                StartActiveLoop();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        private void PanlUniverseView_Loaded(object sender, RoutedEventArgs e)
        {
            InitTargets();
            rayVisualizer = new RayVisualizer(panlUniverseView, 8);
            RestoreOrSeedDefaultScenario();
        }

        private void BtnDeleteObject_Click(object sender, RoutedEventArgs e)
        {
            DisposeObject(SelectedObject);
            if (lsObjects.Count > 0)
                SelectObject(lsObjects.First());
        }

        private void BtnVisualNet_Click_1(object sender, RoutedEventArgs e)
        {
            VisualizeNetwork formVisualNet = new VisualizeNetwork();
            formVisualNet.ParentFormNN = this;
            formVisualNet.VisualizerSendMessage += FormVisualNet_VisualizerSendMessage;
            formVisualNet.Show();
            if (SelectedObject != null && SelectedObject.NNetwork != null)
            {
                formVisualNet.ShowNNet(SelectedObject.NNetwork);
                formVisualNet.Status = "Neural network of the selected object is loaded.";
            }
            else
                formVisualNet.Status = "No neural network is loaded.";
        }

        private void FormVisualNet_VisualizerSendMessage(string Message)
        {
            Log(Message);
        }

        private void TxtCreateObjectsNumber_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void DdlPopulationName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ddlPopulationName.SelectedItem != null)
            {
                Log(ddlPopulationName.SelectedItem.ToString() + ":" + ddlPopulationName.SelectedValue.ToString());
                SelectedPopulation = lsPopulations.FirstOrDefault(p => p.ID == ddlPopulationName.SelectedValue.ToString());
            }
        }

        private void BtnPopulationUpdate_Click(object sender, RoutedEventArgs e)
        {
            // Update only the population that is currently selected.
            Population pop = _selectedPopulation;
            if (pop == null)
                return;

            if (int.TryParse(txtPopulationSize.Text, out int intSize) && intSize > 0)
                pop.SizeLimit = intSize;

            // Read the brain size from the actual selected item (not the unreliable .Text)
            string nnType = (ddlPopulationNeuroNetType.SelectedItem as ListBoxItem)?.Content?.ToString();
            NeuroNetStructure newTemplate;
            switch (nnType)
            {
                case "Medium": newTemplate = NeuroNetStructure.Mid_3Lx10N(); break;
                case "Large": newTemplate = NeuroNetStructure.Big_5Lx20N(); break;
                default: newTemplate = NeuroNetStructure.Small_1Lx9N(); break;
            }
            bool brainChanged = pop.NeuroNetTemplate == null || pop.NeuroNetTemplate.Id != newTemplate.Id;
            pop.NeuroNetTemplate = newTemplate;

            PopulationBeing newBeing = GetSelectedPopulationBeing();
            bool beingChanged = pop.Being != newBeing;
            pop.Being = newBeing;
            pop.ObjectType = GetObjectTypeForBeing(newBeing);

            // A different brain topology or species can't reuse the old genes/agents, so rebuild
            // this population's members from scratch with the new template.
            if (brainChanged || beingChanged)
            {
                RebuildPopulationMembers(pop);
                Log($"'{pop.Name}' rebuilt with {nnType ?? "Small"} brain ({GetPopulationBeingName(pop.Being)}). " +
                    "Previous evolution for this population was reset.");
            }

            SaveSession();
        }

        /// <summary>
        /// Disposes a population's current members and archived genes and grows a fresh set from
        /// its (new) NeuroNetTemplate / Being. Used when the brain size or species changes.
        /// </summary>
        private void RebuildPopulationMembers(Population pop)
        {
            bool selectedWasInPop = SelectedObject != null && pop.Members.Contains(SelectedObject);

            RemoveGoldenAgent(pop);

            for (int i = pop.Members.Count - 1; i >= 0; i--)
                DisposeObject(pop.Members[i]);

            pop.Members.Clear();
            pop.lsBestGenes.Clear(); // old genes are a different topology/species now
            pop.ResetGoldenBrain();

            FillPopulationImmediate(pop, useArchive: false);
            EnsureGoldenAgent(pop);

            // The previously selected agent may have just been disposed — reselect cleanly.
            if (selectedWasInPop)
            {
                SelectedObject = null;
                if (pop.Members.Count > 0)
                    SelectObject(pop.Members[0]);
                else
                {
                    UpdateSelectedAgentVisual();
                    UpdateSelectedAgentStats();
                }
            }
        }

        private PopulationBeing GetSelectedPopulationBeing()
        {
            string beingName = (ddlPopulationBeing.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.Equals(beingName, "Birds", StringComparison.OrdinalIgnoreCase))
                return PopulationBeing.Bird;
            if (string.Equals(beingName, "Sharks", StringComparison.OrdinalIgnoreCase))
                return PopulationBeing.Shark;
            return PopulationBeing.Frog;
        }

        private void SelectPopulationBeing(PopulationBeing being)
        {
            foreach (ComboBoxItem item in ddlPopulationBeing.Items)
            {
                if (item.Content?.ToString() == GetPopulationBeingName(being))
                {
                    ddlPopulationBeing.SelectedItem = item;
                    ddlPopulationBeing.UpdateLayout();
                    return;
                }
            }
        }

        private static string GetPopulationBeingName(PopulationBeing being)
        {
            switch (being)
            {
                case PopulationBeing.Bird: return "Birds";
                case PopulationBeing.Shark: return "Sharks";
                default: return "Frogs";
            }
        }

        private void BtnNewObject_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPopulation == null)
                return;

            int populationIndex = lsPopulations.FindIndex(p => p.ID == _selectedPopulation.ID);

            RemoveGoldenAgent(_selectedPopulation);

            for (int i = _selectedPopulation.Members.Count - 1; i > -1; i--)
            {
                DisposeObject(_selectedPopulation.Members[i] as SmartObject);
            }

            Population newSelectedPopul = lsPopulations.FirstOrDefault(p => p.ID != _selectedPopulation.ID);
            lsPopulations.Remove(_selectedPopulation);
            _selectedPopulation.Members.Clear();
            _selectedPopulation = null;

            if (populationIndex >= 0 && populationIndex < lsPopuCards.Count)
            {
                PopulationCard delCard = lsPopuCards[populationIndex];
                StackPnlPopulations.Children.Remove(delCard.Root);
                lsPopuCards.RemoveAt(populationIndex);
            }

            // Persist the new set immediately so the deleted population does not reload next launch,
            // even if the app later exits without firing the Closing handler.
            SaveSession();

            lblPopulationInfo.Content = "Population:";

            UpdatePopulationsDDL(newSelectedPopul);
        }

        private void WindowEnvirnoment_MouseMove(object sender, MouseEventArgs e)
        {
        }

        private static double NextRandomDouble()
        {
            lock (RndLock)
                return Rnd.NextDouble();
        }

        private static int NextRandom(int minValue, int maxValue)
        {
            lock (RndLock)
                return Rnd.Next(minValue, maxValue);
        }

        private void DdlEnvirnoment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem SelectedItem = (ComboBoxItem)ddlEnvirnoment.SelectedValue;
            if (SelectedItem.Content.ToString() == "Food is moving" && eEnvironmentType != EEnvironmentType.OneTarget)
                eEnvironmentType = EEnvironmentType.OneTarget;
            else if (SelectedItem.Content.ToString().StartsWith("One raft") && eEnvironmentType != EEnvironmentType.TwoTargets)
                eEnvironmentType = EEnvironmentType.TwoTargets;
            InitTargets();
        }

        private void ImgHelp_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (eEnvironmentType == EEnvironmentType.OneTarget)
            {
                Log("environment: 'Food is moving'.  Description:");
                Log("The large circle is source of food. Agents has to go over it, to gain HP. When agents are outside of the food they gradualy loose HP. The food source changes direction and  speed when it bounces of the borders and changes size while moving.");
            }

            else if (eEnvironmentType == EEnvironmentType.TwoTargets)
            {
                Log("Environment: 'One raft can't take them all'.  Description:");
                Log("Agents can swim around in the water. They get tired after while and sink. If they go to one of the 2 rafts, they rest and restore HP. HP is restored 4 times faster than it is lost. If one third of the frog population is on one raft, the raft goes under water and agents no longer restore HP there. ");
                Log(" When less than one third of the frog population is on the top of the sunked raft, it comes back to the surfice. ");
            }
        }


        bool StopTargets = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StopTargets = !StopTargets;
            btnStopTargets.Content = StopTargets ? "Release rafts" : "Anchor rafts";
        }

        private void BtnMovementSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new MovementSettingsWindow(SmartObject.MovementSettings)
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() == true)
            {
                SmartObject.MovementSettings = settingsWindow.Settings;
                SaveMovementSettings();
                Log("Simulation parameters saved to " + MovementSettingsFilePath);
            }
        }

        private void ChkHeadless_Changed(object sender, RoutedEventArgs e)
        {
            isHeadlessMode = chkHeadless.IsChecked == true;
            if (!isHeadlessMode)
            {
                // Re-render all objects when switching back to visual mode
                foreach (ISmartObject smartObject in lsObjects)
                {
                    Population population = lsPopulations.FirstOrDefault(p => p.Members.Contains(smartObject));
                    EnsureVisualForObject(smartObject, population);

                    if (smartObject.VisibleShape != null)
                        DrawImage(smartObject.VisibleShape, smartObject.Location);
                }
                foreach (TargetObj target in Targets)
                {
                    if (target.VisibleShape != null)
                        DrawImage(target.VisibleShape, target.Location);
                }
            }
            Log(isHeadlessMode ? "Headless mode ON — rendering skipped for faster training" : "Headless mode OFF — rendering resumed");
        }

        BasicObject obj2 = new BasicObject() { ID = "01", Size = 20, };

        private void panlUniverseView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var targ in Targets)
            {
                if (targ.Trajectory is Path_spiral)
                {
                    (targ.Trajectory as Path_spiral).SpiralCenter.X = panlUniverseView.ActualWidth / 2;
                    (targ.Trajectory as Path_spiral).SpiralCenter.Y = panlUniverseView.ActualHeight / 2;
                    (targ.Trajectory as Path_spiral).goToCenterFirst = true;
                    if (panlUniverseView.ActualWidth / 2 > panlUniverseView.ActualHeight / 2)
                        (targ.Trajectory as Path_spiral).MaxSize = panlUniverseView.ActualHeight / 2;
                    else
                        (targ.Trajectory as Path_spiral).MaxSize = panlUniverseView.ActualWidth / 2;

                }
            }
        }

        private void windowEnvirnoment_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            simulationRunning = false;   // also halts the max-speed pump
            Clock.Stop();
            Objects.WindowBoundsStore.Save("MainWindow", ActualWidth, ActualHeight);
            SaveMovementSettings();
            SaveSession();
            lblStatusBar.Content = $"Saved {lsPopulations.Count} population(s).";
        }

        /// <summary>Per-user folder holding the last session.</summary>
        private static string SaveDirectory
        {
            get
            {
                string dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AI-Evlo", "populations");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>Single file that holds the whole session (the exact set of current populations).</summary>
        private static string SessionFilePath => System.IO.Path.Combine(SaveDirectory, "session.json");

        private static string MovementSettingsFilePath => System.IO.Path.Combine(SaveDirectory, "movement-settings.json");

        private void LoadMovementSettings()
        {
            try
            {
                SmartObject.MovementSettings = LoadMovementSettingsFromPath(MovementSettingsFilePath);
            }
            catch (Exception ex)
            {
                SmartObject.MovementSettings = new MovementSettings();
                Log("Could not load movement settings: " + ex.Message);
            }
        }

        private void SaveMovementSettings()
        {
            try
            {
                SaveMovementSettingsToPath(MovementSettingsFilePath, SmartObject.MovementSettings);
            }
            catch (Exception ex)
            {
                Log("Could not save movement settings: " + ex.Message);
            }
        }

        internal static MovementSettings LoadMovementSettingsFromPath(string filePath)
        {
            MovementSettings settings = File.Exists(filePath)
                ? ReadFromJsonFile<MovementSettings>(filePath)
                : new MovementSettings();

            if (settings == null)
                settings = new MovementSettings();

            settings.Normalize();
            return settings;
        }

        internal static void SaveMovementSettingsToPath(string filePath, MovementSettings settings)
        {
            MovementSettings normalized = (settings ?? new MovementSettings()).Clone();
            normalized.Normalize();

            string directory = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            WriteToJsonFile(filePath, normalized);
        }

        /// <summary>
        /// Writes the current populations to a single session file. Because it is one file that
        /// mirrors <see cref="lsPopulations"/> exactly, a deleted population can never linger and
        /// reload, and the saved set is always "what was loaded when the app closed".
        /// </summary>
        private void SaveSession()
        {
            try
            {
                WriteToJsonFile(SessionFilePath, lsPopulations.ToList());

                CleanupLegacyPopulationFiles(SaveDirectory);
            }
            catch (Exception ex)
            {
                Log("Could not save session: " + ex.Message);
            }
        }

        internal static void CleanupLegacyPopulationFiles(string directory)
        {
            foreach (string f in Directory.GetFiles(directory, "*.json"))
            {
                string fileName = System.IO.Path.GetFileName(f);
                bool isCurrentSessionFile = string.Equals(fileName, "session.json", StringComparison.OrdinalIgnoreCase);
                bool isMovementSettingsFile = string.Equals(fileName, "movement-settings.json", StringComparison.OrdinalIgnoreCase);
                if (!isCurrentSessionFile && !isMovementSettingsFile)
                    File.Delete(f);
            }
        }

        /// <summary>
        /// On first paint: restore the last saved session if any, otherwise seed the default
        /// scenario (2 rafts, 50 frogs, 10 birds with fresh random brains). Then start running.
        /// </summary>
        private void RestoreOrSeedDefaultScenario()
        {
            if (lsPopulations.Count > 0)
                return;

            int restored = TryRestoreSavedPopulations();
            if (restored > 0)
            {
                Log($"Welcome back — restored {restored} saved population(s) from your last session. Resuming evolution…");
                StartSimulation();
                return;
            }

            LoadDefaultScenario();
        }

        private int TryRestoreSavedPopulations()
        {
            int count = 0;
            try
            {
                List<Population> pops = null;
                if (File.Exists(SessionFilePath))
                    pops = ReadFromJsonFile<List<Population>>(SessionFilePath);

                // One-time migration: if there is no session file yet, gather legacy per-GUID files.
                if (pops == null || pops.Count == 0)
                    pops = LoadLegacyPopulationFiles();

                if (pops != null)
                {
                    foreach (Population pop in pops)
                    {
                        if (pop == null || pop.SizeLimit < 1)
                            continue;
                        try
                        {
                            RestorePopulation(pop);
                            count++;
                        }
                        catch (Exception ex)
                        {
                            Log($"Skipped a saved population: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Could not read saved session: " + ex.Message);
            }
            return count;
        }

        /// <summary>Reads populations from the old per-population GUID files (pre-session-file format).</summary>
        private List<Population> LoadLegacyPopulationFiles()
        {
            var list = new List<Population>();
            foreach (string file in Directory.GetFiles(SaveDirectory, "*.json"))
            {
                if (string.Equals(System.IO.Path.GetFileName(file), "session.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    Population p = ReadFromJsonFile<Population>(file);
                    if (p != null)
                        list.Add(p);
                }
                catch { /* skip unreadable legacy file */ }
            }
            return list;
        }

        /// <summary>Rebuilds runtime state for a deserialized population and grows live members from its archived genes.</summary>
        private void RestorePopulation(Population pop)
        {
            pop.ObjectType = GetObjectTypeForBeing(pop.Being);
            pop.NeuroNetTemplate = ResolveRestoredNeuroNetTemplate(pop);
            if (pop.Members == null)
                pop.Members = new List<ISmartObject>();
            pop.Members.Clear();
            pop.GoldenAgent = null;

            FillPopulationImmediate(pop, useArchive: true);
            RegisterPopulation(pop);
        }

        internal static NeuroNetStructure ResolveRestoredNeuroNetTemplate(Population pop)
        {
            NeuroNetStructure restored = NormalizeTemplate(pop?.NeuroNetTemplate);
            if (restored != null)
                return restored;

            if (pop?.lsBestGenes != null)
            {
                foreach (GenomeRecord record in pop.lsBestGenes)
                {
                    NeuroNetStructure inferred = NormalizeTemplate(NeuroNetStructure.FromGene(record?.Gene));
                    if (inferred != null)
                        return inferred;
                }
            }

            return NeuroNetStructure.Small_1Lx9N();
        }

        private static NeuroNetStructure NormalizeTemplate(NeuroNetStructure template)
        {
            if (template == null)
                return null;

            if (template.Id == "Small" || (template.HiddenLayers == 1 && template.NeuronsInHiddenLayer == 18))
                return NeuroNetStructure.Small_1Lx9N();
            if (template.Id == "Medium" || (template.HiddenLayers == 3 && template.NeuronsInHiddenLayer == 13))
                return NeuroNetStructure.Mid_3Lx10N();
            if (template.Id == "Large" || (template.HiddenLayers == 5 && template.NeuronsInHiddenLayer == 20))
                return NeuroNetStructure.Big_5Lx20N();

            return template;
        }

        private void LoadDefaultScenario()
        {
            Log("No saved populations found — setting up a fresh ecosystem.");

            if (eEnvironmentType != EEnvironmentType.TwoTargets)
            {
                eEnvironmentType = EEnvironmentType.TwoTargets;
                InitTargets();
            }

            Log("Seeding 50 frogs and 10 birds with brand-new random brains on 2 rafts…");
            SeedPopulation(50, "Frogs", "Small", PopulationBeing.Frog);
            SeedPopulation(10, "Birds", "Small", PopulationBeing.Bird);

            Log("Evolution begins now. Frogs must rest on rafts to survive; sharks hunt frogs in water, and hungry birds hunt sharks.");
            Log("Tip: click any agent to inspect its brain, or use the Populations panel to add a Sharks population.");
            StartSimulation();
        }

        /// <summary>Creates and registers a population with a name made unique against existing ones.</summary>
        private Population SeedPopulation(int size, string name, string nnType, PopulationBeing being)
        {
            string uniqueName = name;
            int n = 1;
            while (lsPopulations.Exists(p => p.Name == uniqueName))
                uniqueName = name + "_" + (n++);

            Population pop = CreatePopulation(size, uniqueName, nnType, being);
            RegisterPopulation(pop);
            return pop;
        }

        private void StartSimulation()
        {
            if (simulationRunning)
                return;

            simulationRunning = true;
            btnStart.Content = "⏸ Pause";    // pause glyph
            btnStart.Background = HpOrange;        // amber = running
            Log("Started at " + DateTime.Now.ToShortTimeString());
            StartActiveLoop();
        }

        private void StopSimulation()
        {
            if (!simulationRunning)
                return;

            simulationRunning = false;
            Clock.Stop();                          // the max-speed pump self-stops via the flag
            btnStart.Content = "▶ Start";    // play glyph
            btnStart.Background = HpGreen;         // green = ready
            Log($"Paused {DateTime.Now.ToShortTimeString()}");
        }

        /// <summary>Starts whichever driver matches the current speed setting.</summary>
        private void StartActiveLoop()
        {
            if (!simulationRunning)
                return;

            if (runAtMaxSpeed)
            {
                Clock.Stop();
                StartMaxPump();
            }
            else
            {
                Clock.Start();
            }
        }

        /// <summary>
        /// Writes the given object instance to a Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// <para>Only Public properties and variables will be written to the file. These can be any type though, even other classes.</para>
        /// <para>If there are public properties/variables that you do not want written to the file, decorate them with the [JsonIgnore] attribute.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            var jsonSettings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects };
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite, Formatting.Indented, jsonSettings);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        /// <summary>
        /// Reads an object instance from an Json file.
        /// <para>Object type must have a parameterless constructor.</para>
        /// </summary>
        /// <typeparam name="T">The type of object to read from the file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the Json file.</returns>
        public static T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(fileContents);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }

    }
}
