// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace SaveOver.AmnesiaDarkDescent.Converters;

/// <summary>Rejects NumberBox's NaN empty state before it reaches a save-backed model.</summary>
public sealed partial class FiniteNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is double number && double.IsFinite(number) ? number : 0d;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        value is double number && double.IsFinite(number)
            ? number
            : DependencyProperty.UnsetValue;
}
