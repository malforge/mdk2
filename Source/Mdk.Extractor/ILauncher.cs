// Mdk.Extractor
// 
// Copyright 2023 Morten A. Lyrstad

namespace Mdk.Extractor
{
    public interface ILauncher
    {
        void Launch(params string[] arguments);
        string Path { get; set; }
    }
}