using System.Collections.ObjectModel;
using MySqlConnector;
using ScottPlot;
using MauiColor = Microsoft.Maui.Graphics.Color;
using MauiColors = Microsoft.Maui.Graphics.Colors;
using MauiLabel = Microsoft.Maui.Controls.Label;
using SpColor = ScottPlot.Color;

namespace BULi_MAUI;

public partial class c1_HomePage : ContentPage
{
    public static string CurrentSessionUsername { get; set; } = string.Empty;
    public static string CurrentSessionPassword { get; set; } = string.Empty;
    public static string CurrentTeacherFirstName { get; set; } = string.Empty;

    private readonly ObservableCollection<StudentRecord> _recentStudents = new();
    private readonly List<Border> _trackedRecentBorders = new();
    private int[] _lastProgramCounts = [0, 0, 0, 0, 0];
    private bool _isPasswordVisible;

    private readonly ScottPlot.Plot _plot = new();
    private double _lastRenderW;
    private double _lastRenderH;

    public c1_HomePage()
    {
        InitializeComponent();
        recentStudentsView.ItemsSource = _recentStudents;

        overviewImage.SizeChanged += (_, _) =>
        {
            if (overviewImage.Width > 0 && overviewImage.Height > 0 &&
                (Math.Abs(overviewImage.Width - _lastRenderW) > 1 ||
                 Math.Abs(overviewImage.Height - _lastRenderH) > 1))
            {
                Dispatcher.Dispatch(() => UpdateMiniPlot(_lastProgramCounts));
            }
        };

        StylePlotDarkTheme();
        UpdateMiniPlot(_lastProgramCounts);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyAllThemes();
        await LoadTeacherInfoFromDbAsync();
        RefreshProfileVisuals();
        UpdateMiniPlot(_lastProgramCounts);
        await Task.Delay(120);
        await LoadOverviewDataAsync();
    }

    private void OnRecentCardLoaded(object? sender, EventArgs e)
    {
        if (sender is Border border)
        {
            if (!_trackedRecentBorders.Contains(border))
                _trackedRecentBorders.Add(border);

            StyleRecentBorder(border, SessionService.IsDarkMode);
        }
    }

    private static void StyleRecentBorder(Border border, bool isDark)
    {
        border.BackgroundColor = isDark ? MauiColor.FromArgb("#15263A") : MauiColor.FromArgb("#FFFFFF");
        border.Stroke = new SolidColorBrush(isDark ? MauiColor.FromArgb("#22364D") : MauiColor.FromArgb("#CBD5E1"));

        var primaryText = isDark ? MauiColors.White : MauiColor.FromArgb("#0F172A");
        var linkBlue = isDark ? MauiColor.FromArgb("#87CEEB") : MauiColor.FromArgb("#0284C7");

        if (border.Content is Grid grid)
        {
            foreach (var child in grid.Children)
            {
                if (child is VerticalStackLayout stack)
                {
                    int idx = 0;
                    foreach (var item in stack.Children)
                    {
                        if (item is MauiLabel lbl)
                        {
                            lbl.TextColor = idx == 0 ? primaryText : linkBlue;
                            idx++;
                        }
                    }
                }
            }
        }
    }

