using DocumentFormat.OpenXml.Packaging;
using ScottPlot;

namespace BULi_MAUI;

public partial class e1_Graphs : ContentPage
{
    private bool _showingProgramChart = true;
    private Dictionary<string, int> _programCounts = new();
    private Dictionary<string, int> _yearCounts = new();

    public e1_Graphs()
    {
        InitializeComponent();
        analyticsPlot.UserInputProcessor.IsEnabled = false;

        analyticsPlot.SizeChanged += (_, _) =>
        {
            if (analyticsPlot.Width > 0 && analyticsPlot.Height > 0)
                RenderCurrentChart();
        };

        ApplyGraphsTheme();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyGraphsTheme();
        await Task.Delay(100);
        await LoadAnalyticsDataAsync();
    }

    private void ApplyGraphsTheme()
    {
        bool isDark = SessionService.IsDarkMode;
        ThemeService.ApplyTheme(this);

        var cardBg = isDark ? Microsoft.Maui.Graphics.Color.FromArgb("#0D1B2A") : Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF");
        var btnBg = isDark ? Microsoft.Maui.Graphics.Color.FromArgb("#15263A") : Microsoft.Maui.Graphics.Color.FromArgb("#F1F5F9");
        var stroke = isDark ? Microsoft.Maui.Graphics.Color.FromArgb("#22364D") : Microsoft.Maui.Graphics.Color.FromArgb("#CBD5E1");
        var primaryText = isDark ? Microsoft.Maui.Graphics.Colors.White : Microsoft.Maui.Graphics.Color.FromArgb("#0F172A");
        var linkBlue = isDark ? Microsoft.Maui.Graphics.Color.FromArgb("#87CEEB") : Microsoft.Maui.Graphics.Color.FromArgb("#0284C7");

        lblAnalyticsTitle.TextColor = primaryText;
        lblChartTitle.TextColor = primaryText;

        ChartCard.BackgroundColor = cardBg;
        ChartCard.Stroke = new SolidColorBrush(stroke);

        if (_showingProgramChart)
        {
            btnProgramChart.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFA500");
            btnProgramChart.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D1B2A");

            btnYearChart.BackgroundColor = btnBg;
            btnYearChart.TextColor = primaryText;
            btnYearChart.BorderColor = stroke;
        }
        else
        {
            btnYearChart.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFA500");
            btnYearChart.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#0D1B2A");

            btnProgramChart.BackgroundColor = btnBg;
            btnProgramChart.TextColor = primaryText;
            btnProgramChart.BorderColor = stroke;
        }

        btnRefreshChart.BackgroundColor = btnBg;
        btnRefreshChart.TextColor = linkBlue;
        btnRefreshChart.BorderColor = stroke;

        StylePlotTheme();
    }

    private void StylePlotTheme()
    {
        bool isDark = SessionService.IsDarkMode;
        var bg = isDark ? ScottPlot.Color.FromHex("#0D1B2A") : ScottPlot.Color.FromHex("#FFFFFF");
        var fg = isDark ? ScottPlot.Color.FromHex("#DCE6F2") : ScottPlot.Color.FromHex("#0F172A");
        var grid = isDark ? ScottPlot.Color.FromHex("#22364D") : ScottPlot.Color.FromHex("#CBD5E1");

        analyticsPlot.BackgroundColor = isDark
            ? Microsoft.Maui.Graphics.Color.FromArgb("#0D1B2A")
            : Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF");

        analyticsPlot.Plot.FigureBackground.Color = bg;
        analyticsPlot.Plot.DataBackground.Color = bg;
        analyticsPlot.Plot.Axes.Color(fg);
        analyticsPlot.Plot.Grid.MajorLineColor = grid;

        analyticsPlot.Plot.Axes.Bottom.MinimumSize = 32;
        analyticsPlot.Plot.Axes.Bottom.TickLabelStyle.ForeColor = fg;
        analyticsPlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 11;

        analyticsPlot.Plot.Axes.Left.TickLabelStyle.ForeColor = fg;
        analyticsPlot.Plot.Axes.Left.TickLabelStyle.FontSize = 11;
        analyticsPlot.Plot.Axes.Left.TickGenerator =
            new ScottPlot.TickGenerators.NumericAutomatic { IntegerTicksOnly = true };
    }

