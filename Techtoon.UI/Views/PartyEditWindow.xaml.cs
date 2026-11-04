using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class PartyEditWindow : Window
{
    private readonly PartyEditViewModel _vm;
    public PartyEditWindow(Party? party = null)
    {
        InitializeComponent();
        _vm = new PartyEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (party != null) _vm.Id = party.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
