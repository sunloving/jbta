﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;

namespace Photosphere.SearchEngine.Utils
{
    internal static class FileSystem
    {
        public static bool IsExistingPath(string path) =>
            Directory.Exists(path) || File.Exists(path);

        public static bool IsDirectory(string path) =>
            File.GetAttributes(path).HasFlag(FileAttributes.Directory);

        public static bool IsRemovedButLocked(string path) =>
            IsExistingPath(path) && !IsAccessableDirectory(path);

        public static string GetDirectoryPathByFilePath(string filePath) =>
            new FileInfo(filePath).DirectoryName;

        public static string GetParentDirectoryPathByDirectoryPath(string directoryPath) =>
            new DirectoryInfo(directoryPath).Parent?.FullName;

        public static IEnumerable<string> GetFilesPathesByDirectory(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            foreach (var file in directoryInfo.EnumerateFiles())
            {
                yield return string.Intern(file.FullName);
            }

            foreach (var subdirectory in directoryInfo.EnumerateDirectories())
            {
                foreach (var filePath in GetFilesPathesByDirectory(subdirectory.FullName))
                {
                    yield return filePath;
                }
            }
        }

        public static string GetFullPath(string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                return string.IsNullOrWhiteSpace(fullPath) ? null : fullPath;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsAccessableDirectory(string directoryPath)
        {
            try
            {
                var readAllow = false;
                var readDeny = false;
                var accessControlList = Directory.GetAccessControl(directoryPath);
                var accessRules =
                    accessControlList?.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                if (accessRules == null)
                {
                    return false;
                }

                foreach (FileSystemAccessRule rule in accessRules)
                {
                    if ((FileSystemRights.Read & rule.FileSystemRights) != FileSystemRights.Read)
                    {
                        continue;
                    }
                    switch (rule.AccessControlType)
                    {
                        case AccessControlType.Allow:
                            readAllow = true;
                            break;
                        case AccessControlType.Deny:
                            readDeny = true;
                            break;
                    }
                }

                return readAllow && !readDeny;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}