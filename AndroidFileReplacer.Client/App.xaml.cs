using System;
using System.Windows;

namespace AndroidFileReplacer.Client
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        // 删除Main方法，因为WPF框架会自动生成
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 在应用程序启动时执行的代码
        }
    }
} 