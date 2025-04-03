using System;

namespace MyASCS.Models;

public class Session
{
    public int Id { get; set; }
    public int StaffId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
