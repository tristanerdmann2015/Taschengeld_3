using Taschengeld_3.ViewModels;
using Taschengeld_3.Models;

namespace Taschengeld_3.Views;

public partial class CostPage : ContentPage
{
    private CostViewModel? _viewModel;

    public CostPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<CostViewModel>();
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel != null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await _viewModel.LoadData();
                    ViewModePicker.SelectedIndex = 1; // Monthly by default
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Fehler", ex.Message, "OK");
                }
            });
        }
    }

    private async void OnLoadCosts(object sender, EventArgs e)
    {
        if (_viewModel != null && ViewModePicker.SelectedIndex >= 0)
        {
            _viewModel.ViewMode = (CostViewModel.CostViewMode)ViewModePicker.SelectedIndex;
            await _viewModel.LoadCostSummaries();
        }
    }

    private async void OnShareEntry(object sender, EventArgs e)
    {
        try
        {
            CostSummary? summary = null;
            
            if (sender is SwipeItem swipeItem)
            {
                summary = swipeItem.CommandParameter as CostSummary;
            }
            
            if (summary != null)
            {
                // Erstelle den Text zum Teilen
                var shareText = new System.Text.StringBuilder();
                shareText.AppendLine($"📊 Taschengeld Übersicht - {summary.Period}");
                shareText.AppendLine();
                shareText.AppendLine($"💰 Gesamtverdienst: {summary.TotalCost:C}");
                shareText.AppendLine($"📝 Anzahl Einträge: {summary.EntryCount}");
                shareText.AppendLine();
                
                if (summary.TaskCosts != null && summary.TaskCosts.Count > 0)
                {
                    shareText.AppendLine("Aufgaben:");
                    foreach (var taskCost in summary.TaskCosts)
                    {
                        shareText.AppendLine($"  • {taskCost.TaskName}: {taskCost.Cost:C}");
                    }
                }
                
                // Teilen-Dialog öffnen
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = shareText.ToString(),
                    Title = $"Taschengeld - {summary.Period}"
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnShareEntry Error: {ex.Message}");
            await DisplayAlert("Fehler", $"Fehler beim Teilen: {ex.Message}", "OK");
        }
    }
}

