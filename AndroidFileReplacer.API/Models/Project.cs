namespace AndroidFileReplacer.API.Models;

public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 