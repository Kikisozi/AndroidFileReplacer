using System;

namespace AndroidFileReplacer.Client.Models
{
    /// <summary>
    /// 文件编辑请求类，用于向服务器发送文件编辑操作
    /// </summary>
    public class FileEditRequest
    {
        /// <summary>
        /// 目标文件路径，需要是设备上的绝对路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 要查找的文本模式
        /// </summary>
        public string SearchPattern { get; set; } = string.Empty;
        
        /// <summary>
        /// 替换后的内容
        /// </summary>
        public string ReplaceContent { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否删除匹配的内容（而不是替换）
        /// </summary>
        public bool DeleteMatchedContent { get; set; } = false;
        
        /// <summary>
        /// ADB工具路径，如果为空则使用服务器默认设置
        /// </summary>
        public string AdbPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// 文件内容响应类，用于服务器返回文件读取结果
    /// </summary>
    public class FileContentResponse
    {
        /// <summary>
        /// 文件内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; } = false;
        
        /// <summary>
        /// 操作结果消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 文件编辑响应类，用于服务器返回文件编辑结果
    /// </summary>
    public class FileEditResponse
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success { get; set; } = false;
        
        /// <summary>
        /// 操作结果消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// 编辑后的文件内容
        /// </summary>
        public string NewContent { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 文件编辑配置
    /// </summary>
    public class FileEditSettings
    {
        /// <summary>
        /// 配置名称
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// 搜索模式
        /// </summary>
        public string SearchPattern { get; set; } = string.Empty;
        
        /// <summary>
        /// 替换内容
        /// </summary>
        public string ReplaceContent { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否删除匹配内容
        /// </summary>
        public bool DeleteMatchedContent { get; set; } = false;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 