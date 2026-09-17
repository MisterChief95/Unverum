namespace Unverum;

public class DownloadProgress(float percentage, long downloadedBytes, long totalBytes, string fileName)
{
    public float Percentage { get; set; } = percentage;
    public long DownloadedBytes { get; set; } = downloadedBytes;
    public long TotalBytes { get; set; } = totalBytes;
    public string FileName { get; set; } = fileName;
}
