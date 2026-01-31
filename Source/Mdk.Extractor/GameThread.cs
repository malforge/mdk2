// Mdk.Extractor
// 
// Copyright 2023-2026 The MDK² Authors

using System;
using System.Runtime.CompilerServices;
using Sandbox.ModAPI;

namespace Mdk.Extractor
{
    public static class GameThread
    {
        public static GameThreadSwitcherAwaitable SwitchToGameThread() => new GameThreadSwitcherAwaitable();

        public class GameThreadSwitcherAwaitable : INotifyCompletion
        {
            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                MyAPIGateway.Utilities.InvokeOnGameThread(continuation);
            }

            public GameThreadSwitcherAwaitable GetAwaiter() => this;

            public void GetResult()
            {
                // No result to get.
            }
        }
    }
}