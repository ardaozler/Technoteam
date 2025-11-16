using Microsoft.Data.Sqlite;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Technoteam
{
    public class PlcLogEntry
    {
        public long Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public int PlcIndex { get; set; }
        public int Temperature { get; set; }
        public int ActualSpeed { get; set; }
        public int TargetSpeed { get; set; }
        public string State { get; set; } = "";
    }
    public partial class LogPanel : UserControl
    {
        private readonly ObservableCollection<PlcLogEntry> _logEntries = new();
        public LogPanel()
        {
            InitializeComponent();

            LogDataGrid.ItemsSource = _logEntries;
            LoadLogsButton.Click += LoadLogsButton_OnClick;
            ClearLogsButton.Click += ClearLogsButton_OnClick;
        }

        private void ClearLogsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("THIS WILL DROP ALL TABLES \n ARE YOU SURE?", "Drop Tables?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plc_log.db");
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM PlcLog;";
            cmd.ExecuteNonQuery();
            _logEntries.Clear();
        }

        public void SetPlcCount(int count)
        {
            PlcFilterComboBox.Items.Clear();

            var allItem = new ComboBoxItem { Content = "All PLCs", Tag = null };
            PlcFilterComboBox.Items.Add(allItem);

            for (int i = 1; i <= count; i++)
            {
                PlcFilterComboBox.Items.Add(new ComboBoxItem
                {
                    Content = $"PLC {i}",
                    Tag = i
                });
            }

            PlcFilterComboBox.SelectedIndex = 0;
        }
        private void LoadLogsButton_OnClick(object sender, RoutedEventArgs e)
        {
            _logEntries.Clear();

            var from = FromDatePicker.SelectedDate;
            var to = ToDatePicker.SelectedDate;

            int? plcFilter = null;
            if (PlcFilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is int idx)
                plcFilter = idx;

            var onlyAlarms = OnlyAlarmsCheckBox.IsChecked == true;

            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plc_log.db");
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();

            using var cmd = conn.CreateCommand();

            string sql = @"
                SELECT Id, TimestampUtc, PlcIndex, Temperature, ActualSpeed, TargetSpeed, State
                FROM PlcLog
                WHERE 1 = 1
                ";

            if (from.HasValue)
            {
                sql += " AND TimestampUtc >= $from";
                cmd.Parameters.AddWithValue("$from", from.Value.Date.ToUniversalTime().ToString("o"));
            }

            if (to.HasValue)
            {
                var endExclusive = to.Value.Date.AddDays(1).ToUniversalTime();
                sql += " AND TimestampUtc < $to";
                cmd.Parameters.AddWithValue("$to", endExclusive.ToString("o"));
            }

            if (plcFilter.HasValue)
            {
                sql += " AND PlcIndex = $idx";
                cmd.Parameters.AddWithValue("$idx", plcFilter.Value);
            }

            if (onlyAlarms)
            {
                sql += " AND (State <> 'Normal' OR Temperature >= $limit)";
                cmd.Parameters.AddWithValue("$limit", PlcSimulator.TemperatureLimit);
            }

            sql += " ORDER BY TimestampUtc DESC";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var entry = new PlcLogEntry
                {
                    Id = reader.GetInt64(0),
                    TimestampUtc = DateTime.Parse(reader.GetString(1)),
                    PlcIndex = reader.GetInt32(2),
                    Temperature = reader.GetInt32(3),
                    ActualSpeed = reader.GetInt32(4),
                    TargetSpeed = reader.GetInt32(5),
                    State = reader.GetString(6)
                };

                _logEntries.Add(entry);
            }
        }

    }
}
