﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Photosphere.SearchEngine.DemoApp.Model;
using Photosphere.SearchEngine.DemoApp.Utils;
using Photosphere.SearchEngine.DemoApp.ViewModels.IndexManagement.TreeView;
using Photosphere.SearchEngine.Events.Args;

namespace Photosphere.SearchEngine.DemoApp.ViewModels.IndexManagement
{
    internal class IndexManagementPanelViewModel : ViewModelBase
    {
        private bool _isRemoveButtonEnabled;
        private int _processingFilesCount;
        private bool _isIndexing;
        private Visibility _indexingStatusLabelVisibility = Visibility.Hidden;
        private RelayCommand _addFolderCommand;
        private RelayCommand _addFilesCommand;
        private RelayCommand _removeButtonClickCommand;

        public IndexManagementPanelViewModel()
        {
            TreeViewItems = new ObservableCollection<ITreeViewItemViewModel>();
            SubscribeOnIndexStateChange();
        }

        public bool IsIndexing
        {
            get => _isIndexing;
            set => SetField(ref _isIndexing, value, nameof(IsIndexing));
        }

        public Visibility IndexingStatusLabelVisibility
        {
            get => _indexingStatusLabelVisibility;
            set => SetField(ref _indexingStatusLabelVisibility, value, nameof(IndexingStatusLabelVisibility));
        }

        public bool IsRemoveButtonEnabled
        {
            get => _isRemoveButtonEnabled;
            set => SetField(ref _isRemoveButtonEnabled, value, nameof(IsRemoveButtonEnabled));
        }

        public ObservableCollection<ITreeViewItemViewModel> TreeViewItems { get; }

        public ICommand AddFolderButtonClick =>
            _addFolderCommand ?? (_addFolderCommand = new RelayCommand(OnAddFolderButtonClick));

        public ICommand AddFilesButtonClick =>
            _addFilesCommand ?? (_addFilesCommand = new RelayCommand(OnAddFilesButtonClick));

        public ICommand RemoveButtonClick =>
            _removeButtonClickCommand ?? (_removeButtonClickCommand = new RelayCommand(OnRemoveButtonClick));

        private void SubscribeOnIndexStateChange()
        {
            SearchSystem.EngineInstance.PathWatchingStarted += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | Path watching started: {a.Path}");
                DispatchService.Invoke(RefreshTree);
            };
            SearchSystem.EngineInstance.PathWatchingEnded += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | Path watching ended: {a.Path}");
                DispatchService.Invoke(RefreshTree);
            };
            SearchSystem.EngineInstance.FileIndexingStarted += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | File indexing started: {a.Path}");
                OnStartFileProcessing(a);
            };
            SearchSystem.EngineInstance.FileIndexingEnded += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | File indexing ended: {a.Path}");
                OnStopFileProcessing(a);
            };
            SearchSystem.EngineInstance.FileRemovingStarted += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | File removing started: {a.Path}");
                OnStartFileProcessing(a);
            };
            SearchSystem.EngineInstance.FileRemovingEnded += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | File removing ended: {a.Path}");
                OnStopFileProcessing(a);
            };
            SearchSystem.EngineInstance.FilePathChanged += a =>
            {
                var args = (FilePathChangedEventArgs) a;
                Trace.WriteLine($@"{DateTime.Now} | File path changed: old: {args.OldFilePath} new: {args.Path}");
                DispatchService.Invoke(RefreshTree);
            };
            SearchSystem.EngineInstance.FileUpdateInitiated += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | File update initiated: {a.Path}");
            };
            SearchSystem.EngineInstance.FileUpdateFailed += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | File update failed: path: {a.Path} error: {a.Error.Message}");
            };
            SearchSystem.EngineInstance.IndexCleanUpFailed += a =>
            {
                Trace.WriteLine($@"{DateTime.Now} | Index clean up failed: {a.Error.Message}");
            };

            void OnStartFileProcessing(SearchEngineEventArgs a) => DispatchService.Invoke(() =>
            {
                Interlocked.Increment(ref _processingFilesCount);
                if (_processingFilesCount >= 1)
                {
                    IndexingStatusLabelVisibility = Visibility.Visible;
                }
                RefreshTree();
            });

            void OnStopFileProcessing(SearchEngineEventArgs a) => DispatchService.Invoke(() =>
            {
                Interlocked.Decrement(ref _processingFilesCount);
                if (_processingFilesCount < 1)
                {
                    IndexingStatusLabelVisibility = Visibility.Hidden;
                }
                RefreshTree();
            });
        }

        private static async Task OnAddFolderButtonClick(object sender)
        {
            string selectedPath = null;
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                var dialogResult = dialog.ShowDialog();
                if (dialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    selectedPath = dialog.SelectedPath;
                }
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            await Task.Run(() => SearchSystem.EngineInstance.Add(selectedPath));
        }

        private static async Task OnAddFilesButtonClick(object sender)
        {
            var selectedPathes = GetFilesFromDialog();

            if (selectedPathes == null || !selectedPathes.Any())
            {
                return;
            }

            await AyncIndexing(selectedPathes);
        }

        private static string[] GetFilesFromDialog()
        {
            string[] selectedPathes = null;
            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = Path.GetPathRoot(Environment.SystemDirectory);
                dialog.Multiselect = true;
                dialog.DefaultExt = ".txt";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.Filter = "Text files (*.txt)|*.txt|Log Files (*.log)|*.log|C# Files (*.cs)|*.cs|All files (*.*)|*.*";

                var dialogResult = dialog.ShowDialog();
                if (dialogResult == DialogResult.OK && dialog.FileNames != null && dialog.FileNames.Any())
                {
                    selectedPathes = dialog.FileNames;
                }
            }
            return selectedPathes;
        }

        private static async Task AyncIndexing(IEnumerable<string> pathes)
        {
            var tasks = pathes.Select(async path =>
            {
                await Task.Run(() => SearchSystem.EngineInstance.Add(path));
            });
            await Task.WhenAll(tasks);
        }

        private async Task OnRemoveButtonClick(object sender)
        {
            async Task RemoveAction(ICollection<ITreeViewItemViewModel> items)
            {
                foreach (var item in items.ToList())
                {
                    if (item.IsSelected)
                    {
                        items.Remove(item);
                        RemoveFromSearchSystem(item);
                        return;
                    }
                    if (item.Children != null)
                    {
                        await RemoveAction(item.Children);
                    }
                }
            }

            await RemoveAction(TreeViewItems);
            CheckRemoveButtonAvailability();
        }

        private static void RemoveFromSearchSystem(ITreeViewItemViewModel item)
        {
            Task.Run(() => SearchSystem.EngineInstance.Remove(item.Content));
        }

        private void RefreshTree()
        {
            var indexedPathes = SearchSystem.EngineInstance.PathesUnderIndex;
            TreeViewItems.Clear();
            foreach (var path in indexedPathes.OrderBy(p => p))
            {
                TreeViewItemViewModelBase viewModel;
                if (FileSystem.IsDirectory(path))
                {
                    viewModel = new FolderTreeViewItemViewModel(path);
                }
                else
                {
                    viewModel = new FileTreeViewItemViewModel(path);
                }
                viewModel.PropertyChanged += (o, a) =>
                {
                    if (a.PropertyName == nameof(TreeViewItemViewModelBase.IsSelected))
                    {
                        CheckRemoveButtonAvailability();
                    }
                };
                TreeViewItems.Add(viewModel);
            }
        }

        private void CheckRemoveButtonAvailability()
        {
            IsRemoveButtonEnabled = TreeViewItems.Any(vm => vm.IsSelected);
        }
    }
}