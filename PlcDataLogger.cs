using Microsoft.Data.Sqlite;
using System.IO;

namespace Technoteam
{
    internal class PlcDataLogger : IDisposable
    {
        private readonly string _dbPath;
        private readonly SqliteConnection _connection;

        public PlcDataLogger(string dbFileName = "plc_log.db")
        {
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbFileName);
            _connection = new SqliteConnection($"Data Source={_dbPath}");
            _connection.Open();

            EnsureSchema();
        }

        private void EnsureSchema()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS PlcLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TimestampUtc TEXT NOT NULL,
                PlcIndex INTEGER NOT NULL,
                Temperature INTEGER NOT NULL,
                ActualSpeed INTEGER NOT NULL,
                TargetSpeed INTEGER NOT NULL,
                State TEXT NOT NULL
                );";

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public void LogSnapshot(int plcIndex, PlcSimulator plc)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO PlcLog
                (TimestampUtc, PlcIndex, Temperature, ActualSpeed, TargetSpeed, State)
                VALUES ($ts, $idx, $temp, $act, $tgt, $state);";

            cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
            cmd.Parameters.AddWithValue("$idx", plcIndex);
            cmd.Parameters.AddWithValue("$temp", plc.Temperature);
            cmd.Parameters.AddWithValue("$act", plc.ActualSpeed);
            cmd.Parameters.AddWithValue("$tgt", plc.TargetSpeed);
            cmd.Parameters.AddWithValue("$state", plc.State.ToString());

            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
