using System;

namespace MyASCS.Models;

public class ImageRecord
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int StaffId { get; set; }
    public string Position { get; set; } = string.Empty; // ('face', 'clothes', 'hands', 'shoes')
    public string FilePath { get; set; } = string.Empty;
    public bool Uploaded { get; set; } = false;
}