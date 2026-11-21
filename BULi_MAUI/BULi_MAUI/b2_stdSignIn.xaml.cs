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
        string username = txtUsername.Text?.Trim() ?? string.Empty;
        string password = txtPassword.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Missing Fields", "Please enter your username and password.", "OK");
            return;
        }

        btnSignIn.IsEnabled = false;
        try
        {
            var student = await SisWriteService.AuthenticateStudentAsync(username, password);
            if (student is null)
            {
                await DisplayAlertAsync("Sign-In Failed", "Invalid username or password.", "OK");
                return;
            }

            // Load student data from MySQL into SessionService
            SessionService.LoginAsStudent(student.StudentId.ToString(), student.FullName);
            SessionService.Program = student.Program;
            SessionService.YearLevel = student.YearLevel;
            SessionService.Bloc = student.Block;
            SessionService.Gender = student.Gender;
            SessionService.Contact = student.ContactNumber;
            SessionService.Email = student.Email ?? string.Empty;
            SessionService.BirthDate = student.Birthdate;

            await Shell.Current.GoToAsync("//StudentMain");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Sign-In Error", ex.Message, "OK");
        }
        finally
        {
            btnSignIn.IsEnabled = true;
        }
    }

    private async void OnForgotPasswordTapped(object? sender, TappedEventArgs e)
    {
        await DisplayAlertAsync("Password Reset", "Please contact the BSCS/College of Science registrar or check your student email to reset your password.", "OK");
    }
}