using ScottPlot;

namespace BULi_MAUI;

public partial class e1_Graphs : ContentPage
{
    public e1_Graphs()
    {
        InitializeComponent();
        analyticsPlot.UserInputProcessor.IsEnabled = false;
        StylePlotDarkTheme();
    }

    private void StylePlotDarkTheme()
    {
        var bg = ScottPlot.Color.FromHex("#0D1B2A");
        var fg = ScottPlot.Color.FromHex("#DCE6F2");
        var grid = ScottPlot.Color.FromHex("#22364D");

        analyticsPlot.Plot.FigureBackground.Color = bg;
        analyticsPlot.Plot.DataBackground.Color = bg;
        analyticsPlot.Plot.Axes.Color(fg);
        analyticsPlot.Plot.Grid.MajorLineColor = grid;
    }

    private void OnShowProgramChartClicked(object? sender, EventArgs e)
    {
        lblChartTitle.Text = "Enrolled Students per Program Distribution";
        btnProgramChart.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFA500");
        btnProgramChart.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D1B2A");

        btnYearChart.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#15263A");
        btnYearChart.TextColor = Microsoft.Maui.Graphics.Colors.White;

        analyticsPlot.Refresh();
    }

    private void OnShowYearChartClicked(object? sender, EventArgs e)
    {
        lblChartTitle.Text = "What Year They're In";
        btnYearChart.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFA500");
        btnYearChart.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D1B2A");

        btnProgramChart.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#15263A");
        btnProgramChart.TextColor = Microsoft.Maui.Graphics.Colors.White;

        analyticsPlot.Refresh();
    }

    private void OnRefreshChartClicked(object? sender, EventArgs e)
    {
        // TODO: Hook up data refresh when ready
        analyticsPlot.Refresh();
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        // TODO: Hook up chart export when ready
        await DisplayAlertAsync("Export", "Export button ready.", "OK");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.ApplyTheme(this);
    }
}