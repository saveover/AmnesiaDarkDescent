// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using Microsoft.UI.Xaml.Controls;
using SaveOver.AmnesiaDarkDescent.Models;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SaveOver.AmnesiaDarkDescent.Pages;

public sealed partial class CharacterPage : Page
{
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "This property is an instance source for compiled x:Bind expressions.")]
    public PlayerData Player => App.CurrentSaveData.Player
        ?? throw new InvalidOperationException("A save must be loaded before opening the Character page.");

    public CharacterPage() => InitializeComponent();

    private void ConditionMinimumButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Player.Health = 0;
        Player.Sanity = 0;
    }

    private void ConditionMaximumButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Player.Health = 100;
        Player.Sanity = 100;
    }

    private void ResourcesMinimumButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Player.LampOil = 0;
        Player.Tinderboxes = 0;
    }

    private void ResourcesMaximumButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Player.LampOil = 100;
        Player.Tinderboxes = int.MaxValue;
    }
}
