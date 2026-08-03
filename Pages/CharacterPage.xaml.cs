// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using Microsoft.UI.Xaml.Controls;
using SaveOver.AmnesiaDarkDescent.Models;
using System;

namespace SaveOver.AmnesiaDarkDescent.Pages;

public sealed partial class CharacterPage : Page
{
    public PlayerData Player => App.CurrentSaveData.Player
        ?? throw new InvalidOperationException("A save must be loaded before opening the Character page.");

    public CharacterPage() => InitializeComponent();
}
