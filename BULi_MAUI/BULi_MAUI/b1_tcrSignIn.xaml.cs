namespace BULi_MAUI;

public partial class b1_tcrSignIn : ContentPage
{
    public b1_tcrSignIn()
    {
        InitializeComponent();
    }

    private async void OnEnterClicked(object? sender, EventArgs e)
    {
        try
        {
            SessionService.LoginAsTeacher();
            await Shell.Current.GoToAsync("//MainTabs/HomeTab/c1_HomePage");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Sign-In Error", ex.Message, "OK");
        }
    }
}