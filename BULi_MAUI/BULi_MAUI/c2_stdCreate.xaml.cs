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
        string username = txtUsername.Text?.Trim() ?? string.Empty;
        string password = txtPassword.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Missing Fields", "Please enter both a username and a password.", "OK");
            return;
        }

        btnSignUp.IsEnabled = false;
        try
        {
            var result = await SisWriteService.RegisterStudentAsync(username, password);
            if (!result.Success)
            {
                await DisplayAlertAsync("Sign-Up Failed", result.Message, "OK");
                return;
            }

            // Save the new database student_id into SessionService so d2_SetUp can update this row
            SessionService.Logout();
            SessionService.LoginAsStudent(result.StudentId.ToString(), "");
            await Shell.Current.GoToAsync(nameof(d2_SetUp));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Connection Error", ex.Message, "OK");
        }
        finally
        {
            btnSignUp.IsEnabled = true;
        }
    }
}