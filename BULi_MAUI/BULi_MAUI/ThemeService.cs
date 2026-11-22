namespace BULi_MAUI;

public static class ThemeService
{
    private static readonly Color DarkBgTop = Color.FromArgb("#050B14");
    private static readonly Color DarkBgMid = Color.FromArgb("#0D1B2A");
    private static readonly Color DarkBgBot = Color.FromArgb("#3E2723");

    private static readonly Color LightBgTop = Color.FromArgb("#FFFFFF");
    private static readonly Color LightBgMid = Color.FromArgb("#F1F5F9");
    private static readonly Color LightBgBot = Color.FromArgb("#E6D5C3");

    private static readonly Color DarkCard = Color.FromArgb("#0D1B2A");
    private static readonly Color DarkCardAlt = Color.FromArgb("#15263A");
    private static readonly Color DarkStroke = Color.FromArgb("#22364D");

    private static readonly Color LightCard = Color.FromArgb("#FFFFFF");
    private static readonly Color LightCardAlt = Color.FromArgb("#F8FAFC");
    private static readonly Color LightStroke = Color.FromArgb("#CBD5E1");

    private static readonly Color LightPrimaryText = Color.FromArgb("#0F172A");
    private static readonly Color LightSecondaryText = Color.FromArgb("#475569");
    private static readonly Color DarkSecondaryText = Color.FromArgb("#8FA3B8");

