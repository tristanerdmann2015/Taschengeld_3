namespace Taschengeld_3.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        try
        {
            // Starte die Animationen
            await RunSplashAnimations();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SplashPage Animation Error: {ex.Message}");
        }
        
        // Navigiere zur Hauptseite
        await NavigateToMainPage();
    }

    private async Task RunSplashAnimations()
    {
        // Muenze von oben einfliegen lassen
        CoinBorder.TranslationY = -300;
        CoinBorder.Opacity = 0;
        
        // Einflug-Animation
        await Task.WhenAll(
            CoinBorder.TranslateToAsync(0, 0, 600, Easing.BounceOut),
            CoinBorder.FadeToAsync(1, 400)
        );
        
        // Titel einblenden
        await Task.WhenAll(
            AppTitle.FadeToAsync(1, 400),
            LoadingText.FadeToAsync(1, 400)
        );
        
        // Rotation der Muenze (2 vollstaendige Umdrehungen)
        var rotationTask = RotateCoin();
        
        // Pulsieren der Muenze
        var pulseTask = PulseCoin();
        
        // Warte auf alle Animationen (mindestens 2 Sekunden)
        await Task.WhenAll(
            rotationTask,
            pulseTask,
            Task.Delay(2000)
        );
    }

    private async Task RotateCoin()
    {
        // Kontinuierliche Rotation
        for (int i = 0; i < 2; i++)
        {
            await CoinBorder.RotateToAsync(360, 800, Easing.Linear);
            CoinBorder.Rotation = 0;
        }
    }

    private async Task PulseCoin()
    {
        // Pulsieren
        for (int i = 0; i < 3; i++)
        {
            await CoinBorder.ScaleToAsync(1.1, 300, Easing.SinInOut);
            await CoinBorder.ScaleToAsync(1.0, 300, Easing.SinInOut);
        }
    }

    private async Task NavigateToMainPage()
    {
        // Ausblend-Animation
        await Task.WhenAll(
            CoinBorder.FadeToAsync(0, 300),
            AppTitle.FadeToAsync(0, 300),
            LoadingText.FadeToAsync(0, 300),
            LoadingIndicator.FadeToAsync(0, 300)
        );
        
        // Navigiere zur Hauptseite
        if (Application.Current?.Windows.Count > 0)
        {
            var window = Application.Current.Windows[0];
            window.Page = new MainPage();
        }
    }
}
