// Mdk.Extractor
// 
// Copyright 2023-2026 The MDK² Authors

namespace Mdk.Extractor
{
    public interface ILauncher
    {
        void Launch(params string[] arguments);
        string Path { get; set; }
    }
}