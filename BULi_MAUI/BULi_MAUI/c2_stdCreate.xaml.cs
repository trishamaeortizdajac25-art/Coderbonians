namespace BULi_MAUI;

public partial class c2_stdCreate : ContentPage
{
    public c2_stdCreate()
    {
        InitializeComponent();
    }

    private async void OnSignInTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSignUpClicked(object? sender, EventArgs e)
    {
        // New student account -> goes to d2_SetUp profile wizard
        await Shell.Current.GoToAsync(nameof(d2_SetUp));
    }
}