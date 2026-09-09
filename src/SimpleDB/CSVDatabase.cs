using System.Globalization;
using CsvHelper;

namespace SimpleDB;

public sealed class CSVDatabase<T> : IDatabaseRepository<T>
{
    private static readonly Dictionary<string, CSVDatabase<T>> _instances = new();
    private readonly string _filePath;

    private CSVDatabase(string filePath)
    {
        _filePath = filePath;
    }

   
    public static CSVDatabase<T> Instance(string filePath)
    {
        string key = Path.GetFullPath(filePath);

        if (!_instances.TryGetValue(key, out var instance))
        {
            instance = new CSVDatabase<T>(key);
            _instances[key] = instance;
        }

        return instance;
    }

 
    internal static void ResetForTests() => _instances.Clear();

    public IEnumerable<T> Read(int? limit = null)
    {
        if (!File.Exists(_filePath))
        {
            return Enumerable.Empty<T>();
        }

        using var reader = new StreamReader(_filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<T>();

        return limit is null
            ? records.ToList()
            : records.Take(limit.Value).ToList();
    }

    public void Store(T record)
    {
        bool fileNeedsHeader = !File.Exists(_filePath) || new FileInfo(_filePath).Length == 0;

        using var writer = new StreamWriter(_filePath, append: true);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        if (fileNeedsHeader)
        {
            csv.WriteHeader<T>();
            csv.NextRecord();
        }

        csv.WriteRecord(record);
        csv.NextRecord();
    }
}
