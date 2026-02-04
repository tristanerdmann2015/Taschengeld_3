using SQLite;
using Taschengeld_3.Models;

namespace Taschengeld_3.Services;

public class DatabaseService
{
    private static SQLiteAsyncConnection? _database;
    private const string DbFileName = "taschengeld.db";
    private const string MigrationFlagFile = "migration_v1.flag";
    private static readonly object _lockObject = new();
    private static TaskCompletionSource<bool>? _initializationComplete;
    private static bool _isInitialized = false;
    private static readonly SemaphoreSlim _dbSemaphore = new(1, 1);

    public DatabaseService()
    {
        System.Diagnostics.Debug.WriteLine("DatabaseService: Constructor called");
    }

    private async Task Init()
    {
        if (_isInitialized && _database != null)
        {
            return;
        }

        TaskCompletionSource<bool>? completionSource = null;
        bool shouldInitialize = false;

        lock (_lockObject)
        {
            if (_isInitialized && _database != null)
            {
                completionSource = _initializationComplete;
            }
            else if (_initializationComplete == null)
            {
                _initializationComplete = new TaskCompletionSource<bool>();
                completionSource = _initializationComplete;
                shouldInitialize = true;
            }
            else
            {
                completionSource = _initializationComplete;
            }
        }

        if (!shouldInitialize && completionSource != null)
        {
            try
            {
                await completionSource.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DatabaseService.Init: Initialization failed: {ex.Message}");
                throw;
            }
            return;
        }

        System.Diagnostics.Debug.WriteLine("DatabaseService.Init: Starting database initialization");
        
        System.Diagnostics.Debug.WriteLine("DatabaseService.Init: Waiting for semaphore");
        await _dbSemaphore.WaitAsync();
        try
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
            var dbDir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            _database = new SQLiteAsyncConnection(dbPath);
            
            var migrationFlagPath = Path.Combine(FileSystem.AppDataDirectory, MigrationFlagFile);
            if (!File.Exists(migrationFlagPath))
            {
                try
                {
                    await _database.DropTableAsync<TaskItem>();
                }
                catch { }

                try
                {
                    await _database.DropTableAsync<TimeEntry>();
                }
                catch { }

                File.WriteAllText(migrationFlagPath, "migrated");
            }

            await _database.CreateTableAsync<TaskItem>();
            await _database.CreateTableAsync<TimeEntry>();
            
            // Extra delay to ensure native SQLite is fully initialized
            await Task.Delay(200);
            
            _isInitialized = true;
            
            if (_initializationComplete != null && !_initializationComplete.Task.IsCompleted)
            {
                _initializationComplete.SetResult(true);
            }
            System.Diagnostics.Debug.WriteLine("DatabaseService.Init: Database initialization complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DatabaseService.Init ERROR: {ex.Message}");
            _database = null;
            
            if (_initializationComplete != null && !_initializationComplete.Task.IsCompleted)
            {
                _initializationComplete.SetException(ex);
            }
            throw;
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    public async Task<List<TaskItem>> GetAllTasks()
    {
        try
        {
            await Init();
            
            if (_database == null)
            {
                return new List<TaskItem>();
            }
            
            await _dbSemaphore.WaitAsync();
            try
            {
                if (_database == null)
                {
                    return new List<TaskItem>();
                }
                var tasks = await _database.Table<TaskItem>().Where(t => t.IsActive).OrderBy(t => t.Id).ToListAsync();
                return tasks;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAllTasks ERROR: {ex.GetType().Name}: {ex.Message}");
            return new List<TaskItem>();
        }
    }

    public async Task<TaskItem?> GetTask(int id)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"GetTask: Getting task {id}...");
            await Init();
            
            if (_database == null)
            {
                System.Diagnostics.Debug.WriteLine("GetTask: ERROR - _database is null after Init()!");
                return null;
            }
            
            System.Diagnostics.Debug.WriteLine("GetTask: Waiting for database semaphore...");
            await _dbSemaphore.WaitAsync();
            try
            {
                var task = await _database.Table<TaskItem>().Where(t => t.Id == id).FirstOrDefaultAsync();
                System.Diagnostics.Debug.WriteLine($"GetTask: Task {id} found: {task?.Name ?? "null"}");
                return task;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTask ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"GetTask StackTrace: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<int> SaveTask(TaskItem task)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"SaveTask: Starting with task '{task.Name}' (Id={task.Id})...");
            await Init();
            
            if (_database == null)
            {
                System.Diagnostics.Debug.WriteLine("SaveTask: ERROR - _database is null after Init()!");
                return 0;
            }
            
            System.Diagnostics.Debug.WriteLine("SaveTask: Waiting for database semaphore...");
            await _dbSemaphore.WaitAsync();
            try
            {
                if (task.Id == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"SaveTask: Inserting new task '{task.Name}'");
                    var insertedId = await _database.InsertAsync(task);
                    System.Diagnostics.Debug.WriteLine($"SaveTask: Inserted successfully with Id: {insertedId}");
                    return insertedId;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"SaveTask: Updating task Id={task.Id}, Name='{task.Name}'");
                    var rowsAffected = await _database.UpdateAsync(task);
                    System.Diagnostics.Debug.WriteLine($"SaveTask: Updated successfully. Rows affected: {rowsAffected}");
                    if (rowsAffected == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveTask: WARNING - No rows were updated for task {task.Id}");
                    }
                    return task.Id;
                }
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveTask Error: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"SaveTask StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<int> DeleteTask(int id)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTask: Deleting task {id}...");
            await Init();
            
