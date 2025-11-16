using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Technoteam
{
    public partial class PlcPanel : UserControl
    {
        public PlcSimulator Plc { get; private set; }

        bool _overheatFlash;

        public PlcPanel() => InitializeComponent();

        public void Initialize(PlcSimulator plc, int index)
        {
            Plc = plc;
            TitleText.Text = $"PLC {index}";

            TargetSpeedSlider.ValueChanged += TargetSpeedSlider_OnValueChanged;
            StartButton.Click += StartButton_OnClick;
            EmergencyStopButton.Click += EmergencyStopButton_OnClick;

            Refresh();
        }

        private void TargetSpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Plc == null) return;

            Plc.TargetSpeed = (int)TargetSpeedSlider.Value;
            TargetSpeedValue.Text = $"{Plc.TargetSpeed} RPM";
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Plc == null) return;
            Plc.ManualStartRequested = true;
        }

        private void EmergencyStopButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Plc == null) return;
            Plc.ManualEmergencyStopRequested = true;
        }

        public void Refresh()
        {
            if (Plc == null) return;

            TempBar.Value = Plc.Temperature;
            TempValue.Text = $"{Plc.Temperature} °C";

            if (Plc.Temperature >= PlcSimulator.TemperatureLimit)
                TempBar.Foreground = Brushes.Red;
            else
                TempBar.Foreground = Brushes.Green;

            ActualSpeedBar.Value = Plc.ActualSpeed;
            ActualSpeedValue.Text = $"{Plc.ActualSpeed} RPM";

            if ((int)TargetSpeedSlider.Value != Plc.TargetSpeed)
                TargetSpeedSlider.Value = Plc.TargetSpeed;
            TargetSpeedValue.Text = $"{Plc.TargetSpeed} RPM";

            switch (Plc.State)
            {
                case PlcState.Normal:
                    if (Plc.IsOverheatWarning)
                    {
                        _overheatFlash = !_overheatFlash;
                        StatusLight.Fill = _overheatFlash ? Brushes.OrangeRed : Brushes.Yellow;
                        StatusLabel.Text = "Overheating!";
                    }
                    else
                    {
                        _overheatFlash = false;
                        StatusLight.Fill = Brushes.Green;
                        StatusLabel.Text = "Normal";
                    }

                    TargetSpeedSlider.IsEnabled = true;
                    StartButton.IsEnabled = false;
                    EmergencyStopButton.IsEnabled = Plc.ActualSpeed > 0;
                    break;

                case PlcState.EmergencyStop:
                    _overheatFlash = false;
                    StatusLight.Fill = Brushes.Red;
                    StatusLabel.Text = "Emergency Stop";

                    TargetSpeedSlider.IsEnabled = false;
                    StartButton.IsEnabled = Plc.Temperature < PlcSimulator.TemperatureLimit;
                    EmergencyStopButton.IsEnabled = false;
                    break;

                case PlcState.MaintenanceNeeded:
                    _overheatFlash = false;
                    StatusLight.Fill = Brushes.Yellow;
                    StatusLabel.Text = "Maintenance Needed";

                    TargetSpeedSlider.IsEnabled = false;
                    StartButton.IsEnabled = false;
                    EmergencyStopButton.IsEnabled = false;
                    break;
            }
        }
    }
}
