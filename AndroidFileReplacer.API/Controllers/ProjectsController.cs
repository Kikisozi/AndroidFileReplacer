using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AndroidFileReplacer.API.Models;
using AndroidFileReplacer.API.Filters;

namespace AndroidFileReplacer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiKey]
public class ProjectsController : ControllerBase
{
    private readonly ILogger<ProjectsController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _projectsFilePath;
    private readonly string _uploadsDirectory;

    public ProjectsController(ILogger<ProjectsController> logger, IWebHostEnvironment environment)
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
    {
        try
        {
            if (!System.IO.File.Exists(_projectsFilePath))
            {
                _logger.LogWarning("Projects file not found. Creating empty file.");
                await System.IO.File.WriteAllTextAsync(_projectsFilePath, "[]");
                return new List<Project>();
            }

            var json = await System.IO.File.ReadAllTextAsync(_projectsFilePath);
            var projects = JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
            return projects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading projects file");
            return StatusCode(500, "Error reading projects data");
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<Project>> CreateProject([FromForm] string name, [FromForm] string targetPath, [FromForm] IFormFile file)
    {
        try
        {
            _logger.LogInformation($"接收到文件上传请求: 项目名={name}, 目标路径={targetPath}, 文件名={file?.FileName}, 文件大小={file?.Length}字节");
            
            if (string.IsNullOrWhiteSpace(name))
            {
                _logger.LogWarning("项目名称为空");
                return BadRequest("项目名称不能为空");
            }
            
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                _logger.LogWarning("目标路径为空");
                return BadRequest("目标路径不能为空");
            }
            
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("文件为空或大小为0");
                return BadRequest("文件不能为空");
            }
            
            // 读取现有项目
            var projects = new List<Project>();
            if (System.IO.File.Exists(_projectsFilePath))
            {
                _logger.LogInformation($"读取现有项目列表");
                var json = await System.IO.File.ReadAllTextAsync(_projectsFilePath);
                projects = JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
            }
            else
            {
                _logger.LogInformation("项目文件不存在，将创建新文件");
            }
            
            // 创建新项目
            var fileId = Guid.NewGuid().ToString();
            var project = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                TargetPath = targetPath,
                FileId = fileId,
                FileName = file.FileName,
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow
            };
            
            // 确保上传目录存在
            _logger.LogInformation("确保上传目录存在");
            Directory.CreateDirectory(_uploadsDirectory);
            
            // 保存文件
            var filePath = Path.Combine(_uploadsDirectory, fileId);
            _logger.LogInformation($"保存文件ID: {fileId}");
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            // 添加项目并保存
            projects.Add(project);
            var updatedJson = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_projectsFilePath, updatedJson);
            
            _logger.LogInformation($"项目创建成功: {project.Name}, 文件ID: {project.FileId}");
            return CreatedAtAction(nameof(GetProjects), new { id = project.Id }, project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建项目时出错");
            return StatusCode(500, $"创建项目时出错: {ex.Message}");
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest("Project ID is required");
            }
            
            // 读取现有项目
            if (!System.IO.File.Exists(_projectsFilePath))
            {
                return NotFound("Projects file not found");
            }
            
            var json = await System.IO.File.ReadAllTextAsync(_projectsFilePath);
            var projects = JsonSerializer.Deserialize<List<Project>>(json) ?? new List<Project>();
            
            // 查找要删除的项目
            var projectToRemove = projects.FirstOrDefault(p => p.Id == id);
            if (projectToRemove == null)
            {
                return NotFound($"Project with ID {id} not found");
            }
            
            // 删除关联的文件
            var filePath = Path.Combine(_uploadsDirectory, projectToRemove.FileId);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            
            // 从列表中删除项目
            projects.Remove(projectToRemove);
            
            // 保存更新后的项目列表
            var updatedJson = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_projectsFilePath, updatedJson);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project");
            return StatusCode(500, "Error deleting project");
        }
    }
} 