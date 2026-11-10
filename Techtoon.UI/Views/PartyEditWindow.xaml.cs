using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class PartyEditWindow : Window
{
    private readonly PartyEditViewModel _vm;
    private readonly PartyPolicyViewModel _policyVm;

    public PartyEditWindow(Party? party = null)
    {
        InitializeComponent();
        _vm = new PartyEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (party != null) _vm.Id = party.Id;

        // قسم «سياسات العميل»: سياسة الخصم واستثناء رسوم التأخير (B6).
        _policyVm = new PartyPolicyViewModel { PartyId = party?.Id ?? 0 };
        PartyPolicyPanel.DataContext = _policyVm;

        Loaded += async (_, _) => { await _vm.LoadAsync(); _policyVm.Load(); };
    }
}
