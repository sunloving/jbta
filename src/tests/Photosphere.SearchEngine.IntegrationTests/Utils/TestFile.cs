﻿using System;
using System.IO;

namespace Photosphere.SearchEngine.IntegrationTests.Utils
{
    internal class TestFile : IDisposable
    {
        private bool _isDeleted;

        public TestFile(string content, string folderPath = ".", string fileName = null)
        {
            fileName = fileName ?? GenerateFileName();
            Path = $"{folderPath}\\{fileName}";
            using (var file = new StreamWriter(Path, true))
            {
                file.WriteLine(content);
            }
            File.SetAttributes(Path, FileAttributes.Normal);
        }

        public static string GenerateFileName()
        {
            return $"test-{Guid.NewGuid()}.txt";
        }

        public string Name => new FileInfo(Path).Name;

        public string Path { get; private set; }

        public void ChangeContent(string newContent)
        {
            var text = File.ReadAllText(Path);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            text = text.Replace(text, newContent);

            var isChanged = false;
            while (!isChanged)
            {
                try
                {
                    File.WriteAllText(Path, text);
                    isChanged = true;
                }
                catch
                {
                    // ignored
                }
            }
        }

        public void Rename(string newName)
        {
            var newPath = new FileInfo(Path).DirectoryName;
            newPath += newPath.EndsWith("\\") ? newName : "\\" + newName;

            var isRenamed = false;
            while (!isRenamed)
            {
                try
                {
                    File.Move(Path, newPath);
                    isRenamed = true;
                }
                catch
                {
                    // ignored
                }
            }

            Path = newPath;
        }

        public void Move(string folderPath)
        {
            folderPath = folderPath.Replace(".\\", string.Empty);
            var directoryPath = new FileInfo(Path).Directory.Name;
            var newFilePath = Path.Replace(directoryPath, folderPath);

            var isMoved = false;
            while (!isMoved)
            {
                try
                {
                    File.Move(Path, newFilePath);
                    isMoved = true;
                }
                catch
                {
                    // ignored
                }
            }
            Path = newFilePath;
        }

        public void Dispose()
        {
            Delete();
        }

        public void Delete()
        {
            if (_isDeleted)
            {
                return;
            }
            if (!File.Exists(Path))
            {
                return;
            }
            while (!_isDeleted)
            {
                try
                {
                    File.Delete(Path);
                    _isDeleted = true;
                }
                catch
                {
                    // ignored
                }
            }
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}