using AI_Evlo_Test.Objects;
using ArtificialNeuralNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AI_Evlo_Test
{
    public partial class VisualizeNetwork : Window
    {
        private readonly IPopulationNetworkWorkspace workspace;
        private readonly DispatcherTimer refreshTimer;
        private bool suppressSelection;
        private string requestedPopulationId;

        public VisualizeNetwork()
            : this(new EmptyPopulationNetworkWorkspace())
        {
        }

        public VisualizeNetwork(IPopulationNetworkWorkspace workspace)
        {
            this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            InitializeComponent();
            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            refreshTimer.Tick += (sender, args) => RefreshSnapshot(
                refreshPopulationList: true,
                refreshNetwork: liveRefreshCheck.IsChecked == true);
            refreshTimer.Start();
            Loaded += (sender, args) => RefreshSnapshot(refreshPopulationList: true);
            Closed += (sender, args) =>
            {
                refreshTimer.Stop();
                Objects.WindowBoundsStore.Save("VisualizeNetwork", Width, Height);
            };

            if (Objects.WindowBoundsStore.TryGet("VisualizeNetwork", out double width, out double height))
            {
                Width = width;
                Height = height;
            }
        }

        public string Status
        {
            get => statusText.Text;
            set => statusText.Text = value ?? string.Empty;
        }

        internal INeuralNetwork DisplayedNetwork => networkView.Network;

        public void SelectPopulation(string populationId)
        {
            requestedPopulationId = populationId;
            if (IsLoaded)
                RefreshSnapshot(refreshPopulationList: true);
        }

        internal void ShowNNet(INeuralNetwork network)
        {
            networkView.SetSnapshot(network, Array.Empty<bool>());
            Status = network == null ? "No network selected." : "Neural network graph rendered.";
        }

        private void RefreshSnapshot(bool refreshPopulationList, bool refreshNetwork = true)
        {
            SpecialAgentRole preferredRole = (agentCombo.SelectedItem as SpecialAgentOption)?.Role
                ?? SpecialAgentRole.Golden;
            string previousPopulationId = (populationCombo.SelectedItem as PopulationOption)?.Id;
            string selectedPopulationId = requestedPopulationId
                ?? previousPopulationId;

            if (refreshPopulationList)
            {
                IReadOnlyList<PopulationOption> populations = workspace.GetPopulations();
                List<PopulationOption> currentOptions = populationCombo.Items
                    .OfType<PopulationOption>()
                    .ToList();
                bool choicesChanged = currentOptions.Count != populations.Count
                    || currentOptions.Where((option, index) =>
                            option.Id != populations[index].Id || option.Name != populations[index].Name)
                        .Any();
                IReadOnlyList<PopulationOption> selectableOptions = choicesChanged
                    ? populations
                    : currentOptions;
                PopulationOption selected = selectableOptions
                    .FirstOrDefault(option => option.Id == selectedPopulationId)
                    ?? selectableOptions.FirstOrDefault();

                suppressSelection = true;
                if (choicesChanged)
                    populationCombo.ItemsSource = populations;
                populationCombo.SelectedItem = selected;
                suppressSelection = false;
                selectedPopulationId = selected?.Id;
                requestedPopulationId = null;

                bool selectionChanged = previousPopulationId != selectedPopulationId;
                if (!refreshNetwork && !selectionChanged)
                    return;
            }

            if (string.IsNullOrEmpty(selectedPopulationId))
            {
                agentCombo.ItemsSource = null;
                networkView.SetSnapshot(null, Array.Empty<bool>());
                agentDetailText.Text = "No active populations";
                growthText.Text = "";
                SetParameterButtonsEnabled(false);
                mutationRateSlider.IsEnabled = false;
                Status = "Create a population to inspect its neural network.";
                return;
            }

            PopulationNetworkSnapshot snapshot = workspace.Capture(selectedPopulationId, preferredRole);
            if (snapshot == null)
            {
                if (!refreshPopulationList)
                    RefreshSnapshot(refreshPopulationList: true);
                return;
            }

            suppressSelection = true;
            agentCombo.ItemsSource = snapshot.AvailableAgents;
            agentCombo.SelectedItem = snapshot.AvailableAgents?
                .FirstOrDefault(option => option.Role == snapshot.SelectedRole);
            mutationRateSlider.Value = snapshot.MutationRate;
            mutationRateText.Text = snapshot.MutationRate.ToString();
            suppressSelection = false;
            mutationRateSlider.IsEnabled = true;

            networkView.SetSnapshot(snapshot.Network, snapshot.LayerLocks);
            SetParameterButtonsEnabled(
                snapshot.Network != null
                && snapshot.LayerLocks != null
                && snapshot.LayerLocks.Any(locked => !locked));
            agentDetailText.Text = snapshot.AgentDetail ?? "No special agent is currently available";
            growthText.Text = snapshot.AutoGrowEnabled
                ? $"Auto-grow enabled · next milestone {snapshot.NextAutoGrowSurvivalCycles:N0} cycles"
                : "Auto-grow disabled";
            Status = snapshot.Network == null
                ? "This population has no available special-agent brain."
                : $"Showing {snapshot.PopulationName} · {snapshot.Network.HiddenLayers.Count} hidden layers";
        }

        private void PopulationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!suppressSelection)
                RefreshSnapshot(refreshPopulationList: false);
        }

        private void AgentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!suppressSelection)
                RefreshSnapshot(refreshPopulationList: false);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
            => RefreshSnapshot(refreshPopulationList: true);

        private void Fit_Click(object sender, RoutedEventArgs e)
            => networkView.FitToView();

        private void BlankNetwork_Click(object sender, RoutedEventArgs e)
        {
            ApplyParameterChange(
                "Set all incoming weights and biases in unlocked layers to zero for every brain in this population?",
                "Zero Unlocked Network Layers",
                populationId => workspace.BlankUnlockedLayers(populationId));
        }

        private void RandomizeNetwork_Click(object sender, RoutedEventArgs e)
        {
            ApplyParameterChange(
                "Replace all incoming weights and biases in unlocked layers with random values for every brain in this population?",
                "Randomize Unlocked Network Layers",
                populationId => workspace.RandomizeUnlockedLayers(populationId));
        }

        private void MutationRateSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (mutationRateText == null)
                return;

            int mutationRate = (int)Math.Round(e.NewValue);
            mutationRateText.Text = mutationRate.ToString();
            if (suppressSelection || !IsLoaded)
                return;

            PopulationOption population = populationCombo.SelectedItem as PopulationOption;
            if (population == null)
                return;

            PopulationNetworkChangeResult result = workspace.SetMutationRate(population.Id, mutationRate);
            Status = result.Message;
        }

        private void MutateAgent_Click(object sender, RoutedEventArgs e)
        {
            PopulationOption population = populationCombo.SelectedItem as PopulationOption;
            SpecialAgentOption agent = agentCombo.SelectedItem as SpecialAgentOption;
            if (population == null || agent == null)
                return;

            PopulationNetworkChangeResult result = workspace.MutateAgent(population.Id, agent.Role);
            Status = result.Message;
            if (result.Succeeded)
                RefreshSnapshot(refreshPopulationList: false);
        }

        private void ApplyParameterChange(
            string confirmationMessage,
            string confirmationTitle,
            Func<string, PopulationNetworkChangeResult> update)
        {
            PopulationOption population = populationCombo.SelectedItem as PopulationOption;
            if (population == null)
                return;

            MessageBoxResult confirmation = MessageBox.Show(
                this,
                confirmationMessage + "\n\nLocked layers and the historical golden baseline will remain unchanged.",
                confirmationTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirmation != MessageBoxResult.Yes)
                return;

            PopulationNetworkChangeResult result = update(population.Id);
            Status = result.Message;
            if (result.Succeeded)
                RefreshSnapshot(refreshPopulationList: false);
        }

        private void SetParameterButtonsEnabled(bool enabled)
        {
            blankNetworkButton.IsEnabled = enabled;
            randomizeNetworkButton.IsEnabled = enabled;
            mutateAgentButton.IsEnabled = enabled;
        }

        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            PopulationOption population = populationCombo.SelectedItem as PopulationOption;
            if (population == null)
                return;

            MessageBoxResult confirmation = MessageBox.Show(
                this,
                $"Add a zero-initialized residual layer to every live and archived brain in '{population.Name}'?\n\n" +
                "The new layer starts unlocked and initially preserves the existing network outputs.",
                "Add Neural-Network Layer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirmation != MessageBoxResult.Yes)
                return;

            PopulationNetworkChangeResult result = workspace.AddLayer(population.Id);
            Status = result.Message;
            if (result.Succeeded)
                RefreshSnapshot(refreshPopulationList: true);
        }

        private void NetworkView_LayerLockToggleRequested(object sender, LayerLockToggleEventArgs e)
        {
            PopulationOption population = populationCombo.SelectedItem as PopulationOption;
            if (population == null)
                return;

            PopulationNetworkChangeResult result = workspace.SetLayerLock(
                population.Id,
                e.DestinationLayerIndex,
                e.Locked);
            Status = result.Message;
            if (result.Succeeded)
                RefreshSnapshot(refreshPopulationList: false);
        }

        private void WeightFilterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (networkView == null || weightFilterText == null)
                return;
            networkView.MinimumAbsoluteWeight = e.NewValue;
            weightFilterText.Text = e.NewValue.ToString("0.00");
        }

        private sealed class EmptyPopulationNetworkWorkspace : IPopulationNetworkWorkspace
        {
            public IReadOnlyList<PopulationOption> GetPopulations() => Array.Empty<PopulationOption>();
            public PopulationNetworkSnapshot Capture(string populationId, SpecialAgentRole preferredRole) => null;
            public PopulationNetworkChangeResult SetLayerLock(string populationId, int destinationLayerIndex, bool locked)
                => PopulationNetworkChangeResult.Failure("No population workspace is connected.");
            public PopulationNetworkChangeResult AddLayer(string populationId)
                => PopulationNetworkChangeResult.Failure("No population workspace is connected.");
            public PopulationNetworkChangeResult BlankUnlockedLayers(string populationId)
                => PopulationNetworkChangeResult.Failure("No population workspace is connected.");
            public PopulationNetworkChangeResult RandomizeUnlockedLayers(string populationId)
                => PopulationNetworkChangeResult.Failure("No population workspace is connected.");
            public PopulationNetworkChangeResult SetMutationRate(string populationId, int mutationRate)
                => PopulationNetworkChangeResult.Failure("No population workspace is connected.");
            public PopulationNetworkChangeResult MutateAgent(string populationId, SpecialAgentRole role)
                => PopulationNetworkChangeResult.Failure("No population workspace is connected.");
        }
    }
}
