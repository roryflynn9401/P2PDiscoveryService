using System.Text;
using System.Text.Json;

namespace P2PDirectoryService.Data
{
    public static class ByteExtensions
    {
        public static byte[] GetByteArray<T>(T item)
        {
            var jsonString = JsonSerializer.Serialize(item);
            return Encoding.UTF8.GetBytes(jsonString);
        }
    }
}