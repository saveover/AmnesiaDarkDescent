// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using Microsoft.Extensions.Logging;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SaveOver.AmnesiaDarkDescent.Helpers;

/// <summary>
/// Enforces strict UTF-8 loading, bounded input, backup-before-write, and same-volume atomic file
/// replacement for every save operation.
/// </summary>
internal static class FileHelper
{
    private const long MaxFileSize = 50L * 1024 * 1024;
    private const int CopyBufferSize = 80 * 1024;
    private const string BackupFileSuffix = "_backup_";
    private const string BackupDateFormat = "yyyyMMdd_HHmmss";
    private static readonly ILogger Logger = App.LoggerFactory.CreateLogger(typeof(FileHelper).FullName!);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    internal static async Task<string?> PickFileAsync(CancellationToken cancellationToken = default)
    {
        FileOpenPicker picker = new(App.StartupWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            FileTypeFilter = { ".sav" },
        };

        try
        {
            PickFileResult? result = await picker.PickSingleFileAsync()
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
            return result?.Path;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            throw new InvalidOperationException("An error occurred while opening the file picker.", ex);
        }
    }

    internal static async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        FolderPicker picker = new(App.StartupWindow!.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            CommitButtonText = "Select folder",
            SettingsIdentifier = "BackupFolder",
        };

        try
        {
            PickFolderResult? result = await picker.PickSingleFolderAsync()
                .AsTask(cancellationToken)
                .ConfigureAwait(false);
            return result?.Path;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            throw new InvalidOperationException("An error occurred while opening the folder picker.", ex);
        }
    }

    internal static async Task<string> LoadSaveFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        cancellationToken.ThrowIfCancellationRequested();

        FileInfo fileInfo = new(filePath);
        if (!fileInfo.Exists || fileInfo.Length is 0 or > MaxFileSize)
        {
            throw new InvalidDataException($"'{Path.GetFileName(filePath)}' is not a valid Amnesia save file.");
        }

        try
        {
            byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken).ConfigureAwait(false);
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException($"'{Path.GetFileName(filePath)}' is not valid UTF-8.", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"An error occurred while loading '{filePath}'.", ex);
        }
    }

    internal static async Task SaveSaveFileAsync(
        string filePath,
        string content,
        string expectedOriginalContent,
        bool createBackup = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentException.ThrowIfNullOrEmpty(expectedOriginalContent);

        string onDiskContent = await LoadSaveFileAsync(filePath, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(onDiskContent, expectedOriginalContent, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "The save file changed on disk after it was opened. Reload it before saving so external changes are not overwritten.");
        }

        if (createBackup)
        {
            await CreateBackupAsync(filePath, cancellationToken).ConfigureAwait(false);
        }

        string directory = Path.GetDirectoryName(filePath)
            ?? throw new IOException($"Could not determine the directory for '{filePath}'.");
        string tempPath = Path.Combine(directory, $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(tempPath, content, new UTF8Encoding(false), cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Replace(tempPath, filePath, destinationBackupFileName: null, ignoreMetadataErrors: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"An error occurred while saving '{filePath}'.", ex);
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    private static async Task CreateBackupAsync(string filePath, CancellationToken cancellationToken)
    {
        string directory = BackupSettings.FolderPath;
        string timestamp = DateTime.Now.ToString(BackupDateFormat, CultureInfo.InvariantCulture);
        string backupBaseName = $"{Path.GetFileNameWithoutExtension(filePath)}{BackupFileSuffix}{timestamp}";
        string extension = Path.GetExtension(filePath);

        try
        {
            _ = Directory.CreateDirectory(directory);

            for (int copyNumber = 1; ; copyNumber++)
            {
                string collisionSuffix = copyNumber == 1 ? string.Empty : $" ({copyNumber})";
                string backupPath = Path.Combine(directory, $"{backupBaseName}{collisionSuffix}{extension}");
                if (await TryCreateBackupAsync(filePath, backupPath, cancellationToken).ConfigureAwait(false))
                {
                    PruneBackups(filePath, directory);
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new IOException("The backup could not be created, so the save was not changed.", ex);
        }
    }

    private static void PruneBackups(string sourcePath, string backupDirectory)
    {
        int retentionCount = BackupSettings.RetentionCount;
        if (retentionCount == 0 || BackupSettings.IsGameSaveFolder)
        {
            return;
        }

        string sourceStem = Path.GetFileNameWithoutExtension(sourcePath);
        string extension = Path.GetExtension(sourcePath);
        string prefix = $"{sourceStem}{BackupFileSuffix}";

        try
        {
            FileInfo[] backups = [.. new DirectoryInfo(backupDirectory)
                .EnumerateFiles($"{prefix}*{extension}", SearchOption.TopDirectoryOnly)
                .Where(file => IsRecognizedBackup(file, prefix, extension))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)];

            foreach (FileInfo backup in backups.Skip(retentionCount))
            {
                try
                {
                    backup.Delete();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Logger.LogWarning(ex, "Could not prune an old backup.");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.LogWarning(ex, "Could not enumerate backups for retention cleanup.");
        }
    }

    private static bool IsRecognizedBackup(FileInfo file, string prefix, string extension)
    {
        string fileName = file.Name;
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string suffix = fileName[prefix.Length..^extension.Length];
        if (suffix.Length < BackupDateFormat.Length ||
            !DateTime.TryParseExact(
                suffix[..BackupDateFormat.Length],
                BackupDateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            return false;
        }

        ReadOnlySpan<char> collision = suffix.AsSpan(BackupDateFormat.Length);
        return collision.IsEmpty ||
               collision.StartsWith(" (", StringComparison.Ordinal) &&
               collision.EndsWith(')') &&
               int.TryParse(collision[2..^1], NumberStyles.None, CultureInfo.InvariantCulture, out int copyNumber) &&
               copyNumber >= 2;
    }

    private static async Task<bool> TryCreateBackupAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        bool destinationCreated = false;

        try
        {
            await using FileStream destination = new(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                CopyBufferSize,
                FileOptions.Asynchronous);
            destinationCreated = true;

            await using FileStream source = new(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await source.CopyToAsync(destination, CopyBufferSize, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (
            !destinationCreated &&
            ex is IOException or UnauthorizedAccessException &&
            (File.Exists(destinationPath) || Directory.Exists(destinationPath)))
        {
            return false;
        }
        catch
        {
            if (destinationCreated)
            {
                TryDeleteFile(destinationPath);
            }

            throw;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.LogWarning(ex, "Could not remove a temporary staging file.");
        }
    }
}
