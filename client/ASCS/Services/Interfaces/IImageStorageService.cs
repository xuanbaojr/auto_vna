using System.Threading.Tasks;

namespace MyASCS.Services.Interfaces;

public interface IImageStorageService
{
    Task SaveImageRecordAsync(string sessionId, string filePath, string imageType);
}