    private async Task LoadAnalyticsDataAsync()
    {
        try
        {
            var progTask = SisReadService.GetProgramCountsAsync();
            var yearTask = SisReadService.GetYearLevelCountsAsync();
            await Task.WhenAll(progTask, yearTask);

            _programCounts = progTask.Result;
            _yearCounts = yearTask.Result.ToDictionary(x => x.YearLevel, x => x.Total);

            await MainThread.InvokeOnMainThreadAsync(RenderCurrentChart);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Analytics Error", ex.Message, "OK");
        }
    }

    private void RenderCurrentChart()
    {
        string[] labels;
        double[] values;
        string barHex;

        if (_showingProgramChart)
        {
            labels = ["BSIT", "BSCS", "BSMET", "BSBIO", "BSCHEM"];
            string[] fullNames =
            [
                "BS Information Technology",
                "BS Computer Science",
                "BS Meteorology",
                "BS Biology",
                "BS Chemistry"
            ];
            values = fullNames
                .Select(name => (double)(_programCounts.TryGetValue(name, out int c) ? c : 0))
                .ToArray();
            barHex = "#FFA500";
        }
        else
        {
            labels = ["1st Year", "2nd Year", "3rd Year", "4th Year", "5th Year", "6th Year"];
            values = labels
                .Select(yr => (double)(_yearCounts.TryGetValue(yr, out int c) ? c : 0))
                .ToArray();
            barHex = "#87CEEB";
        }

        analyticsPlot.Plot.Clear();
        analyticsPlot.Plot.Axes.Rules.Clear();
        StylePlotTheme();

        double[] positions = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();
        var bars = analyticsPlot.Plot.Add.Bars(positions, values);
        foreach (var bar in bars.Bars)
        {
            bar.FillColor = ScottPlot.Color.FromHex(barHex);
            bar.Size = 0.55;
        }

        var ticks = positions.Select((pos, i) => new Tick(pos, labels[i])).ToArray();
        analyticsPlot.Plot.Axes.Bottom.TickGenerator =
            new ScottPlot.TickGenerators.NumericManual(ticks);

        double maxVal = values.Length > 0 ? values.Max() : 0;
        double topY = Math.Max(4, Math.Ceiling(maxVal * 1.25));
        double minX = -0.6;
        double maxX = labels.Length - 0.4;

        analyticsPlot.Plot.Axes.SetLimits(minX, maxX, 0, topY);
        analyticsPlot.Plot.Axes.Rules.Add(
            new ScottPlot.AxisRules.LockedHorizontal(analyticsPlot.Plot.Axes.Bottom, minX, maxX));
        analyticsPlot.Plot.Axes.Rules.Add(
            new ScottPlot.AxisRules.LockedVertical(analyticsPlot.Plot.Axes.Left, 0, topY));

        analyticsPlot.Refresh();
    }

    private void OnShowProgramChartClicked(object? sender, EventArgs e)
    {
        _showingProgramChart = true;
        lblChartTitle.Text = "Enrolled Students per Program Distribution";
        ApplyGraphsTheme();
        RenderCurrentChart();
    }

    private void OnShowYearChartClicked(object? sender, EventArgs e)
    {
        _showingProgramChart = false;
        lblChartTitle.Text = "What Year They're In";
        ApplyGraphsTheme();
        RenderCurrentChart();
    }

    private async void OnRefreshChartClicked(object? sender, EventArgs e)
    {
        btnRefreshChart.IsEnabled = false;
        try
        {
            await LoadAnalyticsDataAsync();
        }
        finally
        {
            btnRefreshChart.IsEnabled = true;
        }
    }

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        try
        {
            string chartTag = _showingProgramChart ? "program_distribution" : "year_level_distribution";
            string fileName = $"BULi_{chartTag}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            analyticsPlot.Plot.SavePng(filePath, 900, 520);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export Analytics Chart",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Error", ex.Message, "OK");
        }
    }
}