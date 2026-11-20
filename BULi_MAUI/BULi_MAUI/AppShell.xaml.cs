namespace BULi_MAUI;

public partial class AppShell : Shell
{
    private bool _initialRouteChecked;

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(b2_stdSignIn), typeof(b2_stdSignIn));
        Routing.RegisterRoute(nameof(c2_stdCreate), typeof(c2_stdCreate));
        Routing.RegisterRoute(nameof(b1_tcrSignIn), typeof(b1_tcrSignIn));
        Routing.RegisterRoute(nameof(d2_SetUp), typeof(d2_SetUp));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_initialRouteChecked)
            return;

        _initialRouteChecked = true;

        if (SessionService.IsLoggedIn)
        {
            if (SessionService.Role == "Teacher")
                await GoToAsync("//MainTabs/HomeTab/c1_HomePage", false);
            else if (SessionService.Role == "Student")
                await GoToAsync("//StudentMain", false);
        }
    }
}