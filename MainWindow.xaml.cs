using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace Technoteam
{
    public partial class MainWindow : Window
    {
        private readonly List<PlcSimulator> _plcs = new();
        private readonly List<PlcPanel> _panels = new();

        private readonly DispatcherTimer _timer = new();
        private readonly Stopwatch _stopwatch = new();

        private readonly PlcDataLogger _logger = new();
        private double _logAccumulator;

        public MainWindow()
        {
            InitializeComponent();

            ConfigureUiEvents();
            CreatePlcs(4);//default 4 PLCs
            ConfigureTimer();
        }

        private void ConfigureUiEvents()
        {
            ApplyPlcCountButton.Click += ApplyPlcCountButton_OnClick;
            ApplyDefaultRpmButton.Click += ApplyDefaultRpmButton_OnClick;
            StopAllButton.Click += StopAllButton_OnClick;
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

            foreach (var plc in _plcs)
                plc.Update(dt);

            foreach (var panel in _panels)
                panel.Refresh();

            _logAccumulator += dt;
            if (_logAccumulator >= 5.0)
            {
                LogAllPlcs();
                _logAccumulator = 0;
            }
        }

        private void LogAllPlcs()
        {
            for (int i = 0; i < _plcs.Count; i++)
            {
                var plc = _plcs[i];
                _logger.LogSnapshot(i + 1, plc);
            }
        }

        private void CreatePlcs(int count)
        {
            if (count <= 0) return;

            _plcs.Clear();
            _panels.Clear();
            PlcPanelHost.Children.Clear();

            for (int i = 0; i < count; i++)
            {
                var plc = new PlcSimulator();
                plc.Start();
                _plcs.Add(plc);

                var panel = new PlcPanel();
                panel.Initialize(plc, i + 1);
                _panels.Add(panel);

                PlcPanelHost.Children.Add(panel);
            }
        }

        private void ApplyPlcCountButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PlcCountTextBox.Text, out var count))
            {
                MessageBox.Show("Invalid PLC count", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (count <= 0 || count > 200)
            {
                MessageBox.Show("Please choose a PLC count between 1 and 200.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CreatePlcs(count);
        }

        private void ApplyDefaultRpmButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(DefaultRpmTextBox.Text, out var rpm))
            {
                MessageBox.Show("Invalid RPM value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (rpm < 0) rpm = 0;
            if (rpm > 2000) rpm = 2000;

            foreach (var plc in _plcs)
            {
                plc.TargetSpeed = rpm;
            }
        }

        private void StopAllButton_OnClick(object? sender, RoutedEventArgs e)
        {
            foreach (var plc in _plcs)
            {
                plc.ManualEmergencyStopRequested = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer.Stop();
            _logger.Dispose();
        }
    }
}
