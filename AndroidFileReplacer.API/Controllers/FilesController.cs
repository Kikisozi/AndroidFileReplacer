using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AndroidFileReplacer.API.Models;
using AndroidFileReplacer.API.Filters;

namespace AndroidFileReplacer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiKey]
public class FilesController : ControllerBase
{
    private readonly ILogger<FilesController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _projectsFilePath;
    private readonly string _uploadsDirectory;

    public FilesController(ILogger<FilesController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _projectsFilePath = Path.Combine(_environment.ContentRootPath, "projects.json");
        _uploadsDirectory = Path.Combine(_environment.ContentRootPath, "uploads");
        
        // 确保上传目录存在
        if (!Directory.Exists(_uploadsDirectory))
        {
            Directory.CreateDirectory(_uploadsDirectory);
        }
    }

    [HttpGet("{fileId}")]
    public async Task<IActionResult> DownloadFile(string fileId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                _logger.LogWarning("文件ID为空");
                return BadRequest("文件ID不能为空");
            }

            // 检查文件是否存在
            var filePath = Path.Combine(_uploadsDirectory, fileId);
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning($"找不到文件ID: {fileId}");
                return NotFound($"找不到ID为{fileId}的文件");
            }

            // 获取文件信息
            var fileName = "unknown.file";
            if (System.IO.File.Exists(_projectsFilePath))
            {
                var json = await System.IO.File.ReadAllTextAsync(_projectsFilePath);
                var projects = JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
                var project = projects.FirstOrDefault(p => p.FileId == fileId);
                if (project != null)
                {
                    fileName = project.FileName;
                }
            }

            // 返回文件流
            _logger.LogInformation($"下载文件: {fileName}, ID: {fileId}");
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(fileStream, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载文件时出错");
            return StatusCode(500, $"下载文件时出错: {ex.Message}");
        }
    }
} 