﻿namespace Photosphere.SearchEngine.FileVersioning.Services
{
    internal interface IFilePathActualizer
    {
        void Actualize(string oldPath, string newPath);
    }
}