            if (_database == null)
            {
                System.Diagnostics.Debug.WriteLine("DeleteTask: ERROR - _database is null after Init()!");
                return 0;
            }
            
            // Get the task
            var task = await GetTask(id);
            if (task == null)
            {
                System.Diagnostics.Debug.WriteLine($"DeleteTask: Task {id} not found");
                return 0;
            }
            
            System.Diagnostics.Debug.WriteLine($"DeleteTask: Found task {id}, marking as inactive");
            
            // Mark as inactive
            task.IsActive = false;
            System.Diagnostics.Debug.WriteLine($"DeleteTask: About to update task {id} with IsActive=false");
            
            System.Diagnostics.Debug.WriteLine("DeleteTask: Waiting for database semaphore...");
            await _dbSemaphore.WaitAsync();
            try
            {
                var result = await _database.UpdateAsync(task);
                System.Diagnostics.Debug.WriteLine($"DeleteTask: UpdateAsync returned: {result}");
                
                if (result == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"DeleteTask: WARNING - UpdateAsync returned 0, trying alternative approach");
                    // Alternative: Execute raw SQL
                    var sqlResult = await _database.ExecuteAsync("UPDATE TaskItem SET IsActive = 0 WHERE Id = ?", id);
                    System.Diagnostics.Debug.WriteLine($"DeleteTask: Raw SQL update returned: {sqlResult}");
                    return sqlResult;
                }
                