    private void ApplyAllThemes()
    {
        bool isDark = SessionService.IsDarkMode;
        ThemeService.ApplyTheme(this, btnThemeToggle);

        var panelBg = isDark ? MauiColor.FromArgb("#0D1B2A") : MauiColor.FromArgb("#F8FAFC");
        var cardBg = isDark ? MauiColor.FromArgb("#0D1B2A") : MauiColor.FromArgb("#FFFFFF");
        var altCardBg = isDark ? MauiColor.FromArgb("#15263A") : MauiColor.FromArgb("#F1F5F9");
        var strokeColor = isDark ? MauiColor.FromArgb("#22364D") : MauiColor.FromArgb("#CBD5E1");
        var primaryText = isDark ? MauiColors.White : MauiColor.FromArgb("#0F172A");
        var secondaryText = isDark ? MauiColor.FromArgb("#8FA3B8") : MauiColor.FromArgb("#475569");
        var linkBlue = isDark ? MauiColor.FromArgb("#87CEEB") : MauiColor.FromArgb("#0284C7");

        // Program cards & plot panel
        Border[] programCards = [cardBSIT, cardBSCS, cardBSMET, cardBSBIO, cardBSCHEM, cardPlot];
        foreach (var card in programCards)
        {
            card.BackgroundColor = cardBg;
            card.Stroke = new SolidColorBrush(strokeColor);
        }

        // Labels inside program cards & plot card
        foreach (var card in programCards)
        {
            if (card.Content is IVisualTreeElement content)
                StyleLabelsInside(content, primaryText, secondaryText, linkBlue);
        }

        // Recent student cards already loaded
        foreach (var recentBorder in _trackedRecentBorders.ToList())
        {
            if (recentBorder.Handler is null)
                _trackedRecentBorders.Remove(recentBorder);
            else
                StyleRecentBorder(recentBorder, isDark);
        }

        // Slide-out drawer
        ProfileDrawerPanel.BackgroundColor = panelBg;
        ProfileDrawerPanel.Stroke = new SolidColorBrush(strokeColor);

        btnViewProfile.BackgroundColor = altCardBg;
        btnViewProfile.BorderColor = strokeColor;
        btnViewProfile.TextColor = primaryText;

        btnChangePassword.BackgroundColor = altCardBg;
        btnChangePassword.BorderColor = strokeColor;
        btnChangePassword.TextColor = primaryText;

        btnThemeToggle.Text = isDark ? "Light Mode" : "Dark Mode";
        btnThemeToggle.BackgroundColor = altCardBg;
        btnThemeToggle.BorderColor = strokeColor;
        btnThemeToggle.TextColor = linkBlue;

        lblProfileUsername.TextColor = primaryText;
        lblProfilePassword.TextColor = isDark ? MauiColor.FromArgb("#DCE6F2") : primaryText;

        StylePlotDarkTheme();
    }

    private static void StyleLabelsInside(
        IVisualTreeElement node,
        MauiColor primaryText,
        MauiColor secondaryText,
        MauiColor linkBlue)
    {
        if (node is MauiLabel lbl)
        {
            string? hex = lbl.TextColor?.ToArgbHex();
            bool isAccent = hex is "#FFFFA500" or "#FFD97706" or "#FFFF6B6B" or "#FFDC2626"
                or "#FF4ADE80" or "#FF16A34A" or "#FFFACC15" or "#FFCA8A04" or "#FF0D1B2A";

            if (!isAccent)
            {
                if (hex is "#FF87CEEB" or "#FF0284C7")
                    lbl.TextColor = linkBlue;
                else if (hex is "#FF8FA3B8" or "#FFB0C4DE" or "#FF475569" or "#FF64748B" or "#FFDCE6F2")
                    lbl.TextColor = secondaryText;
                else
                    lbl.TextColor = primaryText;
            }
        }

        foreach (var child in node.GetVisualChildren())
        {
            StyleLabelsInside(child, primaryText, secondaryText, linkBlue);
        }
    }

