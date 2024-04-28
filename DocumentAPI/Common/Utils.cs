namespace DocumentAPI.Common;

public static class Utils
{
    public static List<T[]> SplitIntoChunks<T>(T[] items, int chunkSize = 20)
    {
        var chunks = new List<T[]>();
        var index = 0;
        while (index < items.Length)
        {
            chunks.Add(items.Skip(index).Take(chunkSize).ToArray());
            index += chunkSize;
        }

        return chunks;
    }
}