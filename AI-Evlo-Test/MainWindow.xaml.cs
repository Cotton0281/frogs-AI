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
                btnDeletePopulation.IsEnabled = _selectedPopulation != null;
                btnPopulationUpdate.IsEnabled = _selectedPopulation != null;
                if (_selectedPopulation == null)
                    return;

                if (_selectedPopulation.ObjectType == null)
                    _selectedPopulation.ObjectType = GetObjectTypeForBeing(_selectedPopulation.Being);

                foreach (ListBoxItem item in ddlPopulationNeuroNetType.Items)
                {
                    if (item.Content.ToString() == _selectedPopulation.NeuroNetTemplate.Id)
                    {
                        ddlPopulationNeuroNetType.SelectedItem = item;
                        ddlPopulationNeuroNetType.UpdateLayout();
                        txtPopulationSize.Text = _selectedPopulation.SizeLimit.ToString();
                    }
                }
                SelectPopulationBeing(_selectedPopulation.Being);
                rectanglePopulationColor.Fill = new SolidColorBrush()
                { Color = _selectedPopulation.PopulationColor };
                groupBoxPopulation.BorderBrush = rectanglePopulationColor.Fill;
            }
        }

        bool boolTickCompleted = true;
        bool isHeadlessMode = false;
        int headlessBatchSize = 50;
        public MainWindow()
        {

            InitializeComponent();
            Clock.Tick += new EventHandler(Clock_Tick);
            Clock.Interval = new TimeSpan(0, 0, 0, 0, (int)sliderSpeed.Value);
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
            //this.Hide();
        }

        private void Clock_Tick(object sender, EventArgs e)
        {
            if (boolTickCompleted == false)
                return;

            boolTickCompleted = false;

            int iterations = isHeadlessMode ? headlessBatchSize : 1;
            for (int tick = 0; tick < iterations; tick++)
            {
                SimulationTick();
            }

            UpdateLabbels();

            if (!isHeadlessMode)
            {
                // Draw line to selected agent
                if (eEnvironmentType == EEnvironmentType.OneTarget)
                {
                    if (SelectedObject != null && SelectedObject.HP > 0)
                        drawLine(SelectedObject.Location, Target.Location);
                    else
                        drawLine(Target.Location, Target.Location);
                }
            }

            boolTickCompleted = true;
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
            newPopulation.StartingCycle = CycleCount;

            lsPopulations.Add(newPopulation);
            SelectObject(lsObjects.Last());
            Log($"{GetPopulationBeingName(newPopulation.Being)} population of {newPopulation.SizeLimit} created. Total:{lsObjects.Count} " +
                $" Hiddden Layers:{newPopulation.NeuroNetTemplate.HiddenLayers}" +
                $" Neurons per layer:{newPopulation.NeuroNetTemplate.NeuronsInHiddenLayer}");

            // Update ComboBox
            UpdatePopulationsDDL(newPopulation);

            // create info label
            Label newLabel = new Label();
            newLabel.Width = lblPopulationInfo.Width;
            newLabel.Height = lblPopulationInfo.Height;
            newLabel.HorizontalAlignment = HorizontalAlignment.Left;
            newLabel.VerticalAlignment = VerticalAlignment.Top;
            newLabel.Name = "L" + newPopulation.ID.Replace("-", ""); // "lbl" + newPopulation.Name.Replace(" ", "_");
            StackPnlPopulations.Children.Add(newLabel);

            lsPopuLabels.Add(newLabel);
            double topMargin = lblPopulationInfo.Margin.Top + (lsPopuLabels.Count * 1.3 * newLabel.Height);
            newLabel.Margin = new Thickness(1, 2, 0, 0);
            newLabel.Background = new SolidColorBrush(newPopulation.PopulationColor);
            newLabel.BorderBrush = Brushes.LightSlateGray;
            newLabel.Content = "Collecting data...";
            newLabel.BorderThickness = new Thickness(1);
            newLabel.BorderBrush = Brushes.Gray;

            newLabel.MouseLeftButtonDown += NewLabel_MouseLeftButtonDown;
            newLabel.MouseEnter += PopulationLabel_mouseOver;
            newLabel.MouseLeave += PopulaionLabel_mouseLeaave;
        }

        private void PopulaionLabel_mouseLeaave(object sender, MouseEventArgs e)
        {
            (e.Source as Label).BorderThickness = new Thickness(1);
            (e.Source as Label).BorderBrush = Brushes.Gray;
        }

        private void PopulationLabel_mouseOver(object sender, MouseEventArgs e)
        {
            (e.Source as Label).BorderThickness = new Thickness(3);
            (e.Source as Label).BorderBrush = Brushes.DarkRed;
        }

        private void NewLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Label clickedLabel = (Label)sender;

            Population selPopul = lsPopulations.FirstOrDefault(p => "L" + p.ID.Replace("-", "") == clickedLabel.Name);
            if (selPopul != null)
            {
                SelectedPopulation = selPopul;
                ddlPopulationName.SelectedValue = selPopul.ID;
                PopulationList formPopulationList = new PopulationList();
                formPopulationList.Show();
                formPopulationList.SetDataSource(SelectedPopulation);
            }
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
            if (!Clock.IsEnabled)
            {
                Clock.Start();
                Log("Stared at " + DateTime.Now.ToShortTimeString());
                btnStart.Content = "Pause";
            }
            else
            {
                Clock.Stop();
                Log($"Paused {DateTime.Now.ToShortTimeString()}");
                btnStart.Content = "Start";
            }
        }

        private void Log(string msg)
        {
            txtLog.AppendText(Environment.NewLine + msg);
        }

        private void SliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Clock.Interval = new TimeSpan(0, 0, 0, 0, (int)sliderSpeed.Value);
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtLog.Clear();
        }

        private void PanlUniverseView_Loaded(object sender, RoutedEventArgs e)
        {
            InitTargets();
            rayVisualizer = new RayVisualizer(panlUniverseView, 8);
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
            if (_selectedPopulation == null)
                return;

            int intSize = 0;
            if (int.TryParse(txtPopulationSize.Text, out intSize))
            {
                _selectedPopulation.SizeLimit = intSize;
            }

            switch (ddlPopulationNeuroNetType.Text)
            {
                case "Small":
                    _selectedPopulation.NeuroNetTemplate = NeuroNetStructure.Small_1Lx9N();
                    break;
                case "Medium":
                    _selectedPopulation.NeuroNetTemplate = NeuroNetStructure.Mid_3Lx10N();
                    break;
                case "Large":
                    _selectedPopulation.NeuroNetTemplate = NeuroNetStructure.Big_5Lx20N();
                    break;
            }
            _selectedPopulation.Being = GetSelectedPopulationBeing();
            _selectedPopulation.ObjectType = GetObjectTypeForBeing(_selectedPopulation.Being);
            SavePopulations(_selectedPopulation);
        }

        private PopulationBeing GetSelectedPopulationBeing()
        {
            string beingName = (ddlPopulationBeing.SelectedItem as ComboBoxItem)?.Content?.ToString();
            return string.Equals(beingName, "Birds", StringComparison.OrdinalIgnoreCase)
                ? PopulationBeing.Bird
                : PopulationBeing.Frog;
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
            return being == PopulationBeing.Bird ? "Birds" : "Frogs";
        }

        private void BtnNewObject_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPopulation == null)
                return;

            int populationIndex = lsPopulations.FindIndex(p => p.ID == _selectedPopulation.ID);

            for (int i = _selectedPopulation.Members.Count - 1; i > -1; i--)
            {
                DisposeObject(_selectedPopulation.Members[i] as SmartObject);
            }

            Population newSelectedPopul = lsPopulations.FirstOrDefault(p => p.ID != _selectedPopulation.ID);
            lsPopulations.Remove(_selectedPopulation);
            _selectedPopulation.Members.Clear();
            _selectedPopulation = null;

            if (populationIndex >= 0 && populationIndex < lsPopuLabels.Count)
            {
                Label delLabel = lsPopuLabels[populationIndex];
                StackPnlPopulations.Children.Remove(delLabel);
                lsPopuLabels.Remove(delLabel);
            }

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
                Log("Agents can swim around in the water. They get tired after while and sink. If they go to one of the 2 rafts, they rest and restore HP. HP is restored 4 times faster than it is lost. If more that half of all agents are on one raft, the raft goes under water and agens no longer restore HP there. ");
                Log(" When less of one half of all agents are on the top of the sunked raft, it comes back to the surfice. ");
            }
        }


        bool StopTargets = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StopTargets = !StopTargets;
            btnStopTargets.Content = StopTargets ? "Release rafts" : "Anchor rafts";
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
            Clock.Stop();
            lblStatusBar.Content += $"Saving {lsPopulations.Count} populations.";
            foreach (Population po in lsPopulations)
            {
                SavePopulations(po);
            }
            lblStatusBar.Content += $"Populations saved.";
        }

        private void SavePopulations(Population objPopulation)
        {
            lblStatusBar.Content += $"Saving  population {objPopulation.ID}";
            string appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            WriteToJsonFile(appPath+"\\" + objPopulation.ID + ".json", (Population)objPopulation);
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
