using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace AndroidFileReplacer.Client.Services
{
    /// <summary>
    /// ADB服务类，用于与Android设备通信
    /// </summary>
    public class AdbService
    {
        private string _adbPath = "adb.exe";
        private static readonly Regex DeviceRegex = new Regex(@"^([^\s]+)\s+device", RegexOptions.Multiline);
        
        /// <summary>
        /// 设置ADB工具路径
        /// </summary>
        /// <param name="path">ADB工具路径</param>
        /// <exception cref="FileNotFoundException">找不到ADB工具时抛出</exception>
        public void SetAdbPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                _adbPath = path;
                // 确保ADB路径可用
                if (!File.Exists(_adbPath))
                {
                    throw new FileNotFoundException($"找不到ADB工具: {_adbPath}", _adbPath);
                }
            }
        }

        /// <summary>
        /// 连接到设备
        /// </summary>
        /// <param name="host">设备IP地址</param>
        /// <param name="port">设备端口</param>
        /// <returns>连接结果</returns>
        public async Task<(bool Success, string Output)> ConnectAsync(string host, string port)
        {
            return await ExecuteCommandAsync($"connect {host}:{port}");
        }
        
        /// <summary>
        /// 检查是否有设备连接
        /// </summary>
        /// <returns>如果有设备连接则返回true</returns>
        public async Task<bool> HasDeviceConnectedAsync()
        {
            var result = await GetDevicesAsync();
            if (!result.Success) return false;
            
            // 使用正则表达式查找设备列表
            return DeviceRegex.IsMatch(result.Output);
        }
        
        /// <summary>
        /// 获取已连接设备列表
        /// </summary>
        /// <returns>设备列表，每行一个设备信息</returns>
        public async Task<(bool Success, string Output, List<string> Devices)> GetDeviceListAsync()
        {
            var result = await GetDevicesAsync();
            if (!result.Success) return (false, result.Output, new List<string>());
            
            var devices = new List<string>();
            var matches = DeviceRegex.Matches(result.Output);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    devices.Add(match.Groups[1].Value);
                }
            }
            
            return (true, result.Output, devices);
        }

        /// <summary>
        /// 将文件推送到设备
        /// </summary>
        /// <param name="localFilePath">本地文件路径</param>
        /// <param name="remotePath">设备上的目标路径</param>
        /// <returns>操作结果</returns>
        public async Task<(bool Success, string Output)> PushFileAsync(string localFilePath, string remotePath)
        {
            // 确保本地文件存在
            if (!File.Exists(localFilePath))
            {
                return (false, $"找不到本地文件: {localFilePath}");
            }
            
            return await ExecuteCommandAsync($"push \"{localFilePath}\" \"{remotePath}\"");
        }

        /// <summary>
        /// 检查设备上的文件是否存在
        /// </summary>
        /// <param name="remotePath">设备上的文件路径</param>
        /// <returns>检查结果</returns>
        public async Task<(bool Success, string Output, bool Exists)> CheckFileExistsAsync(string remotePath)
        {
            var result = await ExecuteCommandAsync($"shell [ -e \"{remotePath}\" ] && echo \"File exists\" || echo \"File does not exist\"");
            return (result.Success, result.Output, result.Output.Contains("File exists"));
        }

        /// <summary>
        /// 从设备中删除文件
        /// </summary>
        /// <param name="remotePath">设备上的文件路径</param>
        /// <returns>操作结果</returns>
        public async Task<(bool Success, string Output)> DeleteFileAsync(string remotePath)
        {
            return await ExecuteCommandAsync($"shell rm \"{remotePath}\"");
        }

        /// <summary>
        /// 从设备拉取文件到本地
        /// </summary>
        /// <param name="remotePath">设备上的文件路径</param>
        /// <param name="localFilePath">本地目标路径</param>
        /// <returns>操作结果</returns>
        public async Task<(bool Success, string Output)> PullFileAsync(string remotePath, string localFilePath)
        {
            return await ExecuteCommandAsync($"pull \"{remotePath}\" \"{localFilePath}\"");
        }

        /// <summary>
        /// 读取设备上文件的内容
        /// </summary>
        /// <param name="remotePath">设备上的文件路径</param>
        /// <returns>文件内容</returns>
        public async Task<(bool Success, string Output, string Content)> ReadFileContentAsync(string remotePath)
        {
            // 首先检查文件是否存在
            var existsResult = await CheckFileExistsAsync(remotePath);
            if (existsResult.Success && !existsResult.Exists)
            {
                return (false, $"远程文件不存在: {remotePath}", string.Empty);
            }
            
            var result = await ExecuteCommandAsync($"shell cat \"{remotePath}\"");
            return (result.Success, result.Output, result.Success ? result.Output : string.Empty);
        }

        /// <summary>
        /// 获取已连接的设备信息
        /// </summary>
        /// <returns>设备信息</returns>
        public async Task<(bool Success, string Output)> GetDevicesAsync()
        {
            return await ExecuteCommandAsync("devices");
        }

        /// <summary>
        /// 执行ADB命令
        /// </summary>
        /// <param name="command">要执行的命令</param>
        /// <returns>命令输出</returns>
        public async Task<(bool Success, string Output)> ExecuteCommandAsync(string command)
        {
            // 确保ADB路径有效
            if (!File.Exists(_adbPath))
            {
                return (false, $"ADB工具不存在: {_adbPath}");
            }
            
            var startInfo = new ProcessStartInfo
            {
                FileName = _adbPath,
                Arguments = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // 设置超时时间，避免长时间挂起
                if (!await Task.Run(() => process.WaitForExit(30000))) // 30秒超时
                {
                    try
                    {
                        process.Kill();
                        return (false, "ADB命令执行超时");
                    }
                    catch
                    {
                        return (false, "ADB命令执行超时，且无法终止进程");
                    }
                }

                var output = outputBuilder.ToString().TrimEnd();
                var error = errorBuilder.ToString().TrimEnd();

                // 检查是否包含常见的错误信息
                if (output.Contains("no devices/emulators found") || 
                    output.Contains("device offline") || 
                    error.Contains("no devices/emulators found") || 
                    error.Contains("device offline"))
                {
                    return (false, "未找到设备或设备处于离线状态，请检查设备连接");
                }

                // 检查权限错误
                if (output.Contains("Permission denied") || error.Contains("Permission denied"))
                {
                    return (false, "无法访问文件或目录，权限被拒绝");
                }

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    string errorMsg = string.IsNullOrEmpty(error) ? output : error;
                    return (false, errorMsg);
                }

                return (true, output);
            }
            catch (Exception ex)
            {
                return (false, $"执行ADB命令时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取设备当前路径
        /// </summary>
        public async Task<(bool Success, string Output, string Path)> GetCurrentPathAsync()
        {
            var result = await ExecuteCommandAsync("shell pwd");
            return (result.Success, result.Output, result.Success ? result.Output.Trim() : string.Empty);
        }
        
        /// <summary>
        /// 列出设备上目录中的文件
        /// </summary>
        /// <param name="remotePath">设备上的目录路径</param>
        public async Task<(bool Success, string Output, string[] Files)> ListDirectoryAsync(string remotePath)
        {
            var result = await ExecuteCommandAsync($"shell ls -la \"{remotePath}\"");
            if (!result.Success) return (false, result.Output, Array.Empty<string>());
            
            var lines = result.Output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return (true, result.Output, lines);
        }
    }
} 