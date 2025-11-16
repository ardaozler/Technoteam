using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Technoteam
{
    public partial class MainWindow : Window
    {
        private readonly PlcSimulator _plc = new();
        private readonly DispatcherTimer _timer = new();
        private readonly Stopwatch _stopwatch = new();
        private bool _overheatFlash;

        public MainWindow()
        {
            InitializeComponent();

            _plc.Start();

            ConfigureUIEvents();
            ConfigureTimer();
        }

        private void ConfigureUIEvents()
        {
            EmergencyStopButton1.Click += (s, e) => EmergencyStopButton1_OnClick(s, e);
            StartButton1.Click += (s, e) => StartButton1_OnClick(s, e);
            TargetSpeedSlider1.ValueChanged += (s, e) => TargetSpeedSlider1_OnValueChanged(s, e);
        }

        private void TargetSpeedSlider1_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _plc.TargetSpeed = (int)TargetSpeedSlider1.Value;
            TargetSpeedValue1.Text = $"{_plc.TargetSpeed} RPM";
        }

        private void StartButton1_OnClick(object sender, RoutedEventArgs e)
        {
            _plc.ManualStartRequested = true;
        }

        private void EmergencyStopButton1_OnClick(object sender, RoutedEventArgs e)
        {
            _plc.ManualEmergencyStopRequested = true;
        }

        private void ConfigureTimer()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += TimerOnTick;

            _stopwatch.Start();
            _timer.Start();
        }

        private void TimerOnTick(object? sender, EventArgs e)
        {
            var dt = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            _plc.Update(dt);

            if (_plc.IsOverheatWarning)
            {
                _overheatFlash = !_overheatFlash;
            }
            else
            {
                _overheatFlash = false;
            }

            UpdateUiFromPlc();
        }

        private void UpdateUiFromPlc()
        {
            TempBar1.Value = _plc.Temperature;
            if (_plc.Temperature >= PlcSimulator.TemperatureLimit)
            {
                TempBar1.Foreground = Brushes.Red;
            }
            else
            {
                TempBar1.Foreground = Brushes.Green;
            }
            TempValue1.Text = $"{_plc.Temperature} °C";

            ActualSpeedBar1.Value = _plc.ActualSpeed;
            ActualSpeedValue1.Text = $"{_plc.ActualSpeed} RPM";

            if ((int)TargetSpeedSlider1.Value != _plc.TargetSpeed)
            {
                TargetSpeedSlider1.Value = _plc.TargetSpeed;
            }
            TargetSpeedValue1.Text = $"{_plc.TargetSpeed} RPM";

            switch (_plc.State)
            {
                case PlcState.Normal:
                    if (_plc.IsOverheatWarning)
                    {
                        StatusLight1.Fill = _overheatFlash ? Brushes.OrangeRed : Brushes.Yellow;
                        StatusLabel1.Text = "Overheating!";
                    }
                    else
                    {
                        StatusLight1.Fill = Brushes.Green;
                        StatusLabel1.Text = "Normal";
                    }

                    TargetSpeedSlider1.IsEnabled = true;
                    StartButton1.IsEnabled = false;
                    EmergencyStopButton1.IsEnabled = _plc.ActualSpeed > 0;
                    break;

                case PlcState.EmergencyStop:
                    StatusLight1.Fill = Brushes.Red;
                    StatusLabel1.Text = "Emergency Stop";

                    TargetSpeedSlider1.IsEnabled = false;
                    StartButton1.IsEnabled = _plc.Temperature < PlcSimulator.TemperatureLimit;
                    EmergencyStopButton1.IsEnabled = false;
                    break;

                case PlcState.MaintenanceNeeded:
                    StatusLight1.Fill = Brushes.Yellow;
                    StatusLabel1.Text = "Maintenance Needed";

                    TargetSpeedSlider1.IsEnabled = false;
                    StartButton1.IsEnabled = false;
                    EmergencyStopButton1.IsEnabled = false;
                    break;
            }
        }
    }
}
