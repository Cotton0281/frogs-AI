using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AI_Evlo_Test.Persistence;
using AI_Evlo_Test.Objects;
using AI_Evlo_Test.Enumerators;

namespace AI_Evlo_Test
{
    public partial class MainWindow
    {
        private void PopulationEditor_Expanded(object sender, RoutedEventArgs e)
        {
            if (FindName("inspectorPanel") is Expander inspector) inspector.IsExpanded = false;
        }

        private void ShowAgentInspector()
        {
            if (FindName("populationEditor") is Expander editor) editor.IsExpanded = false;
            if (FindName("inspectorPanel") is Expander inspector) inspector.IsExpanded = true;
        }

        private bool TryReadPopulationSize(out int size)
        {
            bool valid = int.TryParse(txtPopulationSize.Text, out size) && size >= 1 && size <= 10000;
            populationValidation.Text = valid ? "" : "Enter a population size between 1 and 10,000.";
            if (!valid) txtPopulationSize.Focus();
            return valid;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            if (e.Key == Key.Space) BtnStart_Click(sender, new RoutedEventArgs());
            else if (e.Key == Key.D) ShowPopulationDashboard(SelectedPopulation);
            else if (e.Key == Key.B) ShowPopulationNetworkDesigner(SelectedPopulation);
            else if (e.Key == Key.M) ShowPopulationListForm(SelectedPopulation);
            else return;
            e.Handled = true;
        }

        private void SavePreset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            { Filter = "AI-Evlo starting scenarios (*.scenario.json)|*.scenario.json", FileName = "My ecosystem.scenario.json" };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                ScenarioPreset preset;
                lock (simLock) preset = ScenarioPreset.Capture(Path.GetFileNameWithoutExtension(dialog.FileName),
                    eEnvironmentType, SmartObject.MovementSettings, lsPopulations);
                preset.Save(dialog.FileName);
                Log("Saved starting scenario: " + preset.Name);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not save scenario"); }
        }

        private void LoadPreset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "AI-Evlo starting scenarios (*.scenario.json)|*.scenario.json" };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                ScenarioPreset preset = ScenarioPreset.Load(dialog.FileName);
                if (MessageBox.Show(this, "Replace the current ecosystem with this starting scenario? All populations start with fresh brains.",
                    "Load starting scenario", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
                ApplyScenarioPreset(preset);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not load scenario"); }
        }
        internal void ApplyScenarioPreset(ScenarioPreset preset)
        {
            preset.Validate();
            // Own settings and discard any runtime state supplied by an edited preset.
            preset = ScenarioPreset.Capture(preset.Name, preset.Environment, preset.Movement, preset.Populations);
            if (!simulationRunner.Stop(TimeSpan.FromSeconds(1)))
                throw new InvalidOperationException("The simulation is stopping. Try loading the scenario again when it has stopped.");
            btnStart.Content = "▶ Start";
            btnStart.Background = HpGreen;
            // Construct fresh agents before changing the current ecosystem. Invalid networks
            // fail here while the existing populations are still intact.
            foreach (var population in preset.Populations)
                for (int i = 0; i < population.SizeLimit; i++)
                {
                    var member = CreatePopulationMember(population);
                    population.Add(member);
                    member.ID = population.GenerateMemberId();
                }
            foreach (var dashboard in _openDashboards.ToArray()) dashboard.Close();
            _populationListForm?.Close();
            _networkDesigner?.Close();
            lock (simLock)
            {
                foreach (var population in lsPopulations)
                {
                    RemoveGoldenAgent(population);
                    for (int i = population.Members.Count - 1; i >= 0; i--) DisposeObject(population.Members[i]);
                }
                lsObjects.Clear();
                lsPopulations.Clear();
                lsPopuCards.Clear();
                StackPnlPopulations.Children.Clear();
                shapeToObjectMap.Clear();
                SelectedObject = null;
                SelectedPopulation = null;
                CycleCount = lastCpsCheckCycle = 0;
                lastCpsCheckTime = DateTime.Now;
                _sparkAlive.Clear(); _sparkFitness.Clear();
                eEnvironmentType = preset.Environment;
                ddlEnvirnoment.SelectedIndex = eEnvironmentType == EEnvironmentType.OneTarget ? 0 : 1;
                foreach (var target in Targets) panlUniverseView.Children.Remove(target.VisibleShape);
                Targets.Clear();
                InitTargets();
                raftAnimations.Clear();
                SmartObject.MovementSettings = preset.Movement;
                foreach (var population in preset.Populations)
                {
                    population.ObjectType = GetObjectTypeForBeing(population.Being);
                    foreach (var member in population.Members) SetInitialAgentLocation(member);
                    lsObjects.AddRange(population.Members);
                    if (population.AutoGrowNeuralNetwork) PopulationAutoGrowthPolicy.SetEnabled(population, true);
                    RegisterPopulation(population);
                }
                SaveSession();
                SaveMovementSettings();
            }
            btnStart.Content = "▶ Start";
            btnStart.Background = HpGreen;
            Log("Loaded starting scenario: " + preset.Name + ". Press Start to begin.");
        }
    }
}
