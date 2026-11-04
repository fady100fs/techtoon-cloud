using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class PartiesViewModel : ViewModelBase
{
    private ObservableCollection<Party> _parties = new();
    private string _searchText = "";
    private Party? _selectedParty;

    public ObservableCollection<Party> Parties { get => _parties; set => SetProperty(ref _parties, value); }
    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) ApplyFilter(); } }
    public Party? SelectedParty { get => _selectedParty; set => SetProperty(ref _selectedParty, value); }

    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public PartiesViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedParty), () => SelectedParty != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedParty != null);
    }

    public async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var list = await db.Parties.Include(p => p.Account).OrderBy(p => p.Name).Take(500).ToListAsync();
            Parties = new ObservableCollection<Party>(list);
            ApplyFilter();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        var s = SearchText.Trim().ToLower();
        var filtered = Parties.Where(p => (p.Name?.ToLower().Contains(s) == true) || (p.Phone?.ToLower().Contains(s) == true)).ToList();
        Parties = new ObservableCollection<Party>(filtered);
    }

    private void OpenEdit(Party? party)
    {
        var window = new PartyEditWindow(party) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedParty == null) return;
        var confirm = System.Windows.MessageBox.Show("Delete: " + SelectedParty.Name + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var p = await db.Parties.FirstOrDefaultAsync(x => x.Id == SelectedParty.Id);
            if (p != null) { db.Parties.Remove(p); await db.SaveChangesAsync(); await LoadAsync(); }
        }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
