﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jbta.SearchEngine.DemoApp.Model;
using Jbta.SearchEngine.DemoApp.Utils;
using Jbta.SearchEngine.Events;
using Jbta.SearchEngine.Events.Args;

namespace Jbta.SearchEngine.DemoApp.ViewModels.SearchPanel
{
    internal class SearchPanelViewModel : ViewModelBase
    {
        private string _searchString = string.Empty;
        private bool _isWholeWord;

        public SearchPanelViewModel()
        {
            ListBoxItems = new ObservableCollection<ListBoxItemViewModel>();

            SubscribeOnIndexStateChange();
        }

        public string SearchText
        {
            get => _searchString;
            set
            {
                var val = value?.Trim();
                SetField(ref _searchString, val, nameof(SearchText));
                Search(val, IsWholeWord);
            }
        }

        public bool IsWholeWord
        {
            get => _isWholeWord;
            set
            {
                SetField(ref _isWholeWord, value, nameof(IsWholeWord));
                Search(SearchText, value);
            }
        }

        public ObservableCollection<ListBoxItemViewModel> ListBoxItems { get; }

        private void SubscribeOnIndexStateChange()
        {
            SearchSystem.EngineInstance.FileIndexed += OnIndexStateChange;
            SearchSystem.EngineInstance.FileRemoved += OnIndexStateChange;
            SearchSystem.EngineInstance.FilePathChanged += OnIndexStateChange;

            void OnIndexStateChange(SearchEngineEventArgs a)
            {
                DispatchService.Invoke(() => Search(_searchString, _isWholeWord));
            }
        }

        private void Search(string value, bool isWholeWord)
        {
            ListBoxItems.Clear();
            if (value.Length < 3)
            {
                return;
            }

            var serchResult = SearchSystem.EngineInstance.Search(value, isWholeWord);
            if (serchResult == null)
            {
                return;
            }

            AddResultsToList(serchResult);
        }

        private void AddResultsToList(IEnumerable<WordEntry> result)
        {
            var orderedResult = result
                .OrderBy(r => r.FileVersion.Path)
                .ThenBy(r => r.LineNumber)
                .ThenBy(r => r.Position)
                .Take(500)
                .ToList();

            foreach (var wordEntry in orderedResult)
            {
                var item = new ListBoxItemViewModel(wordEntry.FileVersion.Path, wordEntry.LineNumber, wordEntry.Position);
                ListBoxItems.Add(item);
            }
        }
    }
}
