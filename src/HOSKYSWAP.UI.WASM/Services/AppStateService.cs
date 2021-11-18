using System.ComponentModel;
using System.Runtime.CompilerServices;
using HOSKYSWAP.Common;
using MudBlazor;

namespace HOSKYSWAP.UI.WASM.Services;

public class AppStateService : INotifyPropertyChanged
{
    public readonly DialogOptions DialogOptions = new() {FullWidth = true, DisableBackdropClick = true};

    public readonly string HoskyUnit = "a0028f350aaabe0545fdcb56b039bfb08e4bb4d8c4d7c3c7d481c235484f534b59";

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

    private decimal _marketCap = 0m;

    public decimal MarketCap
    {
        get => _marketCap;
        set
        {
            _marketCap = value;
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

    private List<Order>? _orderHistory = null;

    public List<Order>? OrderHistory
    {
        get => _orderHistory;
        set
        {
            _orderHistory = value;
            NotifyPropertyChanged();
        }
    }

    private OpenOrderRatio? _openOrderRatio = null;

    public OpenOrderRatio? OpenOrderRatio
    {
        get => _openOrderRatio;
        set
        {
            _openOrderRatio = value;
            NotifyPropertyChanged();
        }
    }

    private Order? _lastExcecutedOrder = null;

    public Order? LastExcecutedOrder
    {
        get => _lastExcecutedOrder;
        set
        {
            _lastExcecutedOrder = value;
            NotifyPropertyChanged();
        }
    }
    
    private decimal _dailyVolume = 0.00m;

    public decimal DailyVolume
    {
        get => _dailyVolume;
        set
        {
            _dailyVolume = value;
            NotifyPropertyChanged();
        }
    }
    
    private decimal _adaToUsdRate = 0.00m;

    public decimal AdaToUsdRate
    {
        get => _adaToUsdRate;
        set
        {
            _adaToUsdRate = value;
            NotifyPropertyChanged();
        }
    }
    
    private string _currentWalletAddress = string.Empty;
    public string CurrentWalletAddress
    {
        get => _currentWalletAddress;

        set
        {
            _currentWalletAddress = value;
            NotifyPropertyChanged();
        }
    }

    private ulong _hoskyBalance = 0;
    public ulong HoskyBalance
    {
        get => _hoskyBalance;

        set
        {
            _hoskyBalance = value;
            NotifyPropertyChanged();
        }
    }
    
    private ulong _lovelaceBalance = 0;
    public ulong LovelaceBalance
    {
        get => _lovelaceBalance;

        set
        {
            _lovelaceBalance = value;
            NotifyPropertyChanged();
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}