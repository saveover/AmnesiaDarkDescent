// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveOver.AmnesiaDarkDescent.Models;

/// <summary>
/// The four supported values stored directly beneath Amnesia's mPlayer save object. Range
/// correction occurs after the model is attached to a save session so a corrected source save is
/// visibly marked as changed rather than being normalized silently during parsing.
/// </summary>
public sealed partial class PlayerData : ObservableObject
{
    [ObservableProperty]
    public partial double Health { get; set; }

    [ObservableProperty]
    public partial double Sanity { get; set; }

    [ObservableProperty]
    public partial double LampOil { get; set; }

    [ObservableProperty]
    public partial int Tinderboxes { get; set; }
}