    public static void ApplyTheme(ContentPage page, Button? themeToggleButton = null)
    {
        bool isDark = SessionService.IsDarkMode;

        if (themeToggleButton is not null)
        {
            themeToggleButton.Text = isDark ? "Light Mode" : "Dark Mode";
        }

        page.Background = isDark
            ? new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                [
                    new GradientStop(DarkBgTop, 0.0f),
                    new GradientStop(DarkBgMid, 0.5f),
                    new GradientStop(DarkBgBot, 1.0f)
                ]
            }
            : new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                [
                    new GradientStop(LightBgTop, 0.0f),
                    new GradientStop(LightBgMid, 0.55f),
                    new GradientStop(LightBgBot, 1.0f)
                ]
            };

        if (Shell.Current is Shell shell)
        {
            Shell.SetTabBarBackgroundColor(shell, isDark ? DarkBgTop : LightCard);
            Shell.SetTabBarForegroundColor(shell, isDark ? Color.FromArgb("#FFA500") : Color.FromArgb("#D97706"));
            Shell.SetTabBarUnselectedColor(shell, isDark ? Color.FromArgb("#80AAB0") : Color.FromArgb("#64748B"));
            Shell.SetTabBarTitleColor(shell, isDark ? Color.FromArgb("#FFA500") : Color.FromArgb("#D97706"));
        }

        if (page.Content is not null)
        {
            ApplyToElement(page.Content, isDark);
        }
    }

    private static bool IsAccentColor(Color? c)
    {
        if (c is null) return false;
        string hex = c.ToArgbHex();
        return hex is "#FFFFA500" or "#FFD97706"
            or "#FFFF6B6B" or "#FFDC2626"
            or "#FF4ADE80" or "#FF16A34A"
            or "#FFFACC15" or "#FFCA8A04"
            or "#FF0D1B2A";
    }

    private static bool IsSecondaryColor(Color? c)
    {
        if (c is null) return false;
        string hex = c.ToArgbHex();
        return hex is "#FF8FA3B8" or "#FFB0C4DE" or "#FF475569" or "#FF334155" or "#FF64748B";
    }

    private static bool IsLinkBlue(Color? c)
    {
        if (c is null) return false;
        string hex = c.ToArgbHex();
        return hex is "#FF87CEEB" or "#FF0284C7";
    }

    public static void ApplyToElement(object? node, bool isDark)
    {
        if (node is null) return;

        if (node is Element mauiElement)
        {
            mauiElement.ChildAdded -= OnDynamicChildAdded;
            mauiElement.ChildAdded += OnDynamicChildAdded;
        }

        if (node is Border border)
        {
            string? bgHex = border.BackgroundColor?.ToArgbHex();
            if (isDark)
            {
                if (bgHex is "#FFFFFFFF" or "#FFF8FAFC" or "#FFF1F5F9")
                    border.BackgroundColor = DarkCard;
                if (border.Stroke is SolidColorBrush scb && scb.Color.ToArgbHex() == "#FFCBD5E1")
                    border.Stroke = new SolidColorBrush(DarkStroke);
            }
            else
            {
                if (bgHex is "#FF0D1B2A")
                {
                    border.BackgroundColor = LightCard;
                    border.Stroke = new SolidColorBrush(LightStroke);
                }
                else if (bgHex is "#FF15263A")
                {
                    border.BackgroundColor = LightCardAlt;
                    border.Stroke = new SolidColorBrush(LightStroke);
                }
                else if (border.Stroke is SolidColorBrush scb && scb.Color.ToArgbHex() == "#FF22364D")
                {
                    border.Stroke = new SolidColorBrush(LightStroke);
                }
            }

            if (border.Content is not null)
                ApplyToElement(border.Content, isDark);
        }
        else if (node is ContentView cv && cv.Content is not null)
        {
            ApplyToElement(cv.Content, isDark);
        }
        else if (node is ScrollView sv && sv.Content is not null)
        {
            ApplyToElement(sv.Content, isDark);
        }
        else if (node is Label label)
        {
            if (!IsAccentColor(label.TextColor))
            {
                if (IsLinkBlue(label.TextColor))
                    label.TextColor = isDark ? Color.FromArgb("#87CEEB") : Color.FromArgb("#0284C7");
                else if (IsSecondaryColor(label.TextColor))
                    label.TextColor = isDark ? DarkSecondaryText : LightSecondaryText;
                else
                    label.TextColor = isDark ? Colors.White : LightPrimaryText;
            }
        }
        else if (node is Entry entry)
        {
            entry.TextColor = isDark ? Colors.White : LightPrimaryText;
            entry.BackgroundColor = isDark ? DarkCardAlt : Color.FromArgb("#F1F5F9");
            entry.PlaceholderColor = isDark ? Color.FromArgb("#6C8299") : Color.FromArgb("#64748B");
        }
        else if (node is DatePicker datePicker)
        {
            datePicker.TextColor = isDark ? Colors.White : LightPrimaryText;
            datePicker.BackgroundColor = isDark ? DarkCardAlt : Color.FromArgb("#F1F5F9");
        }
        else if (node is Picker picker)
        {
            picker.TextColor = isDark ? Colors.White : LightPrimaryText;
            picker.TitleColor = isDark ? DarkSecondaryText : LightSecondaryText;
            picker.BackgroundColor = isDark ? DarkCardAlt : Color.FromArgb("#F1F5F9");
        }
        else if (node is Button btn)
        {
            string? bgHex = btn.BackgroundColor?.ToArgbHex();
            bool isLogoutBtn = bgHex == "#FF3A151B";

            if (!isLogoutBtn)
            {
                if (isDark)
                {
                    if (bgHex is "#FFF1F5F9" or "#FFE2E8F0" or "#FFFFFFFF" or "#FFF8FAFC")
                    {
                        btn.BackgroundColor = DarkCardAlt;
                        btn.BorderColor = DarkStroke;
                        if (!IsAccentColor(btn.TextColor))
                            btn.TextColor = IsLinkBlue(btn.TextColor) ? Color.FromArgb("#87CEEB") : Colors.White;
                    }
                }
                else
                {
                    if (bgHex is "#FF15263A" or "#FF0D1B2A")
                    {
                        btn.BackgroundColor = Color.FromArgb("#F1F5F9");
                        btn.BorderColor = LightStroke;
                        if (!IsAccentColor(btn.TextColor))
                            btn.TextColor = IsLinkBlue(btn.TextColor) ? Color.FromArgb("#0284C7") : LightPrimaryText;
                    }
                }
            }
        }

        if (node is IVisualTreeElement vte)
        {
            foreach (var child in vte.GetVisualChildren())
            {
                ApplyToElement(child, isDark);
            }
        }
    }

    private static void OnDynamicChildAdded(object? sender, ElementEventArgs e)
    {
        ApplyToElement(e.Element, SessionService.IsDarkMode);
    }
}