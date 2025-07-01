using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AndroidFileReplacer.Client.Models;
using AndroidFileReplacer.Client.Services;

namespace AndroidFileReplacer.Client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService;
        private readonly AdbService _adbService;
        private AppSettings _settings;
        private List<Project> _projects = new List<Project>();

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            _adbService = new AdbService();
            _settings = AppSettings.Load();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 默认选择第一个功能
            if (FunctionListBox.Items.Count > 0)
            {
                FunctionListBox.SelectedIndex = 0;
            }

            // 加载设置
            ServerUrlTextBox.Text = _settings.ServerUrl;
            ApiKeyTextBox.Text = _settings.ApiKey;
            AdbPathTextBox.Text = _settings.AdbPath;
            PortTextBox.Text = _settings.Port;

            // 禁用按钮直到连接成功
            RefreshButton.IsEnabled = false;
            ExecuteButton.IsEnabled = false;

            LogMessage("应用程序已启动，请连接ADB。");
        }

        private void FunctionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainTabControl != null && FunctionListBox.SelectedIndex != -1)
            {
                MainTabControl.SelectedIndex = FunctionListBox.SelectedIndex;
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettings();
                _apiService.Configure(_settings.ServerUrl, _settings.ApiKey);
                _adbService.SetAdbPath(_settings.AdbPath);

                ConnectButton.IsEnabled = false;
                LogMessage("正在连接ADB...");
                var (success, output) = await _adbService.ConnectAsync("127.0.0.1", _settings.Port);

                if (success)
                {
                    LogMessage($"ADB连接成功。");
                    RefreshButton.IsEnabled = true;
                    await RefreshProjects();
                }
                else
                {
                    LogMessage($"ADB连接失败: {output}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"连接错误: {ex.Message}");
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshProjects();
        }

        private async Task RefreshProjects()
        {
            try
            {
                SaveSettings();
                _apiService.Configure(_settings.ServerUrl, _settings.ApiKey);

                RefreshButton.IsEnabled = false;
                LogMessage("正在获取项目列表...");
                _projects = await _apiService.GetProjectsAsync();

                ProjectsComboBox.ItemsSource = _projects;
                ProjectsComboBox.DisplayMemberPath = "Name";

                if (_projects.Count > 0)
                {
                    ProjectsComboBox.SelectedIndex = 0;
                    ExecuteButton.IsEnabled = true;
                    LogMessage($"已获取到 {_projects.Count} 个项目。");
                }
                else
                {
                    ExecuteButton.IsEnabled = false;
                    LogMessage("没有可用的远程项目。");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"获取项目列表失败: {ex.Message}");
                ExecuteButton.IsEnabled = false;
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        private void SaveSettings()
        {
            _settings.ServerUrl = ServerUrlTextBox.Text;
            _settings.ApiKey = ApiKeyTextBox.Text;
            _settings.AdbPath = AdbPathTextBox.Text;
            _settings.Port = PortTextBox.Text;
            _settings.Save();
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            if (LogTextBox.Text.Length > 5000) LogTextBox.Clear();
            LogTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            LogTextBox.ScrollToEnd();
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ProjectsComboBox.SelectedItem is not Project selectedProject)
                {
                    LogMessage("请先选择一个远程项目。");
                    return;
                }

                ExecuteButton.IsEnabled = false;
                LogMessage($"开始执行替换操作: {selectedProject.Name}");
                await _apiService.DownloadAndPushFile(selectedProject, LogMessage, _adbService.PushFileAsync);
            }
            catch (Exception ex)
            {
                LogMessage($"执行替换时发生错误: {ex.Message}");
            }
            finally
            {
                ExecuteButton.IsEnabled = true;
            }
        }
    }
} 