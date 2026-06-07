using System.Data;

namespace LetMeIn.Core.Interfaces
{
    public interface IDatabaseService : IDisposable
    {
        IDbConnection CreateConnection();

        void Initialize(string databasePath, string password);

        int RunNonQuery(string query, Dictionary<string, object>? parameters = null);
        IDataReader RunQuery(string query, Dictionary<string, object>? parameters = null);
    }
}
