using System.ComponentModel;
using System.Runtime.CompilerServices;
using MudBlazor;

namespace HOSKYSWAP.UI.WASM.Services;

public class AppStateService : INotifyPropertyChanged
{
    public readonly DialogOptions DialogOptions = new() {FullWidth = true, DisableBackdropClick = true};

    private bool _isWalletConnected = false;

    public bool IsWalletConnected
    {
        get => _isWalletConnected;

        set
        {
            _isWalletConnected = value;
            NotifyPropertyChanged();
        }
    }
    
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")  
    {  
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }  
    
    public event PropertyChangedEventHandler? PropertyChanged;
}