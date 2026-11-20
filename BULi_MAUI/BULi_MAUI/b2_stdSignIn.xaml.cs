namespace BULi_MAUI;

public partial class b2_stdSignIn : ContentPage
{
    public b2_stdSignIn()
    {
        InitializeComponent();
    }

    private async void OnCreateAccountTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(c2_stdCreate));
    }

    private async void OnSignInClicked(object? sender, EventArgs e)
    {
        try
        {
            // Existing student signs in -> goes straight to e2_stdMainPage
            SessionService.LoginAsStudent(SessionService.UserId, SessionService.FullName);
            await Shell.Current.GoToAsync("//StudentMain");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Sign-In Error", ex.Message, "OK");
        }
    }

    private async void OnForgotPasswordTapped(object? sender, TappedEventArgs e)
    {
        await DisplayAlertAsync("Password Reset", "Please contact the BSCS/College of Science registrar or check your student email to reset your password.", "OK");
    }
}