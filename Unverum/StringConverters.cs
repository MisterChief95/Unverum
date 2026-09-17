using System;
using System.IO;

namespace Unverum;

public static class StringConverters
{
    public static string FormatFileName(string filename) => Path.GetFileName(filename);

    // Load all suffixes in an array  
    static readonly string[] suffixes = [" Bytes", " KB", " MB", " GB", " TB", " PB"];
    
    public static string FormatSize(long bytes)
    {
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1000) >= 1)
        {
            number /= 1000;
            counter++;
        }
        return bytes != 0 ? $"{number:n1}{suffixes[counter]}" : $"{number:n0}{suffixes[counter]}";
    }

    public static string FormatNumber(int number) => number switch
    {
        > 1000000 => $"{Math.Round((double)number / 1000000, 1)}M",
        > 1000 => $"{Math.Round((double)number / 1000, 1)}K",
        _ => number.ToString()
    };
    
    public static string FormatTimeSpan(TimeSpan timeSpan) => timeSpan.TotalMinutes switch
    {
        < 1 => $"{Math.Floor(timeSpan.TotalSeconds)}s",
        < 60 => $"{Math.Floor(timeSpan.TotalMinutes)}m",
        < 1440 => $"{Math.Floor(timeSpan.TotalHours)}h",
        < 10080 => $"{Math.Floor(timeSpan.TotalDays)}d",
        < 43830 => $"{Math.Floor(timeSpan.TotalDays / 7)}w",
        < 525960 => $"{Math.Floor(timeSpan.TotalDays / 30.4)}mo",
        _ => $"{Math.Floor(timeSpan.TotalDays / 365.25)}y"

    };
    
    public static string FormatTimeAgo(TimeSpan timeSpan)
    {
        if (timeSpan.TotalMinutes < 60)
        {
            var minutes = Math.Floor(timeSpan.TotalMinutes);
            return minutes > 1 ? $"{minutes} minutes ago" : $"{minutes} minute ago";
        }

        if (timeSpan.TotalHours < 24)
        {
            var hours = Math.Floor(timeSpan.TotalHours);
            return hours > 1 ? $"{hours} hours ago" : $"{hours} hour ago";
        }

        if (timeSpan.TotalDays < 7)
        {
            var days = Math.Floor(timeSpan.TotalDays);
            return days > 1 ? $"{days} days ago" : $"{days} day ago";
        }

        if (timeSpan.TotalDays < 30.4)
        {
            var weeks = Math.Floor(timeSpan.TotalDays / 7);
            return weeks > 1 ? $"{weeks} weeks ago" : $"{weeks} week ago";
        }

        if (timeSpan.TotalDays < 365.25)
        {
            var months = Math.Floor(timeSpan.TotalDays / 30.4);
            return months > 1 ? $"{months} months ago" : $"{months} month ago";
        }

        var years = Math.Floor(timeSpan.TotalDays / 365.25);
        return years > 1 ? $"{years} years ago" : $"{years} year ago";
    }

    public static string FormatSingular(string? rootCat, string cat)
    {
        if (rootCat is null)
        {
            return cat.TrimEnd('s');
        }

        rootCat = rootCat.Replace("User Interface", "UI");

        if (cat == "Skin Packs")
        {
            return cat[..^1];
        }

        if (rootCat[^1] == 's')
        {
            return cat == rootCat
                ? rootCat.Replace("xes", "xs").Replace("xs/", "xes/")[..^1]
                : $"{cat} {rootCat[..^1]}";
        }

        return cat == rootCat ? rootCat : $"{cat} {rootCat}";
    }
}
