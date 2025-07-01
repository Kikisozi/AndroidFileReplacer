using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AndroidFileReplacer.Client.Models;

namespace AndroidFileReplacer.Client.Services
{
    /// <summary>
    /// API服务类，用于与后端API进行通信
    /// </summary>
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _apiKey = string.Empty;
        private string _baseUrl = string.Empty;
        private string _adbPath = string.Empty;
        private bool _isRemoteServer = false;

        /// <summary>
        /// 初始化API服务
        /// </summary>
        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 配置API服务参数
        /// </summary>
        /// <param name="baseUrl">服务器基础URL</param>
        /// <param name="apiKey">API密钥</param>
        /// <param name="adbPath">ADB工具路径</param>
        /// <param name="isRemoteServer">是否为远程服务器</param>
        public void Configure(string baseUrl, string apiKey, string adbPath = "", bool isRemoteServer = false)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _apiKey = apiKey;
            _adbPath = adbPath;
            _isRemoteServer = isRemoteServer;
            
            // 如果是远程服务器，自动判断
            if (!_isRemoteServer && !baseUrl.Contains("localhost") && !baseUrl.Contains("127.0.0.1"))
            {
                _isRemoteServer = true;
            }
            
            _httpClient.DefaultRequestHeaders.Clear();
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            }
            
            // 设置超时时间
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// 获取项目列表
        /// </summary>
        public async Task<List<Project>> GetProjectsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/projects");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Project>>(content) ?? new List<Project>();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"获取项目列表失败: {ex.Message}", ex);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("获取项目列表请求超时");
            }
            catch (Exception ex)
            {
                throw new Exception($"获取项目列表时出现未知错误: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="fileId">文件ID</param>
        public async Task<byte[]> DownloadFileAsync(string fileId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/files/{fileId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"下载文件失败: {ex.Message}", ex);
            }
            catch (TaskCanceledException)
            {
                throw new Exception("下载文件请求超时");
            }
            catch (Exception ex)
            {
                throw new Exception($"下载文件时出现未知错误: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 读取文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public async Task<FileContentResponse> ReadFileAsync(string filePath)
        {
            try
            {
                var request = new FileEditRequest { 
                    FilePath = filePath,
                    AdbPath = _isRemoteServer ? string.Empty : _adbPath // 远程服务器不发送ADB路径
                };
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/FileEditor/read", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return new FileContentResponse { 
                        Success = false, 
                        Message = $"服务器错误: {response.StatusCode} - {responseContent}" 
                    };
                }
                
                var result = JsonConvert.DeserializeObject<FileContentResponse>(responseContent);
                return result ?? new FileContentResponse { Success = false, Message = "解析响应失败" };
            }
            catch (Exception ex)
            {
                return new FileContentResponse { Success = false, Message = $"读取文件时出错: {ex.Message}" };
            }
        }
        
        /// <summary>
        /// 在文件中搜索内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="searchPattern">搜索模式</param>
        public async Task<FileEditResponse> SearchInFileAsync(string filePath, string searchPattern)
        {
            try
            {
                var request = new FileEditRequest 
                { 
                    FilePath = filePath,
                    SearchPattern = searchPattern,
                    AdbPath = _isRemoteServer ? string.Empty : _adbPath // 远程服务器不发送ADB路径
                };
                
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/FileEditor/search", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return new FileEditResponse { 
                        Success = false, 
                        Message = $"服务器错误: {response.StatusCode} - {responseContent}" 
                    };
                }
                
                var result = JsonConvert.DeserializeObject<FileEditResponse>(responseContent);
                return result ?? new FileEditResponse { Success = false, Message = "解析响应失败" };
            }
            catch (Exception ex)
            {
                return new FileEditResponse { Success = false, Message = $"搜索文件时出错: {ex.Message}" };
            }
        }
        
        /// <summary>
        /// 编辑文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="searchPattern">搜索模式</param>
        /// <param name="replaceContent">替换内容</param>
        /// <param name="deleteMatchedContent">是否删除匹配内容</param>
        public async Task<FileEditResponse> EditFileAsync(string filePath, string searchPattern, string replaceContent, bool deleteMatchedContent)
        {
            try
            {
                var request = new FileEditRequest 
                { 
                    FilePath = filePath,
                    SearchPattern = searchPattern,
                    ReplaceContent = replaceContent,
                    DeleteMatchedContent = deleteMatchedContent,
                    AdbPath = _isRemoteServer ? string.Empty : _adbPath // 远程服务器不发送ADB路径
                };
                
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/FileEditor/edit", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    return new FileEditResponse { 
                        Success = false, 
                        Message = $"服务器错误: {response.StatusCode} - {responseContent}" 
                    };
                }
                
                var result = JsonConvert.DeserializeObject<FileEditResponse>(responseContent);
                return result ?? new FileEditResponse { Success = false, Message = "解析响应失败" };
            }
            catch (Exception ex)
            {
                return new FileEditResponse { Success = false, Message = $"编辑文件时出错: {ex.Message}" };
            }
        }

        /// <summary>
        /// 写入文件内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileContent">文件内容</param>
        public async Task<FileEditResponse> WriteFileAsync(string filePath, string fileContent)
        {
            try
            {
                var request = new
                {
                    FilePath = filePath,
                    Content = fileContent,
                    AdbPath = _isRemoteServer ? string.Empty : _adbPath // 远程服务器不发送ADB路径
                };

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/FileEditor/write", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new FileEditResponse { 
                        Success = false, 
                        Message = $"服务器错误: {response.StatusCode} - {responseContent}" 
                    };
                }
                
                var result = JsonConvert.DeserializeObject<FileEditResponse>(responseContent);
                return result ?? new FileEditResponse { Success = false, Message = "解析响应失败" };
            }
            catch (Exception ex)
            {
                return new FileEditResponse { Success = false, Message = $"写入文件时出错: {ex.Message}" };
            }
        }

        /// <summary>
        /// 下载文件并推送到设备
        /// </summary>
        /// <param name="project">项目信息</param>
        /// <param name="logCallback">日志回调函数</param>
        /// <param name="pushFileCallback">推送文件回调函数</param>
        public async Task DownloadAndPushFile(Project project, Action<string> logCallback, Func<string, string, Task<(bool success, string output)>> pushFileCallback)
        {
            try
            {
                // 创建临时文件路径
                string tempFilePath = Path.Combine(Path.GetTempPath(), project.FileName);
                
                // 记录日志
                logCallback?.Invoke($"正在下载文件: {project.FileName}...");
                
                // 下载文件
                byte[] fileData = await DownloadFileAsync(project.FileId);
                
                // 写入临时文件
                await File.WriteAllBytesAsync(tempFilePath, fileData);
                logCallback?.Invoke($"文件已下载到临时位置: {tempFilePath}");
                
                // 推送文件到设备
                logCallback?.Invoke($"正在推送文件到设备: {project.TargetPath}...");
                var (success, output) = await pushFileCallback(tempFilePath, project.TargetPath);
                
                if (success)
                {
                    logCallback?.Invoke($"文件推送成功: {project.TargetPath}");
                }
                else
                {
                    logCallback?.Invoke($"文件推送失败: {output}");
                }
                
                // 删除临时文件
                try
                {
                    File.Delete(tempFilePath);
                    logCallback?.Invoke("临时文件已删除");
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"删除临时文件时出错: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"处理文件时出错: {ex.Message}");
                throw;
            }
        }
    }
} 