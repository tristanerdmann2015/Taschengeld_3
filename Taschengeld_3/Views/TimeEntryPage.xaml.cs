using Taschengeld_3.Models;
using Taschengeld_3.Services;
using Taschengeld_3.ViewModels;

namespace Taschengeld_3.Views;

public partial class TimeEntryPage : ContentPage
{
    private TimeEntryViewModel? _viewModel;
    private SoundService? _soundService;

    public TimeEntryPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<TimeEntryViewModel>();
        _soundService = ServiceHelper.GetService<SoundService>();
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
                    await DisplayAlertAsync("Fehler beim Laden", $"{ex.GetType().Name}: {ex.Message}", "OK");
                }
            });
        }
    }

    private async void OnNewEntry(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            await _soundService!.PlayClickSound();
            
            _viewModel.ClearForm();
            _viewModel.IsFormVisible = true;
            UpdateFieldVisibility();
        }
    }

    private async void OnApplyFilter(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            await _soundService!.PlayClickSound();
            await _viewModel.ApplyFilter();
        }
    }

    private async void OnEditEntry(object sender, EventArgs e)
    {
        try
        {
            await _soundService!.PlayClickSound();
            
            TimeEntry? entry = null;
            
            if (sender is Button button)
            {
                entry = button.CommandParameter as TimeEntry;
            }
            else if (sender is ImageButton imageButton)
            {
                entry = imageButton.CommandParameter as TimeEntry;
            }
            
            if (entry != null)
            {
                // Reload the task data to ensure Task property is populated
                var task = _viewModel!.AvailableTasks.FirstOrDefault(t => t.Id == entry.TaskId);
                if (task != null)
                {
                    entry.Task = task;
                }
                
                _viewModel!.SelectedEntry = entry;
                _viewModel.IsFormVisible = true;
                UpdateFieldVisibility();
                System.Diagnostics.Debug.WriteLine($"OnEditEntry: Selected entry {entry.Id}");
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
            TimeEntry? entry = null;
            
            if (sender is Button button)
            {
                entry = button.CommandParameter as TimeEntry;
            }
            else if (sender is ImageButton imageButton)
            {
                entry = imageButton.CommandParameter as TimeEntry;
            }
            
            if (entry != null)
            {
                bool confirmed = await DisplayAlertAsync("Bestaetigung", 
                    "Moechten Sie diese Zeiteintragung wirklich loeschen?", 
                    "Ja", "Nein");
                
                if (confirmed)
                {
                    await _soundService!.PlayWarningSound();
                    _viewModel!.SelectedEntry = entry;
                    await _viewModel.DeleteEntry();
                    _viewModel.IsFormVisible = false;
                    System.Diagnostics.Debug.WriteLine($"OnDeleteEntry: Deleted entry {entry.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnDeleteEntry Error: {ex.Message}");
            await DisplayAlertAsync("Fehler", $"Fehler beim Loeschen: {ex.Message}", "OK");
        }
    }

    private async void OnSaveEntry(object sender, EventArgs e)
    {
        try
        {
            await _viewModel!.SaveEntry();
            await _soundService!.PlaySuccessSound();
            _viewModel.IsFormVisible = false;
            UpdateFieldVisibility();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnSaveEntry Error: {ex.Message}");
            await DisplayAlertAsync("Fehler", $"Fehler beim Speichern: {ex.Message}", "OK");
        }
    }

    private async void OnClear(object sender, EventArgs e)
    {
        try
        {
            await _soundService!.PlayWarningSound();
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
        // Nur anzeigen wenn eine Aufgabe ausgewählt ist
        if (_viewModel?.SelectedTask == null)
        {
            DurationSection.IsVisible = false;
            CountSection.IsVisible = false;
            return;
        }

        if (_viewModel.SelectedTask.BillingType == BillingType.PerHour)
        {
            DurationSection.IsVisible = true;
            CountSection.IsVisible = false;
        }
        else if (_viewModel.SelectedTask.BillingType == BillingType.PerCount)
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


