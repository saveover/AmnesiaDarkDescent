// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SaveOver.AmnesiaDarkDescent.Helpers;
using SaveOver.AmnesiaDarkDescent.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace SaveOver.AmnesiaDarkDescent.Pages;

public sealed partial class HomePage : Page
{
    private readonly ILogger<HomePage> logger = App.LoggerFactory.CreateLogger<HomePage>();
    private bool isBusy;
    private bool startupResumePromptHandled;

    public HomePage()
    {
        InitializeComponent();
        App.CurrentSaveData.SaveDataChanged += OnSessionChanged;
        App.CurrentSaveData.DirtyStateChanged += OnSessionChanged;
        Loaded += HomePage_Loaded;
        UpdateWorkspaceState();
    }

    private async void LoadFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? filePath = await FileHelper.PickFileAsync();
            if (filePath is not null)
            {
                await LoadFileAsync(filePath);
            }
        }
        catch (Exception ex)
        {
            ShowError("Couldn't open the file picker", ex);
        }
    }

    private async Task LoadFileAsync(string filePath)
    {
        if (!string.Equals(Path.GetExtension(filePath), ".sav", StringComparison.OrdinalIgnoreCase))
        {
            ShowError("Unsupported file", new InvalidDataException("Choose an Amnesia .sav file."));
            return;
        }

        if (!await ConfirmDiscardChangesAsync())
        {
            return;
        }

        SetBusy(true);
        try
        {
            string content = await FileHelper.LoadSaveFileAsync(filePath);
            PlayerData player = await Task.Run(() => AmnesiaSaveCodec.Parse(content));
            App.CurrentSaveData.Load(filePath, content, player);
            if (SaveSettings.RememberLastOpenedSave)
            {
                SaveSettings.LastOpenedSavePath = filePath;
            }

            OperationInfoBar.Severity = InfoBarSeverity.Success;
            OperationInfoBar.Title = "Save opened";
            OperationInfoBar.Message = "Character editing is now available.";
            OperationInfoBar.IsOpen = true;
            logger.LogInformation("A save file was loaded successfully.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            if (string.Equals(SaveSettings.LastOpenedSavePath, filePath, StringComparison.OrdinalIgnoreCase))
            {
                SaveSettings.LastOpenedSavePath = null;
            }

            ShowError("Couldn't open this save", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSession session = App.CurrentSaveData;
        if (!session.IsLoaded || session.Player is null || session.SourceFilePath is null || session.OriginalContent is null)
        {
            return;
        }

        if (SaveSettings.ConfirmBeforeSaving && !await ConfirmSaveAsync(Path.GetFileName(session.SourceFilePath)))
        {
            return;
        }

        SetBusy(true);
        try
        {
            string updatedContent = await Task.Run(() => AmnesiaSaveCodec.Write(session.OriginalContent, session.Player));
            await FileHelper.SaveSaveFileAsync(session.SourceFilePath, updatedContent);
            session.CommitSavedContent(updatedContent);

            OperationInfoBar.Severity = InfoBarSeverity.Success;
            OperationInfoBar.Title = "Changes saved";
            OperationInfoBar.Message = "The original was backed up before the save was replaced.";
            OperationInfoBar.IsOpen = true;
            logger.LogInformation("A save file was written successfully.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ShowError("Couldn't save changes", ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void HomePage_Loaded(object sender, RoutedEventArgs e)
    {
        if (startupResumePromptHandled || App.CurrentSaveData.IsLoaded)
        {
            return;
        }

        startupResumePromptHandled = true;
        if (!SaveSettings.RememberLastOpenedSave ||
            SaveSettings.LastOpenedSavePath is not { Length: > 0 } lastSavePath)
        {
            return;
        }

        if (!File.Exists(lastSavePath))
        {
            SaveSettings.LastOpenedSavePath = null;
            OperationInfoBar.Severity = InfoBarSeverity.Warning;
            OperationInfoBar.Title = "Previous save not found";
            OperationInfoBar.Message = "The previously opened save file could not be found.";
            OperationInfoBar.IsOpen = true;
            return;
        }

        StackPanel dialogContent = new() { Spacing = 10 };
        dialogContent.Children.Add(new TextBlock
        {
            Text = "Reopen the save used in your previous SaveOver session?",
            TextWrapping = TextWrapping.Wrap,
        });
        dialogContent.Children.Add(new TextBlock
        {
            Text = lastSavePath,
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true,
            Opacity = 0.7,
        });
        dialogContent.Children.Add(new TextBlock
        {
            Text = "Reopening does not change the file. Nothing is written until you save an edit.",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.7,
        });

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            RequestedTheme = ActualTheme,
            Title = "Reopen previous save?",
            Content = dialogContent,
            PrimaryButtonText = "Reopen",
            CloseButtonText = "Not now",
            DefaultButton = ContentDialogButton.Primary,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await LoadFileAsync(lastSavePath);
        }
    }

    private void SaveDropZone_DragOver(object sender, DragEventArgs e)
    {
        if (!isBusy && e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Open Amnesia save";
            e.DragUIOverride.IsContentVisible = true;
        }
    }

    private async void SaveDropZone_Drop(object sender, DragEventArgs e)
    {
        try
        {
            IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
            StorageFile? file = items.Count == 1 ? items[0] as StorageFile : null;
            if (file is null)
            {
                ShowError("Couldn't open dropped items", new InvalidDataException("Drop exactly one Amnesia .sav file."));
                return;
            }

            await LoadFileAsync(file.Path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            ShowError("Couldn't open the dropped save", ex);
        }
    }

    private void OpenSaveFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(BackupSettings.GameSaveFolderPath))
            {
                throw new DirectoryNotFoundException("The Amnesia save folder does not exist yet.");
            }

            _ = Process.Start(new ProcessStartInfo(BackupSettings.GameSaveFolderPath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowError("Couldn't open the save folder", ex);
        }
    }

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!App.CurrentSaveData.HasUnsavedChanges)
        {
            return true;
        }

        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Discard unsaved changes?",
            Content = "Opening another save will discard the edits that have not been saved.",
            PrimaryButtonText = "Discard changes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private async Task<bool> ConfirmSaveAsync(string fileName)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = "Save changes?",
            Content = $"Save changes to {fileName}? A backup will be created first.",
            PrimaryButtonText = "Save changes",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private void OnSessionChanged(object? sender, EventArgs e) => UpdateWorkspaceState();

    private void UpdateWorkspaceState()
    {
        SaveSession session = App.CurrentSaveData;
        bool loaded = session.IsLoaded;
        WorkspaceTitleTextBlock.Text = loaded ? "Save file open" : "Choose a save file";
        LoadFileTextBlock.Text = loaded
            ? $"{Path.GetFileName(session.SourceFilePath)}{(session.HasUnsavedChanges ? " — unsaved changes" : string.Empty)}"
            : "No save file is currently open.";
        SaveFileButton.IsEnabled = loaded && session.HasUnsavedChanges && !isBusy;
        LoadFileButton.IsEnabled = !isBusy;
    }

    private void SetBusy(bool value)
    {
        isBusy = value;
        App.StartupWindow?.SetWorkspaceBusy(value);
        UpdateWorkspaceState();
    }

    private void ShowError(string title, Exception exception)
    {
        OperationInfoBar.Severity = InfoBarSeverity.Error;
        OperationInfoBar.Title = title;
        OperationInfoBar.Message = exception.Message;
        OperationInfoBar.IsOpen = true;
        logger.LogError(exception, "{OperationTitle}.", title);
    }
}
