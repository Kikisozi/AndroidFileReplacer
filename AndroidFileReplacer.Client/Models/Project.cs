using System;

namespace AndroidFileReplacer.Client.Models
{
    /// <summary>
    /// 项目模型类，表示一个要进行文件替换的项目
    /// </summary>
    public class Project
    {
        /// <summary>
        /// 项目唯一标识符
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 目标文件路径（安卓设备上的路径）
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;
        
        /// <summary>
        /// 服务器上文件的唯一标识符
        /// </summary>
        public string FileId { get; set; } = string.Empty;
        
        /// <summary>
        /// 原始文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// 项目创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// 获取格式化的文件大小字符串
        /// </summary>
        public string FormattedSize 
        { 
            get
            {
                if (FileSize == 0) return "0 B";
                
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double size = FileSize;
                
                while (size >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    size = size / 1024;
                }
                
                return $"{Math.Round(size, 2):0.##} {sizes[order]}";
            }
        }
        
        /// <summary>
        /// 获取格式化的创建时间
        /// </summary>
        public string FormattedDate => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    }
} 