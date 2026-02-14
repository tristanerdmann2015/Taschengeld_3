using Microsoft.Extensions.DependencyInjection;
using Taschengeld_3.Views;

namespace Taschengeld_3
{
    public partial class App : Application
    {
        private Window? _mainWindow;
        
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Starte mit der animierten Splash-Seite
            _mainWindow = new Window(new SplashPage());
            
            // Lifecycle-Events
            _mainWindow.Resumed += OnWindowResumed;
            
            return _mainWindow;
        }

        private void OnWindowResumed(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("App: Window Resumed");
            
            // Wenn die MainPage eine TabbedPage ist, aktualisiere sie
            if (_mainWindow?.Page is MainPage mainPage)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        // Force UI refresh
                        mainPage.InvalidateMeasure();
                        System.Diagnostics.Debug.WriteLine($"App: MainPage refreshed, Children count: {mainPage.Children.Count}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"App: OnWindowResumed Error: {ex.Message}");
                    }
                });
            }
        }
    }
}


