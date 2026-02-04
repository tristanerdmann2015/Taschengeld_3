using Microsoft.Extensions.Logging;
using Taschengeld_3.Services;
using Taschengeld_3.ViewModels;
using Taschengeld_3.Views;

namespace Taschengeld_3
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register Services
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<TaskViewModel>();
            builder.Services.AddSingleton<TimeEntryViewModel>();
            builder.Services.AddSingleton<CostViewModel>();

            // Register Views
            builder.Services.AddSingleton<TaskPage>();
            builder.Services.AddSingleton<TimeEntryPage>();
            builder.Services.AddSingleton<CostPage>();
            builder.Services.AddSingleton<MainPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            ServiceHelper.SetServiceProvider(app.Services);
            return app;
        }
    }
}


