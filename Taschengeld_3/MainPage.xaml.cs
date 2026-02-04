using Taschengeld_3.Views;

namespace Taschengeld_3
{
    public partial class MainPage : TabbedPage
    {
        public MainPage()
        {
            System.Diagnostics.Debug.WriteLine("MainPage: Constructor called");
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("MainPage: InitializeComponent completed");
            LoadPages();
            System.Diagnostics.Debug.WriteLine("MainPage: LoadPages completed");
        }

        private void LoadPages()
        {
            System.Diagnostics.Debug.WriteLine("MainPage.LoadPages: Starting...");
            try
            {
                System.Diagnostics.Debug.WriteLine("MainPage.LoadPages: Getting TaskPage service");
                var taskPage = ServiceHelper.GetService<TaskPage>();
                System.Diagnostics.Debug.WriteLine($"MainPage.LoadPages: TaskPage = {(taskPage == null ? "null" : "OK")}");
                
                System.Diagnostics.Debug.WriteLine("MainPage.LoadPages: Getting TimeEntryPage service");
                var timeEntryPage = ServiceHelper.GetService<TimeEntryPage>();
                System.Diagnostics.Debug.WriteLine($"MainPage.LoadPages: TimeEntryPage = {(timeEntryPage == null ? "null" : "OK")}");
                
                System.Diagnostics.Debug.WriteLine("MainPage.LoadPages: Getting CostPage service");
                var costPage = ServiceHelper.GetService<CostPage>();
                System.Diagnostics.Debug.WriteLine($"MainPage.LoadPages: CostPage = {(costPage == null ? "null" : "OK")}");

                if (taskPage != null)
                {
                    taskPage.Title = "📋 Aufgaben";
                    Children.Add(taskPage);
                    System.Diagnostics.Debug.WriteLine("MainPage.LoadPages: TaskPage added");
                }

                if (timeEntryPage != null)
                {
                    timeEntryPage.Title = "⏱️ Zeiten";
                    Children.Add(timeEntryPage);
                    System.Diagnostics.Debug.WriteLine("MainPage.LoadPages: TimeEntryPage added");
                }

                if (costPage != null)
                {
                    costPage.Title = "💰 Verdienst";
                    Children.Add(costPage);
                    System.Diagnostics.Debug.WriteLine("MainPage.LoadPages: CostPage added");
                }
                
                System.Diagnostics.Debug.WriteLine($"MainPage.LoadPages: Completed. Total children: {Children.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MainPage.LoadPages: Exception - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"MainPage.LoadPages: StackTrace: {ex.StackTrace}");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Fehler beim Laden der Seiten", ex.Message + "\n\n" + ex.StackTrace, "OK");
                });
            }
        }
    }
}




