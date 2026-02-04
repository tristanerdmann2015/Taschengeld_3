using Taschengeld_3.ViewModels;

namespace Taschengeld_3.Views;

public partial class CostPage : ContentPage
{
    private CostViewModel? _viewModel;

    public CostPage()
    {
        InitializeComponent();
        _viewModel = ServiceHelper.GetService<CostViewModel>();
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
                    await _viewModel.LoadData();
                    ViewModePicker.SelectedIndex = 1; // Monthly by default
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Fehler", ex.Message, "OK");
                }
            });
        }
    }

    private async void OnLoadCosts(object sender, EventArgs e)
    {
        if (_viewModel != null && ViewModePicker.SelectedIndex >= 0)
        {
            _viewModel.ViewMode = (CostViewModel.CostViewMode)ViewModePicker.SelectedIndex;
            await _viewModel.LoadCostSummaries();
        }
    }
}

