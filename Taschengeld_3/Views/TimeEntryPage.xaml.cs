using Taschengeld_3.Models;
using Taschengeld_3.ViewModels;

namespace Taschengeld_3.Views;

public partial class TimeEntryPage : ContentPage
{
    private TimeEntryViewModel? _viewModel;

    public TimeEntryPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<TimeEntryViewModel>();
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
                    UpdateFieldVisibility();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"TimeEntryPage.OnAppearing Error: {ex.Message}");
                    await DisplayAlert("Fehler beim Laden", $"{ex.GetType().Name}: {ex.Message}", "OK");
                }
            });
        }
    }

    private void OnNewEntry(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ClearForm();
            _viewModel.IsFormVisible = true;
            UpdateFieldVisibility();
        }
    }

    private async void OnApplyFilter(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.ApplyFilter();
        }
    }

    private async void OnEditEntry(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is int entryId)
            {
                var entry = _viewModel!.TimeEntries.FirstOrDefault(e => e.Id == entryId);
                if (entry != null)
                {
                    // Reload the task data to ensure Task property is populated
                    var task = _viewModel.AvailableTasks.FirstOrDefault(t => t.Id == entry.TaskId);
                    if (task != null)
                    {
                        entry.Task = task;
                    }
                    
                    _viewModel.SelectedEntry = entry;
                    _viewModel.IsFormVisible = true;
                    UpdateFieldVisibility();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnEditEntry Error: {ex.Message}");
        }
    }

    private async void OnDeleteEntry(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is int entryId)
            {
                var entry = _viewModel!.TimeEntries.FirstOrDefault(e => e.Id == entryId);
                if (entry != null)
                {
                    bool confirmed = await DisplayAlert("Bestätigung", 
                        $"Möchten Sie diese Zeiteintragung wirklich löschen?", 
                        "Ja", "Nein");
                    
                    if (confirmed)
                    {
                        _viewModel.SelectedEntry = entry;
                        await _viewModel.DeleteEntry();
                        _viewModel.IsFormVisible = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnDeleteEntry Error: {ex.Message}");
            await DisplayAlert("Fehler", $"Fehler beim Löschen: {ex.Message}", "OK");
        }
    }

    private async void OnSaveEntry(object sender, EventArgs e)
    {
        try
        {
            await _viewModel!.SaveEntry();
            _viewModel.IsFormVisible = false;
            UpdateFieldVisibility();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnSaveEntry Error: {ex.Message}");
            await DisplayAlert("Fehler", $"Fehler beim Speichern: {ex.Message}", "OK");
        }
    }

    private void OnClear(object sender, EventArgs e)
    {
        try
        {
            _viewModel!.ClearForm();
            _viewModel.IsFormVisible = false;
            UpdateFieldVisibility();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnClear Error: {ex.Message}");
        }
    }

    private void OnTaskPickerSelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateFieldVisibility();
    }

    private void UpdateFieldVisibility()
    {
        if (_viewModel?.SelectedTask?.BillingType == BillingType.PerHour)
        {
            DurationSection.IsVisible = true;
            CountSection.IsVisible = false;
        }
        else if (_viewModel?.SelectedTask?.BillingType == BillingType.PerCount)
        {
            DurationSection.IsVisible = false;
            CountSection.IsVisible = true;
        }
        else
        {
            DurationSection.IsVisible = true;
            CountSection.IsVisible = true;
        }
    }
}