    private static async Task LoadTeacherInfoFromDbAsync()
    {
        bool hasId = uint.TryParse(SessionService.UserId, out uint teacherId) && teacherId > 0;
        bool hasUser = !string.IsNullOrWhiteSpace(CurrentSessionUsername);

        if (!hasId && !hasUser)
            return;

        try
        {
            await using var conn = new MySqlConnection(SisReadService.ConnectionString);
            await conn.OpenAsync();

            string sql = hasId
                ? "SELECT username, first_name, last_name FROM teachers WHERE teacher_id = @id LIMIT 1;"
                : "SELECT username, first_name, last_name FROM teachers WHERE username = @user LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            if (hasId)
                cmd.Parameters.AddWithValue("@id", teacherId);
            else
                cmd.Parameters.AddWithValue("@user", CurrentSessionUsername.Trim());

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string uname = reader.IsDBNull(reader.GetOrdinal("username")) ? string.Empty : reader.GetString("username");
                string firstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? string.Empty : reader.GetString("first_name");
                string lastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? string.Empty : reader.GetString("last_name");

                if (!string.IsNullOrWhiteSpace(uname))
                    CurrentSessionUsername = uname;

                if (!string.IsNullOrWhiteSpace(firstName))
                    CurrentTeacherFirstName = firstName.Trim();

                string combined = $"{firstName} {lastName}".Trim();
                if (!string.IsNullOrWhiteSpace(combined))
                    SessionService.FullName = combined;
            }
        }
        catch
        {
            // Fallback handled in RefreshProfileVisuals
        }
    }

    private async void OnSeeAllRecordsClicked(object? sender, EventArgs e)
    {
        if (Shell.Current is not null)
            await Shell.Current.GoToAsync($"//{nameof(d1_Records)}");
        else
            await Navigation.PushAsync(new d1_Records());
    }

    private async void OnToggleProfileDrawerTapped(object? sender, TappedEventArgs e)
    {
        if (!ProfileDrawerOverlay.IsVisible)
        {
            ApplyAllThemes();
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

    private void RefreshProfileVisuals()
    {
        string greetingName = !string.IsNullOrWhiteSpace(CurrentTeacherFirstName)
            ? CurrentTeacherFirstName
            : (!string.IsNullOrWhiteSpace(SessionService.FullName)
                ? SessionService.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Admin"
                : "Admin");

        lblGreeting.Text = $"Hello, {greetingName}!";

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

        string username = !string.IsNullOrWhiteSpace(CurrentSessionUsername)
            ? CurrentSessionUsername
            : (!string.IsNullOrWhiteSpace(SessionService.Email)
                ? SessionService.Email
                : (!string.IsNullOrWhiteSpace(SessionService.FullName) ? SessionService.FullName : "Admin"));

        lblProfileUsername.Text = username;
        _isPasswordVisible = false;
        btnTogglePasswordVisibility.Text = "Show";
        lblProfilePassword.Text = !string.IsNullOrEmpty(CurrentSessionPassword)
            ? new string('•', Math.Max(8, CurrentSessionPassword.Length))
            : "••••••••";
    }

    private async void OnViewProfileClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();
        await LoadTeacherInfoFromDbAsync();
        RefreshProfileVisuals();
        ApplyAllThemes();
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

            string destPath = Path.Combine(FileSystem.AppDataDirectory, $"teacher_profile{ext}");

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
        if (string.IsNullOrEmpty(CurrentSessionPassword))
            CurrentSessionPassword = Preferences.Default.Get("TeacherSessionPassword", string.Empty);

        _isPasswordVisible = !_isPasswordVisible;
        btnTogglePasswordVisibility.Text = _isPasswordVisible ? "Hide" : "Show";

        if (_isPasswordVisible)
        {
            lblProfilePassword.Text = !string.IsNullOrEmpty(CurrentSessionPassword)
                ? CurrentSessionPassword
                : "(No cached password — use Change Password or re-login)";
        }
        else
        {
            lblProfilePassword.Text = new string('•', Math.Max(8, CurrentSessionPassword.Length));
        }
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
            if (result.Success)
            {
                CurrentSessionPassword = newPassword;
                RefreshProfileVisuals();
            }

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
        ApplyAllThemes();
        UpdateMiniPlot(_lastProgramCounts);
    }

    private async void OnLogOutClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();

        bool confirm = await DisplayAlertAsync("Log Out", "Are you sure you want to log out?", "Yes", "Cancel");
        if (!confirm)
            return;

        CurrentSessionUsername = string.Empty;
        CurrentSessionPassword = string.Empty;
        CurrentTeacherFirstName = string.Empty;
        SessionService.Logout();
        await Shell.Current.GoToAsync("//RolePage");
    }

    private void StylePlotDarkTheme()
    {
        bool isDark = SessionService.IsDarkMode;
        var bg = isDark ? SpColor.FromHex("#0D1B2A") : SpColor.FromHex("#FFFFFF");
        var fg = isDark ? SpColor.FromHex("#DCE6F2") : SpColor.FromHex("#0F172A");
        var grid = isDark ? SpColor.FromHex("#22364D") : SpColor.FromHex("#CBD5E1");

        _plot.FigureBackground.Color = bg;
        _plot.DataBackground.Color = bg;
        _plot.Axes.Color(fg);
        _plot.Grid.MajorLineColor = grid;

        _plot.Axes.Bottom.IsVisible = true;
        _plot.Axes.Bottom.MinimumSize = 44;
        _plot.Axes.Bottom.TickLabelStyle.IsVisible = true;
        _plot.Axes.Bottom.TickLabelStyle.ForeColor = fg;
        _plot.Axes.Bottom.TickLabelStyle.FontSize = 10;
        _plot.Axes.Bottom.TickLabelStyle.Rotation = 0;
        _plot.Axes.Bottom.MajorTickStyle.Length = 4;

        _plot.Axes.Left.IsVisible = true;
        _plot.Axes.Left.MinimumSize = 34;
        _plot.Axes.Left.TickLabelStyle.IsVisible = true;
        _plot.Axes.Left.TickLabelStyle.ForeColor = fg;
        _plot.Axes.Left.TickLabelStyle.FontSize = 10;
        _plot.Axes.Left.TickGenerator =
            new ScottPlot.TickGenerators.NumericAutomatic { IntegerTicksOnly = true };
    }

    private async Task LoadOverviewDataAsync()
    {
        try
        {
            var students = await SisReadService.GetStudentsAsync();

            int bsit = CountStudentsForProgram(students, "bsit", "information technology", "info tech");
            int bscs = CountStudentsForProgram(students, "bscs", "computer science", "comp sci");
            int bsmet = CountStudentsForProgram(students, "bsmet", "meteorology");
            int bsbio = CountStudentsForProgram(students, "bsbio", "biology");
            int bschem = CountStudentsForProgram(students, "bschem", "chemistry");

            _lastProgramCounts = [bsit, bscs, bsmet, bsbio, bschem];

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                lblCountBSIT.Text = bsit.ToString();
                lblCountBSCS.Text = bscs.ToString();
                lblCountBSMET.Text = bsmet.ToString();
                lblCountBSBIO.Text = bsbio.ToString();
                lblCountBSCHEM.Text = bschem.ToString();

                lblTotalStudents.Text = $"{students.Count} Total Students";

                _recentStudents.Clear();
                foreach (var student in students.TakeLast(4).Reverse())
                {
                    _recentStudents.Add(student);
                }

                ApplyAllThemes();
                UpdateMiniPlot(_lastProgramCounts);
            });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Overview Error", ex.Message, "OK");
        }
    }

    private static int CountStudentsForProgram(List<StudentRecord> students, params string[] keywords)
    {
        return students.Count(s =>
        {
            if (string.IsNullOrWhiteSpace(s.Program))
                return false;

            string p = s.Program.Trim().ToLowerInvariant();
            return keywords.Any(k => p == k || p.Contains(k, StringComparison.OrdinalIgnoreCase));
        });
    }

    private void UpdateMiniPlot(int[] valuesInt)
    {
        bool isDark = SessionService.IsDarkMode;
        var fg = isDark ? SpColor.FromHex("#DCE6F2") : SpColor.FromHex("#0F172A");

        _plot.Clear();
        _plot.Axes.Rules.Clear();
        StylePlotDarkTheme();

        string[] labels = ["BSIT", "BSCS", "BSMET", "BSBIO", "BSCHEM"];
        double[] positions = Enumerable.Range(0, labels.Length).Select(i => (double)i).ToArray();
        double[] values = valuesInt.Select(v => (double)v).ToArray();

        var bars = _plot.Add.Bars(positions, values);
        for (int i = 0; i < bars.Bars.Count; i++)
        {
            bars.Bars[i].FillColor = SpColor.FromHex("#FFA500");
            bars.Bars[i].LineWidth = 0;
            bars.Bars[i].Size = 0.55;
            bars.Bars[i].Label = values[i].ToString("0");
        }
        bars.ValueLabelStyle.ForeColor = fg;
        bars.ValueLabelStyle.FontSize = 11;
        bars.ValueLabelStyle.Bold = true;

        _plot.Axes.Bottom.SetTicks(positions, labels);
        _plot.Axes.Bottom.Label.Text = "Program";
        _plot.Axes.Bottom.Label.ForeColor = fg;
        _plot.Axes.Left.Label.Text = "Students";
        _plot.Axes.Left.Label.ForeColor = fg;

        double maxVal = values.Length > 0 ? values.Max() : 0;
        double topY = Math.Max(4, Math.Ceiling(maxVal * 1.25));

        _plot.Axes.Margins(bottom: 0.05, top: 0.1);
        _plot.Axes.SetLimitsX(-0.6, labels.Length - 0.4);
        _plot.Axes.SetLimitsY(0, topY);

        if (overviewImage.Width > 0 && overviewImage.Height > 0)
        {
            _lastRenderW = overviewImage.Width;
            _lastRenderH = overviewImage.Height;
            overviewImage.Source = PlotImageHelper.Render(_plot, _lastRenderW, _lastRenderH);
        }
    }
}
