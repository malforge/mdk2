using System;

namespace Mdk.Hub.Utility;

public interface INotifyDisposing
{
    event EventHandler? Disposing;
}

