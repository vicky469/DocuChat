using Newtonsoft.Json;

namespace DocumentAPI.Infrastructure.Repository;

public class FileRepository: IFileRepository
{
    public static readonly string PersistentDataDirectory = "Data";
    public static readonly string BatchUrlDirectory = "Data/Batch Urls";
    
    public async Task SaveToFileAsync(string fileDirectory, string fileName, IEnumerable<string> data)
    {
        Directory.CreateDirectory(fileDirectory);
        var filePath = Path.Combine(fileDirectory, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            await System.IO.File.WriteAllLinesAsync(filePath, data);
        }
    }
    
    public async Task SaveToFileAsync<T>(string fileDirectory, string fileName, T data)
    {
        Directory.CreateDirectory(fileDirectory);
        var filePath = Path.Combine(fileDirectory, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            var jsonData = JsonConvert.SerializeObject(data);
            await System.IO.File.WriteAllTextAsync(filePath, jsonData);
        }
    }

    public async Task<T> ReadFromFileAsync<T>(string filePath)
    {
        if (!System.IO.File.Exists(filePath)) return default;

        var jsonData = await System.IO.File.ReadAllTextAsync(filePath);
        return JsonConvert.DeserializeObject<T>(jsonData);
    }
    
    public string[] IsFileExist(string filePath, string searchPattern)
    {
        return Directory.EnumerateFiles(filePath, searchPattern).ToArray();
    }
}