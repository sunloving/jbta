﻿using System;
using System.Collections.Generic;

namespace Jbta.SearchEngine.Index
{
    internal interface ITrie<T>
    {
        void Add(string key, T value);
        void Remove(string key, Func<T, bool> valueSelector);
        IEnumerable<T> Get(string query, bool wholeWord = false);
    }
}