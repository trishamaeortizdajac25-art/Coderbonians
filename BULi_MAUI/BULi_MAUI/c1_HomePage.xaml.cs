using System.Collections.ObjectModel;
using ScottPlot;

namespace BULi_MAUI;

public partial class c1_HomePage : ContentPage
{
    private readonly ObservableCollection<StudentRecord> _recentStudents = new();
    private int[] _lastProgramCounts = [0, 0, 0, 0, 0];
    

    public c1_HomePage()
    {
        InitializeComponent();
        overviewPlot.UserInputProcessor.IsEnabled = false;
        recentStudentsView.ItemsSource = _recentStudents;

        overviewPlot.SizeChanged += (_, _) =>
        {
            if (overviewPlot.Width > 0 && overviewPlot.Height > 0)
            {
                StylePlotDarkTheme();
                overviewPlot.Refresh();
            }
        };

        StylePlotDarkTheme();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.ApplyTheme(this, btnThemeToggle);
        RefreshProfileVisuals();
        await Task.Delay(100);
        await LoadOverviewDataAsync();
    }

    // --- SEE ALL NAVIGATION ---

    private async void OnSeeAllRecordsClicked(object? sender, EventArgs e)
    {
        if (Shell.Current is not null)
            await Shell.Current.GoToAsync($"//{nameof(d1_Records)}");
        else
            await Navigation.PushAsync(new d1_Records());
    }

    // --- RIGHT SLIDE-OUT PROFILE DRAWER ---

    private async void OnToggleProfileDrawerTapped(object? sender, TappedEventArgs e)
    {
        if (!ProfileDrawerOverlay.IsVisible)
        {
            ProfileDrawerOverlay.IsVisible = true;
            ProfileDrawerPanel.TranslationX = 280;
            await ProfileDrawerPanel.TranslateToAsync(0, 0, 220, Easing.CubicOut);
        }
        else
        {
            await CloseProfileDrawerAsync();
        }
    }

    private async void OnCloseProfileDrawerTapped(object? sender, TappedEventArgs e)
    {
        await CloseProfileDrawerAsync();
    }

    private async Task CloseProfileDrawerAsync()
    {
        if (!ProfileDrawerOverlay.IsVisible)
            return;

        await ProfileDrawerPanel.TranslateToAsync(280, 0, 180, Easing.CubicIn);
        ProfileDrawerOverlay.IsVisible = false;
        ProfileDrawerPanel.TranslationX = 0;
    }

    // --- 4 DRAWER BUTTONS ---

    private string _actualPassword = "••••••••";
    private bool _isPasswordVisible;

    private void RefreshProfileVisuals()
    {
        string initials = SessionService.GetInitials();
        lblAdminInitials.Text = initials;
        lblModalProfileInitials.Text = initials;

        if (!string.IsNullOrWhiteSpace(SessionService.PhotoPath) && File.Exists(SessionService.PhotoPath))
        {
            imgAdminProfile.Source = ImageSource.FromFile(SessionService.PhotoPath);
            imgAdminProfile.IsVisible = true;
            lblAdminInitials.IsVisible = false;

            imgModalProfilePhoto.Source = ImageSource.FromFile(SessionService.PhotoPath);
            imgModalProfilePhoto.IsVisible = true;
            lblModalProfileInitials.IsVisible = false;
        }
        else
        {
            imgAdminProfile.Source = null;
            imgAdminProfile.IsVisible = false;
            lblAdminInitials.IsVisible = true;

            imgModalProfilePhoto.Source = null;
            imgModalProfilePhoto.IsVisible = false;
            lblModalProfileInitials.IsVisible = true;
        }

        string username = !string.IsNullOrWhiteSpace(SessionService.Email)
            ? SessionService.Email
            : (!string.IsNullOrWhiteSpace(SessionService.FullName) ? SessionService.FullName : "Admin");

        lblProfileUsername.Text = username;
        _isPasswordVisible = false;
        btnTogglePasswordVisibility.Text = "Show";
        lblProfilePassword.Text = new string('•', Math.Max(8, _actualPassword.Length));
    }

