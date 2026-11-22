using ScottPlot;

namespace BULi_MAUI;

public partial class e1_Graphs : ContentPage
{
    private bool _showingProgramChart = true;
    private Dictionary<string, int> _programCounts = new();
    private Dictionary<string, int> _yearCounts = new();

    private readonly ScottPlot.Plot _plot = new();
    private double _lastRenderW;
    private double _lastRenderH;

    public e1_Graphs()
    {
        InitializeComponent();

        analyticsImage.SizeChanged += (_, _) =>
        {
            if (analyticsImage.Width > 0 && analyticsImage.Height > 0 &&
                (Math.Abs(analyticsImage.Width - _lastRenderW) > 1 ||
                 Math.Abs(analyticsImage.Height - _lastRenderH) > 1))
            {
                Dispatcher.Dispatch(RenderCurrentChart);
            }
        };

        ApplyGraphsTheme();
        RenderCurrentChart();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyGraphsTheme();
        RenderCurrentChart();
        await Task.Delay(120);
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

        _plot.FigureBackground.Color = bg;
        _plot.DataBackground.Color = bg;
        _plot.Axes.Color(fg);
        _plot.Grid.MajorLineColor = grid;

        _plot.Axes.Bottom.IsVisible = true;
        _plot.Axes.Bottom.MinimumSize = 52;
        _plot.Axes.Bottom.TickLabelStyle.IsVisible = true;
        _plot.Axes.Bottom.TickLabelStyle.ForeColor = fg;
        _plot.Axes.Bottom.TickLabelStyle.FontSize = 11;
        _plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
        _plot.Axes.Bottom.MajorTickStyle.Length = 4;

        _plot.Axes.Left.IsVisible = true;
        _plot.Axes.Left.MinimumSize = 38;
        _plot.Axes.Left.TickLabelStyle.IsVisible = true;
        _plot.Axes.Left.TickLabelStyle.ForeColor = fg;
        _plot.Axes.Left.TickLabelStyle.FontSize = 11;
        _plot.Axes.Left.TickGenerator =
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
        string xAxisTitle;

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
            xAxisTitle = "Program";
        }
        else
        {
            labels = ["1st Year", "2nd Year", "3rd Year", "4th Year", "5th Year", "6th Year"];
            values = labels
                .Select(yr => (double)(_yearCounts.TryGetValue(yr, out int c) ? c : 0))
                .ToArray();
            barHex = "#87CEEB";
            xAxisTitle = "Year Level";
        }

        bool isDark = SessionService.IsDarkMode;
        var fg = isDark ? ScottPlot.Color.FromHex("#DCE6F2") : ScottPlot.Color.FromHex("#0F172A");

        _plot.Clear();
        _plot.Axes.Rules.Clear();
        StylePlotTheme();

        double[] positions = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();
        var bars = _plot.Add.Bars(positions, values);
        for (int i = 0; i < bars.Bars.Count; i++)
        {
            bars.Bars[i].FillColor = ScottPlot.Color.FromHex(barHex);
            bars.Bars[i].LineWidth = 0;
            bars.Bars[i].Size = 0.55;
            bars.Bars[i].Label = values[i].ToString("0");
        }
        bars.ValueLabelStyle.ForeColor = fg;
        bars.ValueLabelStyle.FontSize = 12;
        bars.ValueLabelStyle.Bold = true;

        _plot.Axes.Bottom.SetTicks(positions, labels);
        _plot.Axes.Bottom.Label.Text = xAxisTitle;
        _plot.Axes.Bottom.Label.ForeColor = fg;
        _plot.Axes.Left.Label.Text = "Students";
        _plot.Axes.Left.Label.ForeColor = fg;

        double maxVal = values.Length > 0 ? values.Max() : 0;
        double topY = Math.Max(4, Math.Ceiling(maxVal * 1.25));

        _plot.Axes.Margins(bottom: 0.05, top: 0.1);
        _plot.Axes.SetLimitsX(-0.6, labels.Length - 0.4);
        _plot.Axes.SetLimitsY(0, topY);

        if (analyticsImage.Width > 0 && analyticsImage.Height > 0)
        {
            _lastRenderW = analyticsImage.Width;
            _lastRenderH = analyticsImage.Height;
            analyticsImage.Source = PlotImageHelper.Render(_plot, _lastRenderW, _lastRenderH);
        }
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

            _plot.ScaleFactor = 1f;
            _plot.SavePng(filePath, 900, 520);

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
