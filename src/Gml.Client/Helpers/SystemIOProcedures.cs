﻿using System.Collections.Concurrent;
using System.Security.Cryptography;
using Gml.Web.Api.Domains.System;
using Gml.Web.Api.Dto.Files;
using Gml.Web.Api.Dto.Profile;

namespace Gml.Client.Helpers;

public class SystemIoProcedures
{
    private readonly string _installationDirectory;
    private readonly OsType _osType;

    public SystemIoProcedures(string installationDirectory, OsType osType)
    {
        _installationDirectory = installationDirectory;
        _osType = osType;
    }

    public List<ProfileFileReadDto> FindErroneousFiles(
        ProfileReadInfoDto profileInfo,
        string installationDirectory)
    {
        // Кэширование списков файлов и белого списка
        var files = profileInfo.Files.ToList();
        var whiteListFiles = profileInfo.WhiteListFiles.ToHashSet();
        var errorFiles = new ConcurrentBag<ProfileFileReadDto>();

        Parallel.ForEach(files, downloadingFile =>
        {
            if (_osType == OsType.Windows)
            {
                downloadingFile.Directory = downloadingFile.Directory.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            }

            string localPath = Path.Combine(installationDirectory, downloadingFile.Directory);

            if (FileExists(localPath))
            {
                var hashIsCorrect = SystemHelper.CalculateFileHash(localPath, new SHA256Managed()) == downloadingFile.Hash;
                if (hashIsCorrect)
                {
                    return;
                }
            }

            if (!FileExists(localPath) || !whiteListFiles.Any(c => c.Hash.Equals(downloadingFile.Hash)))
            {
                errorFiles.Add(downloadingFile);
            }
        });

        return errorFiles.ToList();
    }

    private static bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public Task RemoveFiles(ProfileReadInfoDto profileInfo)
    {
        try
        {
            var profilePath = _installationDirectory + @"\clients\" + profileInfo.ProfileName;

            var directoryInfo = new DirectoryInfo(profilePath);

            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            var files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);

            var hashSet = profileInfo.Files
                .Select(f => new FileInfo(GetRealFilePath(_installationDirectory, f)).FullName)
                .Concat(profileInfo.WhiteListFiles
                    .Select(wf => new FileInfo(GetRealFilePath(_installationDirectory, wf)).FullName));

            var exclusionSet = new HashSet<string>(hashSet);

            var missingFiles = files
                .Where(f => !exclusionSet.Contains(f.FullName))
                .ToList();

            foreach (var file in missingFiles)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }

        return Task.CompletedTask;
    }

    private string GetRealFilePath(string installationDirectory, ProfileFileReadDto file)
    {
        if (_osType == OsType.Windows)
        {
            file.Directory = file.Directory.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        }

        return Path.Combine(installationDirectory, file.Directory);
    }
}