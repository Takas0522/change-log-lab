using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OrderClientApp.Application.Abstractions.Backup;
using OrderClientApp.Application.Abstractions.Operations;
using OrderClientApp.Application.Abstractions.Settings;
using MessageBox = System.Windows.MessageBox;

namespace OrderClientApp.Wpf;

public partial class SettingsWindow : Window
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly IBackupService _backupService;
    private readonly IOperationLogService _operationLogService;
    private readonly string _username;

    public SettingsWindow(
        IAppSettingsService appSettingsService,
        IBackupService backupService,
        IOperationLogService operationLogService,
        string username)
    {
        _appSettingsService = appSettingsService;
        _backupService = backupService;
        _operationLogService = operationLogService;
        _username = username;
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var settings = await _appSettingsService.GetAsync();
        CompanyNameTextBox.Text = settings.CompanyName;
        CompanyAddressTextBox.Text = settings.CompanyAddress;
        ApprovalThresholdTextBox.Text = settings.ApprovalThreshold.ToString(CultureInfo.InvariantCulture);
        ThemeComboBox.SelectedIndex = string.Equals(settings.Theme, "Dark", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        InfoTextBlock.Text = $"最終更新: {settings.UpdatedAtUtc.LocalDateTime:yyyy/MM/dd HH:mm}";
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!decimal.TryParse(ApprovalThresholdTextBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var threshold))
            {
                throw new InvalidOperationException("承認閾値の形式が正しくありません。");
            }

            var selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Light";
            var saved = await _appSettingsService.SaveAsync(
                new SaveAppSettingsRequest(
                    CompanyNameTextBox.Text,
                    CompanyAddressTextBox.Text,
                    threshold,
                    selectedTheme,
                    _username));
            ThemeManager.ApplyTheme(saved.Theme);
            await LoadAsync();
            MessageBox.Show("設定を保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存に失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "バックアップ先フォルダを選択してください。"
        };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            return;
        }

        try
        {
            var backupPath = await _backupService.CreateManualBackupAsync(dialog.FolderName);
            MessageBox.Show($"バックアップを作成しました。\n{backupPath}", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"バックアップに失敗しました。\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new OperationLogWindow(_operationLogService)
        {
            Owner = this
        };
        window.ShowDialog();
    }
}
