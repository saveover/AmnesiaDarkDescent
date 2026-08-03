// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveOver.AmnesiaDarkDescent.Models;

/// <summary>
/// The four supported values stored directly beneath Amnesia's mPlayer save object. Values are not
/// clamped here: unusual source saves must remain representable, and no undocumented game limits
/// are invented by the editor.
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
