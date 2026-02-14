namespace Taschengeld_3.Services;

/// <summary>
/// Service fuer Dialoge, ersetzt das veraltete Application.MainPage.DisplayAlert
/// </summary>
public static class DialogService
{
    /// <summary>
    /// Zeigt einen Alert-Dialog an
    /// </summary>
    public static async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        try
        {
            var page = GetCurrentPage();
            if (page != null)
            {
                await page.DisplayAlertAsync(title, message, cancel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DialogService.DisplayAlertAsync Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Zeigt einen Bestaetigungsdialog an
    /// </summary>
    public static async Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
    {
        try
        {
            var page = GetCurrentPage();
            if (page != null)
            {
                return await page.DisplayAlertAsync(title, message, accept, cancel);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DialogService.DisplayConfirmAsync Error: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// Holt die aktuelle Seite aus dem ersten Fenster
    /// </summary>
    private static Page? GetCurrentPage()
    {
        try
        {
            if (Application.Current?.Windows.Count > 0)
            {
                return Application.Current.Windows[0].Page;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DialogService.GetCurrentPage Error: {ex.Message}");
        }
        return null;
    }
}
