using System;

namespace MyASCS.Models;

public class PersonModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Department { get; set; }
    public string PhotoPath { get; set; }
    public DateTime LastIdentified { get; set; }
}