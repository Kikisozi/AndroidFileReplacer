using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AndroidFileReplacer.API.Filters;

namespace AndroidFileReplacer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[ApiKey]
public class FileEditorController : ControllerBase
{
    private readonly ILogger<FileEditorController> _logger;
    private string _adbPath; // 改为非只读字段，允许在运行时更改

    public FileEditorController(ILogger<FileEditorController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        
        // 优先使用当前目录下的adb.exe
        string baseAdbPath = Path.Combine(AppContext.BaseDirectory, "adb.exe");
        if (System.IO.File.Exists(baseAdbPath))
        {
            _adbPath = baseAdbPath;
            _logger.LogInformation($"使用内置ADB工具: {_adbPath}");
        }
        else
        {
            // 回退到路径环境变量
            _adbPath = "adb";
            _logger.LogWarning($"内置ADB工具未找到，将使用系统PATH中的adb命令");
        }
    }

    [HttpPost("read")]
    public async Task<IActionResult> ReadFile([FromBody] FileEditRequest request)
    {
        try
        {
            _logger.LogInformation($"读取文件: {request.FilePath}");

            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                return BadRequest(new FileContentResponse { Success = false, Message = "文件路径不能为空" });
            }

            // 如果客户端提供了自定义ADB路径，更新本地变量
            if (!string.IsNullOrEmpty(request.AdbPath))
            {
                _logger.LogInformation($"使用客户端提供的ADB路径: {request.AdbPath}");
                _adbPath = request.AdbPath;
            }

            // 执行ADB命令读取文件
            var (success, output, error) = await ExecuteAdbCommand($"shell cat \"{request.FilePath}\"");
            
            if (!success)
            {
                return BadRequest(new FileContentResponse { Success = false, Message = $"读取文件失败: {error}" });
            }

            return Ok(new FileContentResponse
            {
                Content = output,
                Success = true,
                Message = "文件读取成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"读取文件时发生错误: {ex.Message}");
            return StatusCode(500, new FileContentResponse { Success = false, Message = $"服务器错误: {ex.Message}" });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchInFile([FromBody] FileEditRequest request)
    {
        try
        {
            _logger.LogInformation($"在文件中搜索: {request.FilePath}, 搜索内容: {request.SearchPattern}");

            if (string.IsNullOrWhiteSpace(request.FilePath) || string.IsNullOrWhiteSpace(request.SearchPattern))
            {
                return BadRequest(new FileEditResponse { Success = false, Message = "文件路径和搜索内容不能为空" });
            }
            
            // 如果客户端提供了自定义ADB路径，更新本地变量
            if (!string.IsNullOrEmpty(request.AdbPath))
            {
                _logger.LogInformation($"使用客户端提供的ADB路径: {request.AdbPath}");
                _adbPath = request.AdbPath;
            }

            // 先读取文件内容
            var (readSuccess, originalContent, readError) = await ExecuteAdbCommand($"shell cat \"{request.FilePath}\"");
            
            if (!readSuccess)
            {
                return BadRequest(new FileEditResponse { Success = false, Message = $"读取文件失败: {readError}" });
            }

            // 检查是否包含搜索内容
            if (!originalContent.Contains(request.SearchPattern))
            {
                return Ok(new FileEditResponse 
                { 
                    Success = false, 
                    Message = $"文件中未找到搜索内容: {request.SearchPattern}",
                    NewContent = string.Empty
                });
            }

            // 按行分割，找出所有包含关键字的行
            string[] lines = originalContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var matchedLines = new List<string>();

            foreach (var line in lines)
            {
                if (line.Contains(request.SearchPattern, StringComparison.OrdinalIgnoreCase))
                {
                    matchedLines.Add(line);
                }
            }
            
            // 将匹配的行去重
            var uniqueMatchedLines = matchedLines.Distinct().ToList();

            if (uniqueMatchedLines.Count == 0)
            {
                return Ok(new FileEditResponse
                {
                    Success = false,
                    Message = $"文件中未找到搜索内容: {request.SearchPattern}",
                    NewContent = string.Empty
                });
            }

            // 只返回第一个唯一的匹配行
            string searchResult = uniqueMatchedLines.First();

            return Ok(new FileEditResponse
            {
                Success = true,
                Message = "搜索成功，已找到匹配行。",
                NewContent = searchResult
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"搜索文件时发生错误: {ex.Message}");
            return StatusCode(500, new FileEditResponse { Success = false, Message = $"服务器错误: {ex.Message}" });
        }
    }

    [HttpPost("edit")]
    public async Task<IActionResult> EditFile([FromBody] FileEditRequest request)
    {
        try
        {
            _logger.LogInformation($"编辑文件: {request.FilePath}");
            
            if (string.IsNullOrWhiteSpace(request.FilePath) || string.IsNullOrWhiteSpace(request.SearchPattern))
            {
                return BadRequest(new FileEditResponse { Success = false, Message = "文件路径和搜索内容不能为空" });
            }
            
            if (!string.IsNullOrEmpty(request.AdbPath))
            {
                _logger.LogInformation($"使用客户端提供的ADB路径: {request.AdbPath}");
                _adbPath = request.AdbPath;
            }

            var (readSuccess, originalContent, readError) = await ExecuteAdbCommand($"shell cat \"{request.FilePath}\"");
            
            if (!readSuccess)
            {
                return BadRequest(new FileEditResponse { Success = false, Message = $"读取文件失败: {readError}" });
            }

            // 使用正则表达式按通用换行符分割，这更可靠
            string[] lines = Regex.Split(originalContent, @"\r\n?|\n");
            
            var newLines = new List<string>();
            bool contentChanged = false;
            
            foreach (var line in lines)
            {
                if (line.Contains(request.SearchPattern))
                {
                    contentChanged = true;
                    if (!request.DeleteMatchedContent)
                    {
                        // 替换整行
                        newLines.Add(request.ReplaceContent);
                    }
                    // 如果是删除，则不添加该行到 newLines
                }
                else
                {
                    newLines.Add(line);
                }
            }
            
            if (!contentChanged)
            {
                return Ok(new FileEditResponse
                {
                    Success = false,
                    Message = "未找到匹配的行，文件未作修改。",
                    NewContent = originalContent
                });
            }
            
            // 使用标准的 \n 换行符重新组合文件
            string newContent = string.Join("\n", newLines);
            
            string tempFileName = Path.GetTempFileName();
            await System.IO.File.WriteAllTextAsync(tempFileName, newContent, new UTF8Encoding(false));

            var (pushSuccess, _, pushError) = await ExecuteAdbCommand($"push \"{tempFileName}\" \"{request.FilePath}\"");
            
            try
            {
                System.IO.File.Delete(tempFileName);
            }
            catch { /* 忽略错误 */ }

            if (!pushSuccess)
            {
                return BadRequest(new FileEditResponse { Success = false, Message = $"写入文件失败: {pushError}" });
            }

            return Ok(new FileEditResponse
            {
                Success = true,
                Message = "文件编辑成功",
                NewContent = newContent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"编辑文件时发生错误: {ex.Message}");
            return StatusCode(500, new FileEditResponse { Success = false, Message = $"服务器错误: {ex.Message}" });
        }
    }

    [HttpPost("write")]
    public async Task<IActionResult> WriteFile([FromBody] FileWriteRequest request)
    {
        try
        {
            _logger.LogInformation($"写入文件: {request.FilePath}");
            
            if (string.IsNullOrWhiteSpace(request.FilePath) || request.Content == null)
            {
                return BadRequest(new FileEditResponse { Success = false, Message = "文件路径和内容不能为空" });
            }
            
            if (!string.IsNullOrEmpty(request.AdbPath))
            {
                _logger.LogInformation($"使用客户端提供的ADB路径: {request.AdbPath}");
                _adbPath = request.AdbPath;
            }
            
            // 创建临时文件
            string tempFileName = Path.GetTempFileName();
            await System.IO.File.WriteAllTextAsync(tempFileName, request.Content, new UTF8Encoding(false));
            
            // 上传文件到设备
            var (pushSuccess, _, pushError) = await ExecuteAdbCommand($"push \"{tempFileName}\" \"{request.FilePath}\"");
            
            try
            {
                System.IO.File.Delete(tempFileName);
            }
            catch { /* 忽略错误 */ }
            
            if (!pushSuccess)
            {
                return BadRequest(new FileEditResponse { Success = false, Message = $"写入文件失败: {pushError}" });
            }
            
            return Ok(new FileEditResponse
            {
                Success = true,
                Message = "文件写入成功",
                NewContent = request.Content
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"写入文件时发生错误: {ex.Message}");
            return StatusCode(500, new FileEditResponse { Success = false, Message = $"服务器错误: {ex.Message}" });
        }
    }

    private async Task<(bool Success, string Output, string Error)> ExecuteAdbCommand(string arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _adbPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = startInfo;
            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogWarning($"ADB命令执行失败，退出代码: {process.ExitCode}, 错误信息: {error}");
                return (false, output, error);
            }

            return (true, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"执行ADB命令时出错: {ex.Message}");
            return (false, string.Empty, ex.Message);
        }
    }
}

public class FileEditRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string SearchPattern { get; set; } = string.Empty;
    public string ReplaceContent { get; set; } = string.Empty;
    public bool DeleteMatchedContent { get; set; } = false;
    public string AdbPath { get; set; } = string.Empty; // 添加ADB路径属性
}

public class FileContentResponse
{
    public string Content { get; set; } = string.Empty;
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
}

public class FileEditResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public string NewContent { get; set; } = string.Empty;
}

public class FileWriteRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AdbPath { get; set; } = string.Empty;
}
