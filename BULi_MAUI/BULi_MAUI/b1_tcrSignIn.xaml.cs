namespace BULi_MAUI;

public partial class b1_tcrSignIn : ContentPage
{
    public b1_tcrSignIn()
    {
        InitializeComponent();
    }

    private async void OnEnterClicked(object? sender, EventArgs e)
    {
        string username = txtUsername.Text?.Trim() ?? string.Empty;
        string password = txtPassword.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Missing Fields", "Please enter your username and password.", "OK");
            return;
        }

        btnEnter.IsEnabled = false;
        try
        {
            var result = await SisWriteService.AuthenticateTeacherAsync(username, password);
            if (!result.Success)
            {
                await DisplayAlertAsync("Sign-In Failed", "Invalid teacher username or password.", "OK");
                return;
            }

            SessionService.UserId = result.TeacherId.ToString();
            SessionService.FullName = result.FullName;
            SessionService.Email = username;

            c1_HomePage.CurrentSessionUsername = username;
            c1_HomePage.CurrentSessionPassword = password;
            Preferences.Default.Set("TeacherSessionUsername", username);
            Preferences.Default.Set("TeacherSessionPassword", password);

            SessionService.LoginAsTeacher();

            await Shell.Current.GoToAsync("//MainTabs/HomeTab/c1_HomePage");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Sign-In Error", ex.Message, "OK");
        }
        finally
        {
            btnEnter.IsEnabled = true;
        }
    }
}