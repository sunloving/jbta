﻿using System.Collections.Generic;

namespace Jbta.SearchEngine.Searching
{
    internal interface ISearcher
    {
        IEnumerable<WordEntry> Search(string query, bool wholeWord);
    }
}