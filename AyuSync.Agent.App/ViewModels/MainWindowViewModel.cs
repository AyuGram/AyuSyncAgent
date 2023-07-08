using ReactiveUI;

namespace AyuSync.Agent.App.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public bool EnableDebugMode 
    { 
        get => _enableDebugMode;
        set { _enableDebugMode = value; this.RaisePropertyChanged(); }
    }
    private bool _enableDebugMode { get; set; }
    
    
}