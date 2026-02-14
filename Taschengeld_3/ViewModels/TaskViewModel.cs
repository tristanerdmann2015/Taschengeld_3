using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Taschengeld_3.Models;
using Taschengeld_3.Services;

namespace Taschengeld_3.ViewModels;

public class TaskViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;
    private ObservableCollection<TaskItem> _tasks = new();
    private TaskItem? _selectedTask;
    private string _taskName = string.Empty;
    private double _taskPrice;
    private BillingType _selectedBillingType = BillingType.PerHour;
    private bool _isEditMode;
    private bool _isFormVisible = false;

    public ObservableCollection<TaskItem> Tasks
    {
        get => _tasks;
        set
        {
            _tasks = value;
            OnPropertyChanged();
        }
    }

    public TaskItem? SelectedTask
    {
        get => _selectedTask;
        set
        {
            _selectedTask = value;
            OnPropertyChanged();
            if (value != null)
            {
                TaskName = value.Name;
                TaskPrice = (double)value.Price;
                SelectedBillingType = value.BillingType;
                IsEditMode = true;
                IsFormVisible = true;
            }
        }
    }

    public string TaskName
    {
        get => _taskName;
        set
        {
            _taskName = value;
            OnPropertyChanged();
        }
    }

    public double TaskPrice
    {
        get => _taskPrice;
        set
        {
            _taskPrice = value;
            OnPropertyChanged();
        }
    }

    public BillingType SelectedBillingType
    {
        get => _selectedBillingType;
        set
        {
            _selectedBillingType = value;
            OnPropertyChanged();
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            _isEditMode = value;
            OnPropertyChanged();
        }
    }

    public bool IsFormVisible
    {
        get => _isFormVisible;
        set
        {
            _isFormVisible = value;
            OnPropertyChanged();
        }
    }

    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public TaskViewModel()
    {
        _databaseService = ServiceHelper.GetService<DatabaseService>()!;
        
        // Initialize commands
        EditCommand = new Command<TaskItem>(task =>
        {
            if (task != null)
            {
                SelectedTask = task;
            }
        });

        DeleteCommand = new Command<int>(async taskId =>
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                SelectedTask = task;
                await DeleteTask();
            }
        });
        
        // Load tasks immediately
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await LoadTasks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tasks: {ex.Message}");
            }
        });
    }

    public async Task LoadTasks()
    {
        try
        {
            var tasks = await _databaseService.GetAllTasks();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                System.Diagnostics.Debug.WriteLine($"LoadTasks: Loaded {tasks.Count} tasks from database");
                
                // Clear and repopulate instead of creating new collection
                // This preserves the binding references
                _tasks.Clear();
                foreach (var task in tasks)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadTasks: Adding task {task.Id}: {task.Name}");
                    _tasks.Add(task);
                }
                System.Diagnostics.Debug.WriteLine($"LoadTasks: Collection now contains {_tasks.Count} tasks");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadTasks Error: {ex.Message}");
            throw;
        }
    }

    public async Task SaveTask()
    {
        System.Diagnostics.Debug.WriteLine($"SaveTask: ENTRY - SelectedTask={(_selectedTask == null ? "null" : $"Id={_selectedTask.Id}")}");
        System.Diagnostics.Debug.WriteLine($"SaveTask: TaskName='{TaskName}', TaskPrice={TaskPrice}, BillingType={SelectedBillingType}");
        
        if (string.IsNullOrWhiteSpace(TaskName))
        {
            await DialogService.DisplayAlertAsync("Fehler", "Bitte geben Sie einen Namen ein.", "OK");
            return;
        }

        if (TaskPrice <= 0)
        {
            await DialogService.DisplayAlertAsync("Fehler", "Bitte geben Sie einen gueltigen Preis ein.", "OK");
            return;
        }

        try
        {
            if (SelectedTask == null)
            {
                // Create new task
                System.Diagnostics.Debug.WriteLine($"SaveTask: Creating new task");
                var newTask = new TaskItem
                {
                    Name = TaskName,
                    Price = TaskPrice,
                    BillingType = SelectedBillingType,
                    IsActive = true
                };
                System.Diagnostics.Debug.WriteLine($"SaveTask: New task created - BillingTypeValue={newTask.BillingTypeValue}");
                
                // Save the task
                System.Diagnostics.Debug.WriteLine($"SaveTask: About to save new task");
                var savedId = await _databaseService.SaveTask(newTask);
                System.Diagnostics.Debug.WriteLine($"SaveTask: Save returned Id={savedId}");
                
                // Update the ID and add to collection
                newTask.Id = savedId;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _tasks.Add(newTask);
                    System.Diagnostics.Debug.WriteLine($"SaveTask: New task added to collection with Id={savedId}");
                });
            }
            else
            {
                // Update existing task
                System.Diagnostics.Debug.WriteLine($"SaveTask: Updating existing task Id={SelectedTask.Id}, Name='{SelectedTask.Name}'");
                
                // Update the SelectedTask object directly with new values
                SelectedTask.Name = TaskName;
                SelectedTask.Price = TaskPrice;
                SelectedTask.BillingType = SelectedBillingType;
                
                System.Diagnostics.Debug.WriteLine($"SaveTask: Updated object - Name='{SelectedTask.Name}', Price={SelectedTask.Price}, BillingTypeValue={SelectedTask.BillingTypeValue}");
                
                // Save to database
                System.Diagnostics.Debug.WriteLine($"SaveTask: About to save updated task to database");
                await _databaseService.SaveTask(SelectedTask);
                System.Diagnostics.Debug.WriteLine($"SaveTask: Updated task saved to database");
            }

            ClearForm();
            System.Diagnostics.Debug.WriteLine($"SaveTask: About to reload all tasks from database");
            await LoadTasks();
            System.Diagnostics.Debug.WriteLine($"SaveTask: Tasks reloaded, total count={_tasks.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveTask Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            await DialogService.DisplayAlertAsync("Fehler beim Speichern", ex.Message, "OK");
        }
    }

    public async Task DeleteTask()
    {
        if (SelectedTask == null)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTask: SelectedTask is null, returning");
            return;
        }

        var taskId = SelectedTask.Id;
        var taskName = SelectedTask.Name;
        
        System.Diagnostics.Debug.WriteLine($"DeleteTask: About to delete task Id={taskId}, Name='{taskName}'");

        var confirm = await DialogService.DisplayConfirmAsync("Bestaetigung", 
            $"Moechten Sie die Aufgabe '{taskName}' wirklich loeschen?", "Ja", "Nein");

        if (confirm)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DeleteTask: User confirmed deletion, calling DatabaseService");
                await _databaseService.DeleteTask(taskId);
                System.Diagnostics.Debug.WriteLine($"DeleteTask: DatabaseService.DeleteTask completed");
                
                ClearForm();
                System.Diagnostics.Debug.WriteLine($"DeleteTask: About to reload tasks");
                await LoadTasks();
                System.Diagnostics.Debug.WriteLine($"DeleteTask: Tasks reloaded, total count={_tasks.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteTask Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                await DialogService.DisplayAlertAsync("Fehler beim Loeschen", ex.Message, "OK");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTask: User cancelled deletion");
        }
    }

    public void ClearForm()
    {
        TaskName = string.Empty;
        TaskPrice = 0;
        SelectedBillingType = BillingType.PerHour;
        SelectedTask = null;
        IsEditMode = false;
        IsFormVisible = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
