using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;

namespace Unverum.UI;

/// <summary>
/// Interaction logic for ConfigurePaksWindow.xaml
/// </summary>
public partial class ConfigurePaksWindow : Window
{
    public readonly Mod _mod;
    public ConfigurePaksWindow(Mod mod)
    {
        InitializeComponent();
        _mod = mod;
        PakList.ItemsSource = new ObservableCollection<KeyValuePair<string, bool>>(_mod.paks ?? []);
        Title = $"Configure Paks for {_mod.name}";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (ToggleButton)sender;
        var item = (KeyValuePair<string, bool>)button.DataContext;
        if (_mod.paks != null)
            _mod.paks[item.Key] = button.IsChecked == true;
    }
}
