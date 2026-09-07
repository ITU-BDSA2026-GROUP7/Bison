using System.Globalization;
using CsvHelper;

namespace SimpleDB;

public sealed class CSVDatabase<T> : IDatabaseRepository<T>
{
    private readonly string _filePath;

    public CSVDatabase(string filePath)
    {
        _filePath = filePath;
    }

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