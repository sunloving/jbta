﻿using Jbta.Indexing;

namespace Jbta.DemoApp.Model
{
    public static class Index
    {
        public static readonly IIndexer Instance = new Indexer();
    }
}