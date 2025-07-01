using System;
using System.IO;
using Newtonsoft.Json;

namespace AndroidFileReplacer.Client.Models
{
    public class AppSettings
    {
        public string ServerUrl { get; set; } = "http://localhost:5057";
        public string ApiKey { get; set; } = string.Empty; // 移除默认硬编码API密钥
        public string AdbPath { get; set; } = "adb.exe";
        public string Host { get; set; } = "127.0.0.1";
        public string Port { get; set; } = "5555";
        
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AndroidFileReplacer",
            "settings.json");
            
        public static AppSettings Load()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }
                
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                    
                    // 如果配置中没有设置API密钥，提示用户需要设置
                    if (string.IsNullOrEmpty(settings.ApiKey))
                    {
                        settings.ApiKey = string.Empty; // 确保为空字符串而不是null
                    }
                    
                    return settings;
                }
                
                // 首次运行时创建默认设置文件
                var defaultSettings = new AppSettings();
                defaultSettings.Save();
                return defaultSettings;
            }
            catch (Exception ex)
            {
                // 错误处理，记录日志或显示警告
                Console.WriteLine($"加载配置时出错: {ex.Message}");
                return new AppSettings();
            }
        }
        
        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(directory) && directory != null)
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                // 错误处理，记录日志或显示警告
                Console.WriteLine($"保存配置时出错: {ex.Message}");
            }
        }
    }
} 