                return result;
            }
            finally
            {
                _dbSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteTask ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"DeleteTask StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public async Task<List<TimeEntry>> GetAllTimeEntries()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("GetAllTimeEntries: Starting...");
            await Init();
            
            if (_database == null)
            {
                System.Diagnostics.Debug.WriteLine("GetAllTimeEntries: ERROR - _database is null after Init()!");
                return new List<TimeEntry>();
            }

            System.Diagnostics.Debug.WriteLine("GetAllTimeEntries: Waiting for database semaphore...");
            await _dbSemaphore.WaitAsync();
            List<TimeEntry> entries;
            try
            {
                System.Diagnostics.Debug.WriteLine("GetAllTimeEntries: Querying all time entries...");
                entries = await _database.Table<TimeEntry>().ToListAsync();
                System.Diagnostics.Debug.WriteLine($"GetAllTimeEntries: Retrieved {entries.Count} entries");
            }
            finally
            {
                _dbSemaphore.Release();
            }

            // Load tasks for each entry OUTSIDE the semaphore to avoid deadlock
            foreach (var entry in entries)
            {
                try
                {
                    var task = await GetTask(entry.TaskId);
                    entry.Task = task;
                    
                    if (task == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetAllTimeEntries: WARNING - Task {entry.TaskId} not found for entry {entry.Id}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetAllTimeEntries: Error loading task for entry {entry.Id}: {ex.Message}");
                    entry.Task = null;
                }
            }
            
            return entries;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetAllTimeEntries ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"GetAllTimeEntries StackTrace: {ex.StackTrace}");
            return new List<TimeEntry>();
        }
    }

    public async Task<List<TimeEntry>> GetTimeEntriesByDate(DateTime startDate, DateTime endDate)
    {
        System.Diagnostics.Debug.WriteLine($"GetTimeEntriesByDate: Querying entries from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
        try
        {
            await Init();
            
            if (_database == null)
            {
                System.Diagnostics.Debug.WriteLine("GetTimeEntriesByDate: ERROR - _database is null after Init()!");
                return new List<TimeEntry>();
            }
            
            System.Diagnostics.Debug.WriteLine("GetTimeEntriesByDate: Waiting for database semaphore...");
            await _dbSemaphore.WaitAsync();
            List<TimeEntry> entries;
            try
            {
                // First, get all entries in the date range (without Task loading)
                entries = await _database.Table<TimeEntry>()
                    .Where(t => t.EntryDate >= startDate && t.EntryDate <= endDate)
                    .ToListAsync();

                System.Diagnostics.Debug.WriteLine($"GetTimeEntriesByDate: Found {entries.Count} entries");
            }
            finally
            {
                _dbSemaphore.Release();
            }

            // Then, load the associated tasks one by one OUTSIDE the semaphore to avoid deadlock
            foreach (var entry in entries)
            {
                try
                {
                    var task = await GetTask(entry.TaskId);
                    entry.Task = task;
                    
                    if (task == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"GetTimeEntriesByDate: WARNING - Task {entry.TaskId} not found for entry {entry.Id}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"GetTimeEntriesByDate: Loaded task '{task.Name}' for entry {entry.Id}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"GetTimeEntriesByDate: Error loading task for entry {entry.Id}: {ex.Message}");
                    entry.Task = null;
                }
            }

            System.Diagnostics.Debug.WriteLine($"GetTimeEntriesByDate: Returning {entries.Count} entries");
            return entries;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetTimeEntriesByDate ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"GetTimeEntriesByDate StackTrace: {ex.StackTrace}");
            return new List<TimeEntry>();
        }
    }

    public async Task<List<TimeEntry>> GetTimeEntriesByTask(int taskId)
    {
        await Init();
        System.Diagnostics.Debug.WriteLine("GetTimeEntriesByTask: Waiting for database semaphore...");
        await _dbSemaphore.WaitAsync();
        try
        {
            return await _database!.Table<TimeEntry>().Where(t => t.TaskId == taskId).ToListAsync();
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    public async Task<int> SaveTimeEntry(TimeEntry entry)
    {
        await Init();
        entry.CalculatePrice();
        
        System.Diagnostics.Debug.WriteLine("SaveTimeEntry: Waiting for database semaphore...");
        await _dbSemaphore.WaitAsync();
        try
        {
            if (entry.Id == 0)
            {
                return await _database!.InsertAsync(entry);
            }
            else
            {
                await _database!.UpdateAsync(entry);
                return entry.Id;
            }
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    public async Task<int> DeleteTimeEntry(int id)
    {
        await Init();
        System.Diagnostics.Debug.WriteLine("DeleteTimeEntry: Waiting for database semaphore...");
        await _dbSemaphore.WaitAsync();
        try
        {
            return await _database!.DeleteAsync<TimeEntry>(id);
        }
        finally
        {
            _dbSemaphore.Release();
        }
    }

    public async Task<decimal> GetTotalCostForPeriod(DateTime startDate, DateTime endDate)
    {
        await Init();
        var entries = await GetTimeEntriesByDate(startDate, endDate);
        return (decimal)entries.Sum(e => e.TotalPrice);
    }

    public async Task<CostSummary> GetWeeklyCostSummary(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var startOfWeek = date.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));
        var endOfWeek = startOfWeek.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);

        var entries = await GetTimeEntriesByDate(startOfWeek, endOfWeek);
        var summary = BuildCostSummary($"Woche vom {startOfWeek:dd.MM.yyyy}", entries);
        return summary;
    }

    public async Task<CostSummary> GetMonthlyCostSummary(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

        var entries = await GetTimeEntriesByDate(startDate, endDate);
        var summary = BuildCostSummary($"{startDate:MMMM yyyy}", entries);
        return summary;
    }

    public async Task<CostSummary> GetQuarterlyCostSummary(int year, int quarter)
    {
        var startMonth = (quarter - 1) * 3 + 1;
        var startDate = new DateTime(year, startMonth, 1);
        var endDate = startDate.AddMonths(3).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

        var entries = await GetTimeEntriesByDate(startDate, endDate);
        var summary = BuildCostSummary($"Q{quarter} {year}", entries);
        return summary;
    }

    public async Task<List<CostSummary>> GetMonthlyCostSummaries(int year)
    {
        var summaries = new List<CostSummary>();
        for (int month = 1; month <= 12; month++)
        {
            summaries.Add(await GetMonthlyCostSummary(year, month));
        }
        return summaries;
    }

    public async Task<List<CostSummary>> GetWeeklyCostSummariesForMonth(int year, int month)
    {
        var summaries = new List<CostSummary>();
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            summaries.Add(await GetWeeklyCostSummary(currentDate));
            currentDate = currentDate.AddDays(7);
        }

        return summaries;
    }

    private CostSummary BuildCostSummary(string period, List<TimeEntry> entries)
    {
        var summary = new CostSummary
        {
            Period = period,
            TotalCost = (decimal)entries.Sum(e => e.TotalPrice),
            EntryCount = entries.Count
        };

        var taskGroups = entries.GroupBy(e => e.Task?.Name ?? "Unknown");
        foreach (var group in taskGroups)
        {
            summary.TaskCosts.Add(new TaskCostSummary
            {
                TaskName = group.Key,
                Cost = (decimal)group.Sum(e => e.TotalPrice),
                Count = group.Count()
            });
        }

        return summary;
    }
}
