using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace MyASCS.Helpers;

public class FileHelper
{
    public static async Task<Bitmap?> LoadBitmapAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            await using var stream = File.OpenRead(filePath);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading frame: {ex.Message}");
            return null;
        }
    }

    public static void SaveImage(string sourcePath, string destinationPath)
    {
        if (!File.Exists(sourcePath)) return;
        File.Copy(sourcePath, destinationPath, true);
        Console.WriteLine($"📸 Saved frame to {destinationPath}");
    }
}