using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace Unverum.UI;

/// <summary>
/// Interaction logic for EditWindow.xaml
/// </summary>
public partial class EditWindow : Window
{
    public string? _name;
    public bool _folder;
    public string? directory;
    public string? loadout;
    public EditWindow(string name, bool folder)
    {
        InitializeComponent();
        _folder = folder;
        if (!string.IsNullOrEmpty(name))
        {
            _name = name;
            NameBox.Text = name;
            Title = $"Edit {name}";
        }
        else
            Title = _folder ? "Create New Mod" : "Create New Loadout";
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (_folder)
            if (_name != null)
                EditFolderName();
            else
                CreateName();
        else
            CreateLoadoutName();

    }
    private void CreateName()
    {
        var newDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.config.CurrentGame}{Global.s}{NameBox.Text}";
        if (!Directory.Exists(newDirectory))
        {
            directory = newDirectory;
            Close();
        }
        else
            Global.logger.WriteLine($"{newDirectory} already exists", LoggerType.Error);
    }
    private void CreateLoadoutName()
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            Global.logger.WriteLine($"Invalid loadout name", LoggerType.Error);
            return;
        }
        if (!(Global.CurrentGameConfig.Loadouts ??= new()).ContainsKey(NameBox.Text))
        {
            loadout = NameBox.Text;
            Close();
        }
        else
            Global.logger.WriteLine($"{NameBox.Text} already exists", LoggerType.Error);
    }
    private void EditFolderName()
    {
        if (_name is { } oldName && !NameBox.Text.Equals(oldName, StringComparison.InvariantCultureIgnoreCase))
        {
            var oldDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.CurrentGame}{Global.s}{oldName}";
            var newDirectory = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}{Global.CurrentGame}{Global.s}{NameBox.Text}";
            if (!Directory.Exists(newDirectory))
            {
                try
                {
                    Directory.Move(oldDirectory, newDirectory);
                    // Rename in every single loadout
                    var gameConfig = Global.CurrentGameConfig;
                    var loadouts = gameConfig.Loadouts ??= new();
                    foreach (var key in loadouts.Keys)
                    {
                        var index = loadouts[key].ToList().FindIndex(x => x.name == oldName);
                        if (index >= 0)
                            loadouts[key][index].name = NameBox.Text;
                    }
                    if (gameConfig.CurrentLoadout is { } currentLoadout && loadouts.TryGetValue(currentLoadout, out var mods))
                        Global.ModList = mods;
                    Close();
                }
                catch (Exception ex)
                {
                    Global.logger.WriteLine($"Couldn't rename {oldDirectory} to {newDirectory} ({ex.Message})", LoggerType.Error);
                }
            }
            else
                Global.logger.WriteLine($"{newDirectory} already exists", LoggerType.Error);
        }
    }

    private void NameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            if (_folder)
                if (_name != null)
                    EditFolderName();
                else
                    CreateName();
            else
                CreateLoadoutName();
        }
    }
}
