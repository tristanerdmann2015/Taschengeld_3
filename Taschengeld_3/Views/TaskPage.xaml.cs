using Taschengeld_3.Models;
using Taschengeld_3.Services;
using Taschengeld_3.ViewModels;

namespace Taschengeld_3.Views;

public partial class TaskPage : ContentPage
{
    private TaskViewModel? _viewModel;
    private SoundService? _soundService;

    public TaskPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<TaskViewModel>();
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
                    await _viewModel.LoadTasks();
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Fehler", ex.Message, "OK");
                }
            });
        }
    }

    private async void OnNewTask(object sender, EventArgs e)
    {
        try
        {
            await _soundService!.PlayClickSound();
            
            _viewModel!.IsFormVisible = true;
            if (BillingPicker != null)
            {
                BillingPicker.SelectedIndex = 0;
            }
            if (FormLabel != null)
            {
                FormLabel.Text = "Neue Aufgabe";
            }
            System.Diagnostics.Debug.WriteLine("OnNewTask: Form is now visible");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnNewTask Error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async void OnEditTask(object sender, EventArgs e)
    {
        try
        {
            await _soundService!.PlayClickSound();
            
            TaskItem? task = null;
            
            if (sender is Button button)
            {
                task = button.CommandParameter as TaskItem;
            }
            else if (sender is ImageButton imageButton)
            {
                task = imageButton.CommandParameter as TaskItem;
            }
            
            if (task != null)
            {
                _viewModel!.SelectedTask = task;
                if (BillingPicker != null)
                {
                    BillingPicker.SelectedIndex = (int)task.BillingType;
                }
                if (FormLabel != null)
                {
                    FormLabel.Text = "Aufgabe bearbeiten";
                }
                System.Diagnostics.Debug.WriteLine($"OnEditTask: Selected task {task.Id}: {task.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnEditTask Error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async void OnDeleteTask(object sender, EventArgs e)
    {
        try
        {
            TaskItem? task = null;
            
            if (sender is Button button)
            {
                task = button.CommandParameter as TaskItem;
            }
            else if (sender is ImageButton imageButton)
            {
                task = imageButton.CommandParameter as TaskItem;
            }
            
            if (task != null)
            {
                bool confirmed = await DisplayAlertAsync("Bestaetigung", 
                    $"Moechten Sie die Aufgabe '{task.Name}' wirklich loeschen?", 
                    "Ja", "Nein");
                
                if (confirmed)
                {
                    await _soundService!.PlayWarningSound();
                    _viewModel!.SelectedTask = task;
                    await _viewModel.DeleteTask();
                    System.Diagnostics.Debug.WriteLine($"OnDeleteTask: Deleted task {task.Id}: {task.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnDeleteTask Exception: {ex.GetType().Name}: {ex.Message}");
            await DisplayAlertAsync("Fehler", $"Fehler beim Loeschen: {ex.Message}", "OK");
        }
    }

    private async void OnSaveTask(object sender, EventArgs e)
    {
        try
        {
            if (BillingPicker.SelectedIndex >= 0)
            {
                _viewModel!.SelectedBillingType = (BillingType)BillingPicker.SelectedIndex;
            }
            await _viewModel!.SaveTask();
            await _soundService!.PlaySuccessSound();
            BillingPicker.SelectedIndex = -1;
            FormLabel.Text = "Neue Aufgabe";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Fehler", $"Fehler beim Speichern: {ex.Message}", "OK");
        }
    }

    private async void OnClear(object sender, EventArgs e)
    {
        await _soundService!.PlayWarningSound();
        _viewModel!.ClearForm();
        BillingPicker.SelectedIndex = -1;
        FormLabel.Text = "Neue Aufgabe";
    }
}

