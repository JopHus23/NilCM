using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NilCM.App.ViewModels;

namespace NilCM.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>本地文件选择：文件对话框属 View 职责，选完把路径交给 VM（VM 保持无 Avalonia 类型）。</summary>
    private async void OnAddLocalFilesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择本地音频文件",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("音频文件")
                {
                    Patterns = new[] { "*.mp3", "*.flac", "*.m4a", "*.aac", "*.wav", "*.ogg" },
                },
            },
        });

        if (files.Count > 0)
        {
            vm.AddLocalFiles(files.Select(f => f.Path.LocalPath));
        }
    }
}
