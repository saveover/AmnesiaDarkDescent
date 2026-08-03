// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 SaveOver

using SaveOver.AmnesiaDarkDescent.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SaveOver.AmnesiaDarkDescent.Helpers;

/// <summary>
/// Structurally locates Amnesia's one player object and updates only the four explicitly supported
/// attributes. PreserveWhitespace plus DisableFormatting retains unknown nodes, ordering, and the
/// save's existing line endings.
/// </summary>
internal static class AmnesiaSaveCodec
{
    private const string PlayerClassType = "cLuxPlayer_SaveData";
    private const string PlayerClassName = "mPlayer";

    private static readonly IReadOnlyDictionary<string, string> FieldTypes = new Dictionary<string, string>
    {
        ["mfHealth"] = "3",
        ["mfSanity"] = "3",
        ["mfLampOil"] = "3",
        ["mlTinderboxes"] = "2",
    };

    internal static PlayerData Parse(string content)
    {
        XDocument document = ParseDocument(content);
        XElement player = FindPlayer(document);

        return new PlayerData
        {
            Health = ReadFiniteDouble(player, "mfHealth"),
            Sanity = ReadFiniteDouble(player, "mfSanity"),
            LampOil = ReadFiniteDouble(player, "mfLampOil"),
            Tinderboxes = ReadInt32(player, "mlTinderboxes"),
        };
    }

    internal static string Write(string originalContent, PlayerData playerData)
    {
        ArgumentException.ThrowIfNullOrEmpty(originalContent);
        ArgumentNullException.ThrowIfNull(playerData);

        XDocument document = ParseDocument(originalContent);
        XElement player = FindPlayer(document);

        SetDoubleIfChanged(player, "mfHealth", playerData.Health);
        SetDoubleIfChanged(player, "mfSanity", playerData.Sanity);
        SetDoubleIfChanged(player, "mfLampOil", playerData.LampOil);
        SetInt32IfChanged(player, "mlTinderboxes", playerData.Tinderboxes);

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static XDocument ParseDocument(string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);

        try
        {
            return XDocument.Parse(content, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("The selected file is not a valid Amnesia XML save.", ex);
        }
    }

    private static XElement FindPlayer(XDocument document)
    {
        XElement[] players = [.. document
            .Descendants("class")
            .Where(element =>
                (string?)element.Attribute("type") == PlayerClassType &&
                (string?)element.Attribute("name") == PlayerClassName)];

        return players.Length switch
        {
            1 => players[0],
            0 => throw new InvalidDataException("The save does not contain the expected Amnesia player data."),
            _ => throw new InvalidDataException("The save contains more than one Amnesia player object."),
        };
    }

    private static XElement FindField(XElement player, string name)
    {
        XElement[] fields = [.. player.Elements("var").Where(element => (string?)element.Attribute("name") == name)];
        if (fields.Length != 1)
        {
            throw new InvalidDataException($"The player field '{name}' is missing or duplicated.");
        }

        XElement field = fields[0];
        return (string?)field.Attribute("type") != FieldTypes[name] || field.Attribute("val") is null
            ? throw new InvalidDataException($"The player field '{name}' has an unsupported shape.")
            : field;
    }

    private static double ReadFiniteDouble(XElement player, string name)
    {
        string? value = (string?)FindField(player, name).Attribute("val");
        return !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ||
            !double.IsFinite(result)
            ? throw new InvalidDataException($"The player field '{name}' is not a finite number.")
            : result;
    }

    private static int ReadInt32(XElement player, string name)
    {
        string? value = (string?)FindField(player, name).Attribute("val");
        return !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? throw new InvalidDataException($"The player field '{name}' is not a 32-bit integer.")
            : result;
    }

    private static void SetValue(XElement player, string name, string value) =>
        FindField(player, name).SetAttributeValue("val", value);

    private static void SetDoubleIfChanged(XElement player, string name, double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidDataException("Player values must be finite numbers before saving.");
        }

        if (!ReadFiniteDouble(player, name).Equals(value))
        {
            SetValue(player, name, Float(value));
        }
    }

    private static void SetInt32IfChanged(XElement player, string name, int value)
    {
        if (ReadInt32(player, name) != value)
        {
            SetValue(player, name, Integer(value));
        }
    }

    private static string Float(double value) => !double.IsFinite(value)
            ? throw new InvalidDataException("Player values must be finite numbers before saving.")
            : value.ToString("F6", CultureInfo.InvariantCulture);

    private static string Integer(int value) => value.ToString(CultureInfo.InvariantCulture);
}
