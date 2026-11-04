using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class AccountEditWindow : Window
{
    private readonly AccountEditViewModel _vm;
    public AccountEditWindow(Account? account = null)
    {
        InitializeComponent();
        _vm = new AccountEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (account != null) _vm.Id = account.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
