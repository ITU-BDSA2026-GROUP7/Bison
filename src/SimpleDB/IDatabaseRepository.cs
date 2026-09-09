namespace SimpleDB;

public interface IDatabaseRepository<T>
{
    IEnumerable<T> Read(int? limit = null);
    void Store(T record);
}
