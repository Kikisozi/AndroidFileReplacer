# 安卓文件替换工具

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

一个用于远程管理和替换安卓设备文件的跨平台工具，使用 C#/.NET 开发的全栈解决方案。

## 主要功能

- **远程文件管理**：通过直观的界面管理和替换安卓设备上的文件
- **批量操作**：支持多个项目的一键替换部署
- **Web管理界面**：通过浏览器进行项目管理和文件上传
- **桌面客户端**：功能丰富的WPF桌面应用
- **安全性**：API密钥认证保护服务接口

## 项目结构

```
AndroidFileReplacer/
├── AndroidFileReplacer.API/       # ASP.NET Core Web API 服务器
├── AndroidFileReplacer.Client/    # WPF 桌面客户端
└── README.md                      # 项目说明文档
```

## 系统要求

- **.NET 9.0** 或更高版本
- **ADB** (Android Debug Bridge) 工具
- 支持Windows, macOS 和 Linux (通过 .NET Core 兼容性)

## 安装与配置

### 服务器端配置

1. 在项目目录中运行：
   ```
   cd AndroidFileReplacer.API
   dotnet build
   ```

2. 编辑 `appsettings.json` 配置文件：
   ```json
   {
     "ApiKey": "YOUR_CUSTOM_API_KEY_HERE",
     "Logging": {
       "LogLevel": {
         "Default": "Information"
       }
     }
   }
   ```

3. 启动服务器：
   ```
   dotnet run
   ```

### 客户端配置

1. 编译客户端：
   ```
   cd AndroidFileReplacer.Client
   dotnet build
   ```

2. 运行客户端应用，并配置：
   - 服务器URL (默认: http://localhost:5057)
   - API密钥 (与服务器配置一致)
   - ADB路径 (如果不在系统PATH中)

## 使用说明

### Web界面

1. 在浏览器中访问 http://localhost:5057
2. 输入API密钥进行认证
3. 创建项目：
   - 输入项目名称
   - 设置目标路径（安卓设备上的绝对路径）
   - 上传需要替换的文件

### 桌面客户端

1. 启动桌面客户端
2. 配置服务器连接
3. 连接安卓设备 (通过USB或TCP/IP)
4. 选择要部署的项目
5. 执行替换操作

## 开发指南

### 扩展API功能

API项目使用标准ASP.NET Core Web API架构：

- `Controllers/`: API接口控制器
- `Models/`: 数据模型
- `Filters/`: API认证与过滤器

添加新功能时，遵循以下步骤：

1. 在Models目录中定义数据模型
2. 在Controllers目录中创建控制器
3. 应用ApiKey过滤器确保安全性

### 修改客户端

客户端项目使用WPF框架，采用MVVM模式：

- `Models/`: 数据模型
- `Services/`: 服务类，包括ADB和API通信
- `*.xaml` 和 `*.xaml.cs`: UI界面和逻辑

## 安全注意事项

- **更改默认API密钥**: 在生产环境中始终修改默认API密钥
- **限制访问**: 仅在受信任的网络中暴露API服务
- **文件权限**: 确保替换操作不会破坏设备功能

## 常见问题

### ADB连接问题

确保：
1. 设备已启用USB调试模式
2. 已正确配置ADB路径
3. 设备已授权连接的计算机

### 文件替换失败

可能原因：
1. 目标路径不存在
2. 缺少写入权限
3. 设备存储空间不足

## 贡献指南

欢迎贡献代码、报告问题或提出新功能建议！

1. Fork项目
2. 创建特性分支
3. 提交更改
4. 推送到分支
5. 创建Pull Request

## 许可证

本项目基于MIT许可证开源 - 详见[LICENSE](LICENSE)文件。 