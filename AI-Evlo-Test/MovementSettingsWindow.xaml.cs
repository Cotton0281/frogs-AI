using AI_Evlo_Test.Objects;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace AI_Evlo_Test
{
    public partial class MovementSettingsWindow : Window
    {
        public MovementSettings Settings { get; private set; }

        public MovementSettingsWindow(MovementSettings settings)
        {
            InitializeComponent();
            Settings = settings?.Clone() ?? new MovementSettings();
            Settings.Normalize();

            sldRotationHpCost.Value = Settings.RotationHpCost;
            sldThrustHpCost.Value = Settings.ThrustHpCost;
            sldLandedBirdSpeedMultiplier.Value = Settings.LandedBirdSpeedMultiplier;
            sldBiteHpAmount.Value = Settings.BiteHpAmount;
            sldBiteCooldownTicks.Value = Settings.BiteCooldownTicks;
            sldPredatorBiteHpThreshold.Value = Settings.PredatorBiteHpThreshold;
            UpdateValueLabels();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Settings = new MovementSettings
            {
                RotationHpCost = sldRotationHpCost.Value,
                ThrustHpCost = sldThrustHpCost.Value,
                LandedBirdSpeedMultiplier = sldLandedBirdSpeedMultiplier.Value,
                BiteHpAmount = (int)Math.Round(sldBiteHpAmount.Value),
                BiteCooldownTicks = (int)Math.Round(sldBiteCooldownTicks.Value),
                PredatorBiteHpThreshold = sldPredatorBiteHpThreshold.Value
            };
            Settings.Normalize();

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelAndClose();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            e.Handled = true;
            CancelAndClose();
        }

        private void CancelAndClose()
        {
            try
            {
                DialogResult = false;
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }

        private void SettingsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            if (lblRotationHpCost == null ||
                lblThrustHpCost == null ||
                lblLandedBirdSpeedMultiplier == null ||
                lblBiteHpAmount == null ||
                lblBiteCooldownTicks == null ||
                lblPredatorBiteHpThreshold == null ||
                sldRotationHpCost == null ||
                sldThrustHpCost == null ||
                sldLandedBirdSpeedMultiplier == null ||
                sldBiteHpAmount == null ||
                sldBiteCooldownTicks == null ||
                sldPredatorBiteHpThreshold == null)
            {
                return;
            }

            lblRotationHpCost.Text = sldRotationHpCost.Value.ToString("0.00", CultureInfo.InvariantCulture);
            lblThrustHpCost.Text = sldThrustHpCost.Value.ToString("0.00", CultureInfo.InvariantCulture);
            lblLandedBirdSpeedMultiplier.Text = sldLandedBirdSpeedMultiplier.Value.ToString("0.00", CultureInfo.InvariantCulture);
            lblBiteHpAmount.Text = ((int)Math.Round(sldBiteHpAmount.Value)).ToString(CultureInfo.InvariantCulture);
            lblBiteCooldownTicks.Text = ((int)Math.Round(sldBiteCooldownTicks.Value)).ToString(CultureInfo.InvariantCulture);
            lblPredatorBiteHpThreshold.Text = (sldPredatorBiteHpThreshold.Value * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
        }
    }
}
