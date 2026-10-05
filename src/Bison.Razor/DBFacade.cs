using Microsoft.Data.Sqlite;

public class DBFacade
{
    private readonly string _connectionString;

    public DBFacade(string dbFilePath)
    {
        Console.WriteLine($"Opening SQLite file: {dbFilePath}");
        if (!File.Exists(dbFilePath))
        {
            initializeDataBase(dbFilePath);
        }

        _connectionString = $"Data Source={dbFilePath}";
    }

    public List<Dictionary<string, object>> Query(string sql, Dictionary<string, object>? parameters = null)
    {
        var results = new List<Dictionary<string, object>>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;

        if (parameters != null)
        {
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }
        }

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            results.Add(row);
        }

        return results;
    }
    private static void initializeDataBase(string dbFilePath)
    {
        using var connection = new SqliteConnection($"Data Source ={dbFilePath}");
        connection.Open();

        var basePath = AppContext.BaseDirectory;

        var schema = File.ReadAllText(Path.Combine(basePath, "schema.sql"));
        var dump = File.ReadAllText(Path.Combine(basePath, "dump.sql"));

        using var command = connection.CreateCommand();
        command.CommandText = schema;
        command.ExecuteNonQuery();

        command.CommandText = dump;
        command.ExecuteNonQuery();
    }

}
