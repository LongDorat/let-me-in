using LetMeIn.Core.DatabaseService;
using LetMeIn.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace LetMeIn.Tests
{
    public class SqliteDatabaseFixture : IDisposable
    {
        public readonly IDatabaseService DatabaseService;
        public readonly SqliteConnection Connection;
        public readonly string DatabasePath;
        private const string Password = "testpassword";

        static SqliteDatabaseFixture()
        {
            SQLitePCL.Batteries_V2.Init();
        }

        public SqliteDatabaseFixture()
        {
            DatabasePath = Path.Combine(Path.GetTempPath(), $"letmein-tests-{Guid.NewGuid():N}.db");

            DatabaseService = new SqliteDatabase();
            DatabaseService.Initialize(DatabasePath, Password);

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                Password = Password,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            }.ToString();

            Connection = new SqliteConnection(connectionString);
            Connection.Open();

            SetupSchema();
            SeedData();
        }

        private void SetupSchema()
        {
            using var command = Connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL
                );
            ";
            command.ExecuteNonQuery();
        }

        public void SeedData()
        {
            using var command = Connection.CreateCommand();
            command.CommandText = """
                INSERT INTO Users (Username, PasswordHash)
                VALUES ('testuser', 'hashedpassword'),
                          ('updatethisuser', 'hashedpassword'),
                          ('deletethisuser', 'hashedpassword');
            """;
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            Connection.Dispose();
            DatabaseService.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            string tempPath = Path.GetTempPath();
            foreach (var file in Directory.GetFiles(tempPath, "letmein-tests-*.db"))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore exceptions during cleanup
                }
            }
            GC.SuppressFinalize(this);
        }
    }

    public class SqliteDatabaseTests(SqliteDatabaseFixture fixture) : IClassFixture<SqliteDatabaseFixture>
    {
        private readonly SqliteDatabaseFixture _fixture = fixture;

        [Fact]
        public void Database_ShouldNotOpenWithoutPassword_WhenEncrypted()
        {

            Assert.Throws<SqliteException>(() =>
            {
                using var connection = new SqliteConnection($"Data Source={_fixture.DatabasePath};Mode=ReadOnly");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Users;";
                _ = command.ExecuteScalar();
            });
        }

        [Fact]
        public void RunNonQuery_ShouldCreateTable()
        {
            var query = "CREATE TABLE AnotherTestTable (Id INTEGER PRIMARY KEY)";
            _ = _fixture.DatabaseService.RunNonQuery(query);
            // Unreliable, quirky behavior of SQLite when come to counting "Row(s) affected"
            //Assert.Equal(0, rowsAffected

            using var command = _fixture.Connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='AnotherTestTable';";
            var result = command.ExecuteScalar();
            Assert.NotNull(result);
        }

        [Fact]
        public void RunNonQuery_ShouldInsertData()
        {
            var query = "INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash)";
            var parameters = new Dictionary<string, object>
            {
                { "@username", "anotheruser" },
                { "@passwordHash", "hashedpassword" }
            };

            var rowsAffected = _fixture.DatabaseService.RunNonQuery(query, parameters);

            Assert.Equal(1, rowsAffected);
        }

        [Fact]
        public void RunNonQuerry_ShouldHandleInvalidQuery()
        {
            var query = "INSERT INTO NonExistentTable (Column) VALUES ('value')";
            Assert.Throws<SqliteException>(() => _fixture.DatabaseService.RunNonQuery(query));
        }

        [Fact]
        public void RunQuery_ShouldReturnData()
        {
            var selectQuery = "SELECT Username, PasswordHash FROM Users WHERE Username = @username";
            using var reader = _fixture.DatabaseService.RunQuery(selectQuery, new Dictionary<string, object> { { "@username", "testuser" } });

            Assert.True(reader.Read());
            Assert.Equal("testuser", reader["Username"]);
            Assert.Equal("hashedpassword", reader["PasswordHash"]);
        }

        [Fact]
        public void RunQuery_ShouldReturnNull()
        {
            var selectQuery = "SELECT Username FROM Users WHERE Username = @username";
            using var reader = _fixture.DatabaseService.RunQuery(selectQuery, new Dictionary<string, object> { { "@username", "nonexistentuser" } });
            Assert.False(reader.Read());
        }

        [Fact]
        public void RunQuery_ShouldThrow_ForInvalidSql()
        {
            var selectQuery = "SELECT User FROM Users WHERE Username = @username"; //? Intentionally wrong column name
            Assert.Throws<SqliteException>(() => _fixture.DatabaseService.RunQuery(selectQuery, new Dictionary<string, object> { { "@username", "testuser" } }));
        }

        [Fact]
        public void RunNonQuery_ShouldUpdateRow()
        {
            var updateQuery = "UPDATE Users SET PasswordHash = @passwordHash WHERE Username = @username";
            var parameters = new Dictionary<string, object>
            {
                { "@passwordHash", "newhashedpassword" },
                { "@username", "updatethisuser" }
            };
            var rowsAffected = _fixture.DatabaseService.RunNonQuery(updateQuery, parameters);
            Assert.Equal(1, rowsAffected);
        }

        [Fact]
        public void RunNonQuery_ShouldDeleteRow()
        {
            var deleteQuery = "DELETE FROM Users WHERE Username = @username";
            var parameters = new Dictionary<string, object>
            {
                { "@username", "deletethisuser" }
            };
            var rowsAffected = _fixture.DatabaseService.RunNonQuery(deleteQuery, parameters);
            Assert.Equal(1, rowsAffected);
        }
    }
}
