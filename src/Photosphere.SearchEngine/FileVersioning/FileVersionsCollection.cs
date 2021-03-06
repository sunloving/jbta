﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Photosphere.SearchEngine.Utils.Extensions;

namespace Photosphere.SearchEngine.FileVersioning
{
    internal class FileVersionsCollection
    {
        private readonly ISet<FileVersion> _items;
        private readonly ReaderWriterLockSlim _lock;

        public FileVersionsCollection()
        {
            _items = new HashSet<FileVersion>();
            _lock = new ReaderWriterLockSlim();
        }

        public void Add(FileVersion fileVersion)
        {
            using (_lock.Exclusive())
            {
                _items.Add(fileVersion);
            }
        }

        public IReadOnlyCollection<FileVersion> GetList()
        {
            using (_lock.Shared())
            {
                return _items.ToList();
            }
        }

        public bool Any()
        {
            using (_lock.Shared())
            {
                return _items.Any();
            }
        }

        public bool AnyAlive()
        {
            using (_lock.Shared())
            {
                return _items.Any(i => !i.IsDead);
            }
        }

        public void UpdateFilePath(string newPath)
        {
            using (_lock.SharedIntentExclusive())
            {
                foreach (var fileVersion in _items)
                {
                    using (_lock.Exclusive())
                    {
                        fileVersion.Path = newPath;
                    }
                }
            }
        }

        public IEnumerable<FileVersion> RemoveDeadVersions()
        {
            using (_lock.SharedIntentExclusive())
            {
                var deadVersions = _items.Where(i => i.IsDead).ToList();
                foreach (var deadVersion in deadVersions)
                {
                    using (_lock.Exclusive())
                    {
                        _items.Remove(deadVersion);
                    }
                }
                return deadVersions;
            }
        }

        public void KillVersions()
        {
            using (_lock.Exclusive())
            {
                foreach (var version in _items)
                {
                    version.IsDead = true;
                }
            }
        }

        public bool All(Func<FileVersion, bool> predicate)
        {
            using (_lock.Shared())
            {
                return _items.All(predicate);
            }
        }
    }
}