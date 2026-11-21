namespace BULi_MAUI;

public partial class c2_stdCreate : ContentPage
{
    public c2_stdCreate()
    {
        InitializeComponent();
    }

    private async void OnSignUpClicked(object? sender, EventArgs e)
    {
        string username = txtUsername.Text?.Trim() ?? string.Empty;
        string password = txtPassword.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Missing Fields", "Please enter a username and password.", "OK");
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

            // Store the new DB student_id temporarily so d2_SetUp knows which row to update
            SessionService.LoginAsStudent(result.StudentId.ToString(), string.Empty);
            SessionService.Program = string.Empty;
            SessionService.YearLevel = string.Empty;
            SessionService.Bloc = string.Empty;
            SessionService.Gender = string.Empty;
            SessionService.Contact = string.Empty;
            SessionService.Email = string.Empty;
            SessionService.BirthDate = null;

            Preferences.Default.Set("StudentSessionDbId", result.StudentId.ToString());
            Preferences.Default.Set("StudentSessionUsername", username);
            Preferences.Default.Set("StudentSessionPassword", password);

            await Shell.Current.GoToAsync(nameof(d2_SetUp));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Sign-Up Error", ex.Message, "OK");
        }
        finally
        {
            btnSignUp.IsEnabled = true;
        }
    }

    private async void OnSignInTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}