    private async void OnViewProfileClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();
        RefreshProfileVisuals();
        ViewProfileOverlay.IsVisible = true;
    }

    private void OnCloseViewProfileClicked(object? sender, EventArgs e)
    {
        ViewProfileOverlay.IsVisible = false;
    }

private async void OnAddProfileImageClicked(object? sender, EventArgs e)
    {
        try
        {
#pragma warning disable CS0618
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select Profile Picture"
            });
#pragma warning restore CS0618

            if (result is null)
                return;

            string ext = Path.GetExtension(result.FileName);
            if (string.IsNullOrWhiteSpace(ext))
                ext = ".jpg";

            string destPath = Path.Combine(FileSystem.AppDataDirectory, $"student_profile{ext}");

            await using var sourceStream = await result.OpenReadAsync();
            await using var destStream = File.Create(destPath);
            await sourceStream.CopyToAsync(destStream);

            SessionService.PhotoPath = destPath;
            RefreshProfileVisuals();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Photo Error", ex.Message, "OK");
        }
    }

    private void OnRemoveProfileImageClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(SessionService.PhotoPath) && File.Exists(SessionService.PhotoPath))
            {
                File.Delete(SessionService.PhotoPath);
            }
        }
        catch
        {
            // Ignore file lock issues on removal
        }

        SessionService.PhotoPath = string.Empty;
        RefreshProfileVisuals();
    }

    private void OnTogglePasswordVisibilityClicked(object? sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        btnTogglePasswordVisibility.Text = _isPasswordVisible ? "Hide" : "Show";
        lblProfilePassword.Text = _isPasswordVisible
            ? _actualPassword
            : new string('•', Math.Max(8, _actualPassword.Length));
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();

        string? oldPassword = await DisplayPromptAsync(
            "Change Password",
            "Enter your current password:",
            accept: "Next",
            cancel: "Cancel",
            placeholder: "Current password");

        if (string.IsNullOrWhiteSpace(oldPassword))
            return;

        string? newPassword = await DisplayPromptAsync(
            "Change Password",
            "Enter your new password:",
            accept: "Save",
            cancel: "Cancel",
            placeholder: "New password");

        if (string.IsNullOrWhiteSpace(newPassword))
            return;

        try
        {
            if (!uint.TryParse(SessionService.UserId, out uint teacherId) || teacherId == 0)
            {
                await DisplayAlertAsync("Error", "Teacher session not found. Please sign in again.", "OK");
                return;
            }

            var result = await SisWriteService.ChangeTeacherPasswordAsync(teacherId, oldPassword, newPassword);
            await DisplayAlertAsync(result.Success ? "Success" : "Failed", result.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Database Error", ex.Message, "OK");
        }
    }

    private void OnToggleThemeClicked(object? sender, EventArgs e)
    {
        SessionService.IsDarkMode = !SessionService.IsDarkMode;
        ThemeService.ApplyTheme(this, btnThemeToggle);
    }

    private async void OnLogOutClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();

        bool confirm = await DisplayAlertAsync("Log Out", "Are you sure you want to log out?", "Yes", "Cancel");
        if (!confirm)
            return;

        SessionService.Logout();
        await Shell.Current.GoToAsync("//RolePage");
    }

    // --- OVERVIEW DATA & MINI PLOT ---
    // --- OVERVIEW DATA & MINI PLOT ---

    private void StylePlotDarkTheme()
    {
        bool isDark = SessionService.IsDarkMode;
        var bg = isDark ? ScottPlot.Color.FromHex("#0D1B2A") : ScottPlot.Color.FromHex("#FFFFFF");
        var fg = isDark ? ScottPlot.Color.FromHex("#DCE6F2") : ScottPlot.Color.FromHex("#0F172A");
        var grid = isDark ? ScottPlot.Color.FromHex("#22364D") : ScottPlot.Color.FromHex("#CBD5E1");

        overviewPlot.BackgroundColor = isDark
            ? Microsoft.Maui.Graphics.Color.FromArgb("#0D1B2A")
            : Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF");

        overviewPlot.Plot.FigureBackground.Color = bg;
        overviewPlot.Plot.DataBackground.Color = bg;
        overviewPlot.Plot.Axes.Color(fg);
        overviewPlot.Plot.Grid.MajorLineColor = grid;

        overviewPlot.Plot.Axes.Bottom.MinimumSize = 28;
        overviewPlot.Plot.Axes.Bottom.TickLabelStyle.ForeColor = fg;
        overviewPlot.Plot.Axes.Bottom.TickLabelStyle.FontSize = 10;

        overviewPlot.Plot.Axes.Left.TickLabelStyle.ForeColor = fg;
        overviewPlot.Plot.Axes.Left.TickLabelStyle.FontSize = 10;
        overviewPlot.Plot.Axes.Left.TickGenerator =
            new ScottPlot.TickGenerators.NumericAutomatic { IntegerTicksOnly = true };
    }
    private async Task LoadOverviewDataAsync()
    {
        try
        {
            var countsTask = SisReadService.GetProgramCountsAsync();
            var studentsTask = SisReadService.GetStudentsAsync();

            await Task.WhenAll(countsTask, studentsTask);

            var counts = countsTask.Result;
            var students = studentsTask.Result;

            int bsit = GetProgramCount(counts, "BS Information Technology");
            int bscs = GetProgramCount(counts, "BS Computer Science");
            int bsmet = GetProgramCount(counts, "BS Meteorology");
            int bsbio = GetProgramCount(counts, "BS Biology");
            int bschem = GetProgramCount(counts, "BS Chemistry");

            _lastProgramCounts = [bsit, bscs, bsmet, bsbio, bschem];

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                lblCountBSIT.Text = bsit.ToString();
                lblCountBSCS.Text = bscs.ToString();
                lblCountBSMET.Text = bsmet.ToString();
                lblCountBSBIO.Text = bsbio.ToString();
                lblCountBSCHEM.Text = bschem.ToString();

                int totalFromCounts = counts.Values.Sum();
                lblTotalStudents.Text = $"{(totalFromCounts > 0 ? totalFromCounts : students.Count)} Total Students";

                _recentStudents.Clear();
                foreach (var student in students.OrderByDescending(s => s.StudentId).Take(4))
                {
                    _recentStudents.Add(student);
                }

                UpdateMiniPlot(_lastProgramCounts);
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Overview Error", ex.Message, "OK");
        }
    }

    private void UpdateMiniPlot(int[] valuesInt)
    {
        overviewPlot.Plot.Clear();
        overviewPlot.Plot.Axes.Rules.Clear();
        StylePlotDarkTheme();

        string[] labels = ["BSIT", "BSCS", "BSMET", "BSBIO", "BSCHEM"];
        double[] positions = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();
        double[] values = valuesInt.Select(v => (double)v).ToArray();

        var bars = overviewPlot.Plot.Add.Bars(positions, values);
        foreach (var bar in bars.Bars)
        {
            bar.FillColor = ScottPlot.Color.FromHex("#FFA500");
            bar.Size = 0.55;
        }

        var ticks = positions
            .Select((pos, i) => new Tick(pos, labels[i]))
            .ToArray();
        overviewPlot.Plot.Axes.Bottom.TickGenerator =
            new ScottPlot.TickGenerators.NumericManual(ticks);

        double maxVal = values.Length > 0 ? values.Max() : 0;
        double topY = Math.Max(4, Math.Ceiling(maxVal * 1.25));
        double minX = -0.6;
        double maxX = labels.Length - 0.4;

        overviewPlot.Plot.Axes.SetLimits(minX, maxX, 0, topY);
        overviewPlot.Plot.Axes.Rules.Add(
            new ScottPlot.AxisRules.LockedHorizontal(overviewPlot.Plot.Axes.Bottom, minX, maxX));
        overviewPlot.Plot.Axes.Rules.Add(
            new ScottPlot.AxisRules.LockedVertical(overviewPlot.Plot.Axes.Left, 0, topY));

        overviewPlot.Refresh();
    }

    private static int GetProgramCount(Dictionary<string, int> counts, string programName) =>
        counts.TryGetValue(programName, out int total) ? total : 0;
}