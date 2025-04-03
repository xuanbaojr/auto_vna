using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Data.Sqlite;
using MyASCS.Services.Interfaces;

namespace MyASCS.Services.Implementations;

public class ImageUploadService : IDisposable, IImageUploadService
{
    private const string DbPath = "images.db";
        private const string ServerUrl = "https://your-server.com/upload";
        private readonly HttpClient _httpClient;
        private readonly Timer _timer;

        public ImageUploadService()
        {
            _httpClient = new HttpClient();
            _timer = new Timer(async _ => await UploadImages(), null, 0, 60000); // Run every 60 seconds
        }

        private async System.Threading.Tasks.Task UploadImages()
        {
            using var connection = new SqliteConnection($"Data Source={DbPath}");
            await connection.OpenAsync();

            string selectCommand = "SELECT Id, ImagePath FROM Images WHERE Uploaded = 0 LIMIT 10;";
            using var selectCmd = new SqliteCommand(selectCommand, connection);
            using var reader = await selectCmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                int imageId = reader.GetInt32(0);
                string imagePath = reader.GetString(1);

                if (!File.Exists(imagePath)) continue;

                try
                {
                    byte[] imageData = await File.ReadAllBytesAsync(imagePath);
                    var payload = new { filename = Path.GetFileName(imagePath), data = Convert.ToBase64String(imageData) };

                    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(ServerUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string updateCommand = "UPDATE Images SET Uploaded = 1 WHERE Id = @id;";
                        using var updateCmd = new SqliteCommand(updateCommand, connection);
                        updateCmd.Parameters.AddWithValue("@id", imageId);
                        await updateCmd.ExecuteNonQueryAsync();

                        Console.WriteLine($"Uploaded {imagePath} successfully.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading {imagePath}: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _httpClient?.Dispose();
        }
}