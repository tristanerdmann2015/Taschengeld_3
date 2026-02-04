using Taschengeld_3.Models;
using Taschengeld_3.ViewModels;

namespace Taschengeld_3.Views;

public partial class TaskPage : ContentPage
{
    private TaskViewModel? _viewModel;

    public TaskPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<TaskViewModel>();
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
                    await DisplayAlert("Fehler", ex.Message, "OK");
                }
            });
        }
    }

    private void OnNewTask(object sender, EventArgs e)
    {
        try
        {
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

    private void OnEditTask(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int taskId)
        {
            // Find the task by ID
            var task = _viewModel!.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                _viewModel.SelectedTask = task;
                BillingPicker.SelectedIndex = (int)task.BillingType;
                FormLabel.Text = "Aufgabe bearbeiten";
                System.Diagnostics.Debug.WriteLine($"OnEditTask: Selected task {taskId}");
            }
        }
    }

    private async void OnDeleteTask(object sender, EventArgs e)
    {
        try
        {
            if (sender is Button button && button.CommandParameter is int taskId)
            {
                System.Diagnostics.Debug.WriteLine($"OnDeleteTask: Attempting to delete task {taskId}");
                
                // Find the task by ID
                var task = _viewModel!.Tasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    System.Diagnostics.Debug.WriteLine($"OnDeleteTask: Found task {taskId} in collection: {task.Name}");
                    _viewModel.SelectedTask = task;
                    await _viewModel.DeleteTask();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"OnDeleteTask: Task {taskId} NOT found in collection, total tasks: {_viewModel.Tasks.Count}");
                    foreach (var t in _viewModel.Tasks)
                    {
                        System.Diagnostics.Debug.WriteLine($"OnDeleteTask: Available task Id={t.Id}, Name={t.Name}");
                    }
                    await DisplayAlert("Fehler", $"Aufgabe mit ID {taskId} nicht gefunden", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnDeleteTask Exception: {ex.GetType().Name}: {ex.Message}");
            await DisplayAlert("Fehler", $"Fehler beim Löschen: {ex.Message}", "OK");
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
            BillingPicker.SelectedIndex = -1;
            FormLabel.Text = "Neue Aufgabe";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fehler", $"Fehler beim Speichern: {ex.Message}", "OK");
        }
    }

    private void OnClear(object sender, EventArgs e)
    {
        _viewModel!.ClearForm();
        BillingPicker.SelectedIndex = -1;
        FormLabel.Text = "Neue Aufgabe";
    }

    private void OnSwipeEdit(object sender, EventArgs e)
    {
        try
        {
            // Handle both Button (from new layout) and SwipeItem (if used)
            TaskItem? task = null;
            
            if (sender is Button button)
            {
                task = button.CommandParameter as TaskItem;
            }
            else if (sender is SwipeItem swipeItem)
            {
                task = swipeItem.CommandParameter as TaskItem;
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
                System.Diagnostics.Debug.WriteLine($"OnSwipeEdit: Selected task {task.Id}: {task.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OnSwipeEdit: Task parameter is null, sender type: {sender?.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnSwipeEdit Error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async void OnSwipeDelete(object sender, EventArgs e)
    {
        try
        {
            // Handle both Button (from new layout) and SwipeItem (if used)
            TaskItem? task = null;
            
            if (sender is Button button)
            {
                task = button.CommandParameter as TaskItem;
            }
            else if (sender is SwipeItem swipeItem)
            {
                task = swipeItem.CommandParameter as TaskItem;
            }
            
            if (task != null)
            {
                _viewModel!.SelectedTask = task;
                await _viewModel.DeleteTask();
                System.Diagnostics.Debug.WriteLine($"OnSwipeDelete: Deleted task {task.Id}: {task.Name}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OnSwipeDelete: Task parameter is null, sender type: {sender?.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnSwipeDelete Error: {ex.GetType().Name}: {ex.Message}");
        }
    }
}

