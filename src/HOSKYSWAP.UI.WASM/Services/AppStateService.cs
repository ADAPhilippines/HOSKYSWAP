using System.ComponentModel;
using System.Runtime.CompilerServices;
using HOSKYSWAP.Common;
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

    private Order? _currentOrder = null;

    public Order? CurrentOrder
    {
        get => _currentOrder;
        set
        {
            _currentOrder = value;
            NotifyPropertyChanged();
        }
    }

    private decimal _totalFeesRugpulled = 0.000000m;
    public decimal TotalFeesRugpulled
    {
        get => _totalFeesRugpulled;
        set
        {
            _totalFeesRugpulled = value;
            NotifyPropertyChanged();
        }
    }
    
    private List<Order>? _openBuyOrders = null;
    public List<Order>? OpenBuyOrders
    {
        get => _openBuyOrders;
        set
        {
            _openBuyOrders = value;
            NotifyPropertyChanged();
        }
    }
    
    private List<Order>? _openSellOrders = null;
    public List<Order>? OpenSellOrders
    {
        get => _openSellOrders;
        set
        {
            _openSellOrders = value;
            NotifyPropertyChanged();
        }
    }
    
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")  
    {  
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }  
    
    public event PropertyChangedEventHandler? PropertyChanged;
}