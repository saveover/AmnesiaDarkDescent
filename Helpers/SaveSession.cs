// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using CommunityToolkit.Mvvm.ComponentModel;
using SaveOver.AmnesiaDarkDescent.Models;
using System;
using System.ComponentModel;

namespace SaveOver.AmnesiaDarkDescent.Helpers;

/// <summary>Owns the loaded file baseline, editable player model, and dirty-state tracking.</summary>
internal sealed partial class SaveSession : ObservableObject
{
    private bool suppressChangeTracking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoaded))]
    public partial string? SourceFilePath { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoaded))]
    public partial PlayerData? Player { get; private set; }

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; private set; }

    internal string? OriginalContent { get; private set; }

    internal bool IsLoaded => SourceFilePath is not null && Player is not null && OriginalContent is not null;

    internal event EventHandler? SaveDataChanged;

    internal event EventHandler? DirtyStateChanged;

    internal void Load(string filePath, string content, PlayerData player)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentNullException.ThrowIfNull(player);

        StopTrackingPlayer();
        suppressChangeTracking = true;
        try
        {
            SourceFilePath = filePath;
            OriginalContent = content;
            Player = player;
            HasUnsavedChanges = false;
        }
        finally
        {
            suppressChangeTracking = false;
        }

        Player.PropertyChanged += OnPlayerPropertyChanged;
        SaveDataChanged?.Invoke(this, EventArgs.Empty);
        DirtyStateChanged?.Invoke(this, EventArgs.Empty);
    }

    internal void CommitSavedContent(string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        OriginalContent = content;
        SetDirty(false);
    }

    private void StopTrackingPlayer() => Player?.PropertyChanged -= OnPlayerPropertyChanged;

    private void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (!suppressChangeTracking)
        {
            SetDirty(true);
        }
    }

    private void SetDirty(bool value)
    {
        if (HasUnsavedChanges == value)
        {
            return;
        }

        HasUnsavedChanges = value;
        DirtyStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
