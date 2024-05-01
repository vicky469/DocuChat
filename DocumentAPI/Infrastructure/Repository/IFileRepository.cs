namespace DocumentAPI.Infrastructure.Repository;

public interface IFileRepository
{
    Task SaveToFileAsync(string fileDirectory, string fileName, IEnumerable<string> data);
    Task SaveToFileAsync<T>(string fileDirectory, string fileName, T data);
    Task<T> ReadFromFileAsync<T>(string filePath);
    string[] IsFileExist(string filePath, string searchPattern);
}