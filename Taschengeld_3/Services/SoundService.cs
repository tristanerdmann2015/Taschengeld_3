using Plugin.Maui.Audio;

namespace Taschengeld_3.Services;

/// <summary>
/// Service fuer Audio-Feedback bei Benutzerinteraktionen
/// Verwendet plattformspezifische Audio-Ausgabe mit robuster Fallback-Logik
/// </summary>
public class SoundService
{
    private readonly IAudioManager _audioManager;

    public SoundService(IAudioManager audioManager)
    {
        _audioManager = audioManager;
    }

    /// <summary>
    /// Fuehrt haptisches Feedback aus mit Vibration als Fallback
    /// </summary>
    private void PerformHaptic(HapticFeedbackType type = HapticFeedbackType.Click)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (HapticFeedback.Default.IsSupported)
                {
                    HapticFeedback.Default.Perform(type);
                    System.Diagnostics.Debug.WriteLine($"SoundService: HapticFeedback.{type} performed");
                    return; // Wenn Haptic funktioniert, keine zusaetzliche Vibration
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoundService: HapticFeedback failed: {ex.Message}");
            }

            // Fallback: Kurze Vibration nur wenn HapticFeedback nicht verfuegbar
            try
            {
                if (Vibration.Default.IsSupported)
                {
                    var duration = type == HapticFeedbackType.LongPress ? 100 : 60;
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(duration));
                    System.Diagnostics.Debug.WriteLine($"SoundService: Vibration {duration}ms performed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoundService: Vibration failed: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Fuehrt Vibration aus
    /// </summary>
    private void PerformVibration(int milliseconds)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (Vibration.Default.IsSupported)
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(milliseconds));
                    System.Diagnostics.Debug.WriteLine($"SoundService: Vibration {milliseconds}ms performed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SoundService: Vibration failed: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Spielt einen Bestaetigungston ab (fuer Speichern) - freundlicher Ton
    /// </summary>
    public async Task PlaySuccessSound()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("PlaySuccessSound: Playing feedback");
            
#if WINDOWS
            // Windows: System Beep mit aufsteigender Tonfolge (froehlich)
            await Task.Run(() =>
            {
                Console.Beep(800, 100);
                Console.Beep(1000, 100);
                Console.Beep(1200, 150);
            });
#else
            // Mobile: Haptisches Feedback + Vibration
            PerformHaptic(HapticFeedbackType.LongPress);
            await Task.Delay(80);
            PerformHaptic(HapticFeedbackType.Click);
            PerformVibration(100);
#endif
            
            System.Diagnostics.Debug.WriteLine("PlaySuccessSound: Feedback completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlaySuccessSound Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Spielt einen Warnton ab (fuer Abbrechen/Loeschen) - vorsichtiger Ton
    /// </summary>
    public async Task PlayWarningSound()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("PlayWarningSound: Playing feedback");
            
#if WINDOWS
            // Windows: System Beep mit absteigender Tonfolge (Warnung)
            await Task.Run(() =>
            {
                Console.Beep(600, 150);
                Console.Beep(400, 200);
            });
#else
            // Mobile: Doppeltes Haptisches Feedback
            PerformHaptic(HapticFeedbackType.Click);
            await Task.Delay(100);
            PerformHaptic(HapticFeedbackType.Click);
            
            // Doppelte Vibration fuer Warnung
            PerformVibration(50);
            await Task.Delay(100);
            PerformVibration(50);
#endif
            
            System.Diagnostics.Debug.WriteLine("PlayWarningSound: Feedback completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlayWarningSound Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Spielt einen einfachen Klickton ab (fuer Bearbeiten)
    /// </summary>
    public async Task PlayClickSound()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("PlayClickSound: Playing feedback");
            
#if WINDOWS
            // Windows: Kurzer Klick-Ton
            await Task.Run(() =>
            {
                Console.Beep(1000, 50);
            });
#else
            // Mobile: Haptisches Feedback
            PerformHaptic(HapticFeedbackType.Click);
            await Task.CompletedTask;
#endif
            
            System.Diagnostics.Debug.WriteLine("PlayClickSound: Feedback completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlayClickSound Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Spielt einen Teilen-Sound ab (fuer Share-Aktionen)
    /// </summary>
    public async Task PlayShareSound()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("PlayShareSound: Playing feedback");
            
#if WINDOWS
            // Windows: Freundlicher Teilen-Ton
            await Task.Run(() =>
            {
                Console.Beep(700, 80);
                Console.Beep(900, 120);
            });
#else
            // Mobile: Haptisches Feedback + Vibration
            PerformHaptic(HapticFeedbackType.Click);
            await Task.Delay(80);
            PerformVibration(80);
#endif
            
            System.Diagnostics.Debug.WriteLine("PlayShareSound: Feedback completed");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PlayShareSound Error: {ex.Message}");
        }
    }
}
