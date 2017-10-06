﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jbta.SearchEngine.Events;
using Jbta.SearchEngine.FileParsing;
using Jbta.SearchEngine.FileVersioning;
using Jbta.SearchEngine.Index;
using Jbta.SearchEngine.Utils;
using Jbta.SearchEngine.Vendor.SimpleHelpers;

namespace Jbta.SearchEngine.FileIndexing
{
    internal class FileIndexer : IFileIndexer
    {
        private readonly IEventReactor _eventReactor;
        private readonly FileParserProvider _parserProvider;
        private readonly IIndex _index;
        private readonly FilesVersionsRegistry _filesVersionsRegistry;
        private readonly SearchEngineSettings _settings;

        public FileIndexer(
            IEventReactor eventReactor,
            FileParserProvider parserProvider,
            IIndex index,
            FilesVersionsRegistry filesVersionsRegistry,
            SearchEngineSettings settings)
        {
            _eventReactor = eventReactor;
            _parserProvider = parserProvider;
            _index = index;
            _filesVersionsRegistry = filesVersionsRegistry;
            _settings = settings;
        }

        public void Index(string path)
        {
            if (FileSystem.IsDirectory(path))
            {
                var filesPathes = FileSystem.GetFilesPathesByDirectory(path).ToArray();
                foreach (var filePath in filesPathes)
                {
                    LoadFile(filePath);
                }
            }
            else
            {
                LoadFile(path);
            }
        }

        private void LoadFile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            if (extension == null || !_settings.SupportedFilesExtensions.Contains(extension.Substring(1).ToLowerInvariant()))
            {
                return;
            }

            var fileVersion = _filesVersionsRegistry.RegisterFileVersion(filePath);

            Task.Run(() => IndexFile(filePath, fileVersion));
        }

        private void IndexFile(string filePath, IFileVersion version)
        {
            try
            {
                _eventReactor.React(EngineEvent.FileIndexingStarted, filePath);

                var encoding = FileEncoding.DetectFileEncoding(filePath);
                var words = _parserProvider.Provide(filePath).Parse(version, encoding);
                _index.Add(version, words);

                _eventReactor.React(EngineEvent.FileIndexingEnded, filePath);
            }
            catch(Exception exception)
            {
                _eventReactor.React(EngineEvent.FileIndexingEnded, filePath, exception);
            }
        }
    }
}