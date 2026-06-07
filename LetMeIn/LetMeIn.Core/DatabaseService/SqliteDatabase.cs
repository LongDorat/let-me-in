using LetMeIn.Core.Interfaces;
using System.Data;
using Microsoft.Data.Sqlite;

namespace LetMeIn.Core.DatabaseService
{
    public class SqliteDatabase : IDatabaseService
    {
        private string? _connectionString;
        private bool _disposed;

        public IDbConnection CreateConnection()
        {
            ThrowIfDisposed();

            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public void Initialize(string databasePath, string password)
        {
            ThrowIfDisposed();

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Password = password,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();
        }

        public int RunNonQuery(string query, Dictionary<string, object>? parameters = null)
        {
            ThrowIfDisposed();

            using var connection = CreateConnection();
            using var command = CreateCommand(query, parameters, connection);
            return command.ExecuteNonQuery();
        }

        public IDataReader RunQuery(string query, Dictionary<string, object>? parameters = null)
        {
            ThrowIfDisposed();

            var connection = CreateConnection();
            var command = CreateCommand(query, parameters, connection);

            try
            {
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch
            {
                command.Dispose();
                connection.Dispose();
                throw;
            }
        }

        private static SqliteCommand CreateCommand(string sql, Dictionary<string, object>? parameters, IDbConnection connection)
        {
            var command = new SqliteCommand(sql, (SqliteConnection)connection);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
            return command;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _connectionString = null;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(SqliteDatabase));
        }
    }
}
