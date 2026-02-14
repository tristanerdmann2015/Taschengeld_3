namespace Taschengeld_3.Behaviors;

/// <summary>
/// Hilfsmethode fuer robustes haptisches Feedback mit Vibration als Fallback
/// </summary>
public static class HapticHelper
{
    public static void PerformHaptic()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Kurzes haptisches Feedback
                if (HapticFeedback.Default.IsSupported)
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                    System.Diagnostics.Debug.WriteLine("HapticHelper: HapticFeedback.Click performed");
                }
                else if (Vibration.Default.IsSupported)
                {
                    // Fallback: Kurze Vibration
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(60));
                    System.Diagnostics.Debug.WriteLine("HapticHelper: Vibration 60ms performed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HapticHelper: HapticFeedback failed: {ex.Message}");
            }
        });
    }

    public static void PerformStrongHaptic()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (HapticFeedback.Default.IsSupported)
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                    System.Diagnostics.Debug.WriteLine("HapticHelper: HapticFeedback.LongPress performed");
                }
                else if (Vibration.Default.IsSupported)
                {
                    // Fallback: Etwas laengere Vibration
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
                    System.Diagnostics.Debug.WriteLine("HapticHelper: Strong Vibration 100ms performed");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HapticHelper: Strong HapticFeedback failed: {ex.Message}");
            }
        });
    }
}

/// <summary>
/// Behavior fuer haptisches Feedback bei Entry-Fokus
/// </summary>
public class EntryHapticBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        base.OnAttachedTo(entry);
        entry.Focused += OnEntryFocused;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        base.OnDetachingFrom(entry);
        entry.Focused -= OnEntryFocused;
    }

    private void OnEntryFocused(object? sender, FocusEventArgs e)
    {
        if (e.IsFocused)
        {
            HapticHelper.PerformHaptic();
        }
    }
}

/// <summary>
/// Behavior fuer haptisches Feedback bei Picker-Auswahl
/// </summary>
public class PickerHapticBehavior : Behavior<Picker>
{
    protected override void OnAttachedTo(Picker picker)
    {
        base.OnAttachedTo(picker);
        picker.SelectedIndexChanged += OnPickerSelectedIndexChanged;
        picker.Focused += OnPickerFocused;
    }

    protected override void OnDetachingFrom(Picker picker)
    {
        base.OnDetachingFrom(picker);
        picker.SelectedIndexChanged -= OnPickerSelectedIndexChanged;
        picker.Focused -= OnPickerFocused;
    }

    private void OnPickerFocused(object? sender, FocusEventArgs e)
    {
        if (e.IsFocused)
        {
            HapticHelper.PerformHaptic();
        }
    }

    private void OnPickerSelectedIndexChanged(object? sender, EventArgs e)
    {
        HapticHelper.PerformStrongHaptic();
    }
}

/// <summary>
/// Behavior fuer haptisches Feedback bei DatePicker
/// </summary>
public class DatePickerHapticBehavior : Behavior<DatePicker>
{
    protected override void OnAttachedTo(DatePicker picker)
    {
        base.OnAttachedTo(picker);
        picker.DateSelected += OnDateSelected;
        picker.Focused += OnPickerFocused;
    }

    protected override void OnDetachingFrom(DatePicker picker)
    {
        base.OnDetachingFrom(picker);
        picker.DateSelected -= OnDateSelected;
        picker.Focused -= OnPickerFocused;
    }

    private void OnPickerFocused(object? sender, FocusEventArgs e)
    {
        if (e.IsFocused)
        {
            HapticHelper.PerformHaptic();
        }
    }

    private void OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        HapticHelper.PerformStrongHaptic();
    }
}

/// <summary>
/// Behavior fuer haptisches Feedback bei TimePicker
/// </summary>
public class TimePickerHapticBehavior : Behavior<TimePicker>
{
    protected override void OnAttachedTo(TimePicker picker)
    {
        base.OnAttachedTo(picker);
        picker.Focused += OnPickerFocused;
        picker.PropertyChanged += OnTimeChanged;
    }

    protected override void OnDetachingFrom(TimePicker picker)
    {
        base.OnDetachingFrom(picker);
        picker.Focused -= OnPickerFocused;
        picker.PropertyChanged -= OnTimeChanged;
    }

    private void OnPickerFocused(object? sender, FocusEventArgs e)
    {
        if (e.IsFocused)
        {
            HapticHelper.PerformHaptic();
        }
    }

    private void OnTimeChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimePicker.Time))
        {
            HapticHelper.PerformStrongHaptic();
        }
    }
}
