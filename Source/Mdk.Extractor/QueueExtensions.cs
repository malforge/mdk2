// Mdk.Extractor
// 
// Copyright 2023 Morten A. Lyrstad

using System.Collections.Generic;

namespace Mdk.Extractor
{
    static class QueueExtensions
    {
        public static bool TryDequeue<T>(this Queue<T> queue, out T value)
        {
            if (queue.Count == 0)
            {
                value = default;
                return false;
            }
            value = queue.Dequeue();
            return true;
        }
    }
}