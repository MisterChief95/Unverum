using Onova.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

namespace Unverum;

public class ZipExtractor : IPackageExtractor
{
    public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var archive = await SevenZipArchive.OpenAsyncArchive(sourceFilePath, cancellationToken: cancellationToken);
            await using var reader = await archive.ExtractAllEntriesAsync();

            while (await reader.MoveToNextEntryAsync(cancellationToken))
            {
                if (!reader.Entry.IsDirectory)
                    await reader.WriteEntryToDirectoryAsync(destDirPath, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    }, cancellationToken);
            }
        }
        catch
        {
            await using Stream stream = File.OpenRead(sourceFilePath);
            await using var reader = await ReaderFactory.OpenAsyncReader(stream, cancellationToken: cancellationToken);

            while (await reader.MoveToNextEntryAsync(cancellationToken))
            {
                if (!reader.Entry.IsDirectory)
                {
                    await reader.WriteEntryToDirectoryAsync(destDirPath, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    }, cancellationToken);
                }
            }
        }

        File.Delete(sourceFilePath);

        // Move the folders to the right place
        string? parentPath = Directory.GetParent(destDirPath)?.FullName;
        if (parentPath != null)
        {
            Directory.Move(Directory.GetDirectories(destDirPath)[0], $@"{parentPath}{Global.s}Unverum");
            Directory.Delete(destDirPath);
            Directory.Move($@"{parentPath}{Global.s}Unverum", destDirPath);
        }
    }
}
