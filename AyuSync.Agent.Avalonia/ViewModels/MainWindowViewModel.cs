#region

using System;
using ReactiveUI.Fody.Helpers;

#endregion

namespace AyuSync.Client.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    [Reactive] public string URL { get; set; }
    [Reactive] public string Token { get; set; }
    [Reactive] public bool Enabled { get; set; }

    [Reactive] public string SyncStatus { get; set; }
    [Reactive] public int RegisterStatusCode { get; set; }
    [Reactive] public string DeviceIdentifier { get; set; }
    [Reactive] public DateTime LastSentEvent { get; set; }
    [Reactive] public DateTime LastReceivedEvent { get; set; }
    [Reactive] public bool UseSecure { get; set; }
}
