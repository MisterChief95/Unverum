using System;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace Unverum;

public enum LoggerType
{
    Info,
    Warning,
    Error
}

public class Logger(RichTextBox textBox)
{
    private readonly RichTextBox _outputWindow = textBox;

    public void WriteLine(string text, LoggerType type)
    {
        var (color, header) = type switch
        {
            LoggerType.Info => ("#52FF00", "INFO"),
            LoggerType.Warning => ("#FFFF00", "WARNING"),
            LoggerType.Error => ("#FFB0B0", "ERROR"),
            _ => ("#F2F2F2", "")
        };
        // Call on UI thread
        Application.Current.Dispatcher.Invoke(() =>
            _outputWindow.AppendText($"[{DateTime.Now}] [{header}] {text}\n", color));
    }
}

// RichTextBox extension to append color
public static class RichTextBoxExtensions
{
    public static void AppendText(this RichTextBox box, string text, string color)
    {
        BrushConverter bc = new();
        TextRange tr = new(box.Document.ContentEnd, box.Document.ContentEnd)
        {
            Text = text
        };

        try
        {
            tr.ApplyPropertyValue(
                TextElement.ForegroundProperty,
                bc.ConvertFromString(color));
        }
        catch (FormatException) { }
    }
}
