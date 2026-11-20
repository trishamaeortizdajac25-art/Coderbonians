namespace BULi_MAUI;

public partial class e2_stdMainPage : ContentPage
{
    private bool _isEditing;
    private string _actualPassword = "••••••••";
    private bool _isPasswordVisible;

    public e2_stdMainPage()
    {
        InitializeComponent();
        dpBirthDate.MaximumDate = DateTime.Today;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.ApplyTheme(this, btnThemeToggle);
        await LoadStudentInfoAsync();
        RefreshProfileVisuals();
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        int age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;
        return Math.Max(0, age);
    }

    private static string KeepDigitsOnly(string? input, int maxLength = int.MaxValue)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var digits = new string(input.Where(char.IsDigit).ToArray());
        return digits.Length > maxLength ? digits[..maxLength] : digits;
    }

    private async Task LoadStudentInfoAsync()
    {
        try
        {
            string fullName = !string.IsNullOrWhiteSpace(SessionService.FullName) ? SessionService.FullName : "Test Student";
            string studentId = KeepDigitsOnly(!string.IsNullOrWhiteSpace(SessionService.UserId) ? SessionService.UserId : "20260099");
            string program = !string.IsNullOrWhiteSpace(SessionService.Program) ? SessionService.Program : "BS Information Technology";
            string bloc = !string.IsNullOrWhiteSpace(SessionService.Bloc) ? SessionService.Bloc : "A";
            string yearLevel = !string.IsNullOrWhiteSpace(SessionService.YearLevel) ? SessionService.YearLevel : "1st Year";
            string contact = KeepDigitsOnly(!string.IsNullOrWhiteSpace(SessionService.Contact) ? SessionService.Contact : "09123456789", 13);
            string gender = string.Equals(SessionService.Gender, "Female", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";
            string email = !string.IsNullOrWhiteSpace(SessionService.Email) ? SessionService.Email : "test.student@example.com";

            DateTime birthDate = SessionService.BirthDate ?? new DateTime(2008, 6, 15);
            int age = CalculateAge(birthDate);

            lblHeaderName.Text = fullName;
            lblHeaderSub.Text = $"ID: {studentId}";

            lblName.Text = fullName;
            lblStudentId.Text = studentId;
            lblProgram.Text = program;
            lblBirthDate.Text = birthDate.ToString("yyyy-MM-dd");
            lblAge.Text = age.ToString();
            lblBloc.Text = bloc;
            lblYearLevel.Text = yearLevel;
            lblGender.Text = gender;
            lblContact.Text = contact;
            lblEmail.Text = email;

            txtName.Text = fullName;
            txtStudentId.Text = studentId;
            txtProgram.Text = program;
            dpBirthDate.Date = birthDate;
            txtAge.Text = age.ToString();
            txtBloc.Text = bloc;
            txtYearLevel.Text = yearLevel;
            pckGender.SelectedItem = gender;
            txtContact.Text = contact;
            txtEmail.Text = email;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private void SetEditMode(bool editing)
    {
        _isEditing = editing;
        btnEditOrSave.Text = editing ? "Save" : "Edit";

        lblName.IsVisible = !editing;
        lblStudentId.IsVisible = !editing;
        lblProgram.IsVisible = !editing;
        lblBirthDate.IsVisible = !editing;
        lblAge.IsVisible = !editing;
        lblBloc.IsVisible = !editing;
        lblYearLevel.IsVisible = !editing;
        lblGender.IsVisible = !editing;
        lblContact.IsVisible = !editing;
        lblEmail.IsVisible = !editing;

        txtName.IsVisible = editing;
        txtStudentId.IsVisible = editing;
        txtProgram.IsVisible = editing;
        dpBirthDate.IsVisible = editing;
        txtAge.IsVisible = editing;
        txtBloc.IsVisible = editing;
        txtYearLevel.IsVisible = editing;
        pckGender.IsVisible = editing;
        txtContact.IsVisible = editing;
        txtEmail.IsVisible = editing;

        if (editing)
            txtName.Focus();
    }

    // --- INPUT RESTRICTIONS & DATE SELECTION ---

    private void OnDigitsOnlyTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        string filtered = KeepDigitsOnly(e.NewTextValue);
        if (entry.Text != filtered)
            entry.Text = filtered;
    }

    private void OnContactTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        string filtered = KeepDigitsOnly(e.NewTextValue, 13);
        if (entry.Text != filtered)
            entry.Text = filtered;
    }

    private void OnBirthDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (e.NewDate is DateTime selected)
            txtAge.Text = CalculateAge(selected).ToString();
    }

    private void RefreshProfileVisuals()
    {
        string initials = SessionService.GetInitials();
        lblHeaderInitials.Text = initials;
        lblCardInitials.Text = initials;
        lblModalInitials.Text = initials;

        if (!string.IsNullOrWhiteSpace(SessionService.PhotoPath) && File.Exists(SessionService.PhotoPath))
        {
            var src = ImageSource.FromFile(SessionService.PhotoPath);

            imgHeaderAvatar.Source = src;
            imgHeaderAvatar.IsVisible = true;
            lblHeaderInitials.IsVisible = false;

            imgCardPhoto.Source = src;
            imgCardPhoto.IsVisible = true;
            lblCardInitials.IsVisible = false;

            imgModalPhoto.Source = src;
            imgModalPhoto.IsVisible = true;
            lblModalInitials.IsVisible = false;
        }
        else
        {
            imgHeaderAvatar.Source = null;
            imgHeaderAvatar.IsVisible = false;
            lblHeaderInitials.IsVisible = true;

            imgCardPhoto.Source = null;
            imgCardPhoto.IsVisible = false;
            lblCardInitials.IsVisible = true;

            imgModalPhoto.Source = null;
            imgModalPhoto.IsVisible = false;
            lblModalInitials.IsVisible = true;
        }

        lblProfileUsername.Text = !string.IsNullOrWhiteSpace(SessionService.UserId)
            ? SessionService.UserId
            : "student";

        _isPasswordVisible = false;
        btnTogglePasswordVisibility.Text = "Show";
        lblProfilePassword.Text = new string('•', Math.Max(8, _actualPassword.Length));
    }

    // --- EDIT / SAVE & DELETE BUTTONS ---

    private async void OnEditOrSaveClicked(object? sender, EventArgs e)
    {
        if (!_isEditing)
        {
            SetEditMode(true);
            return;
        }

        string cleanId = KeepDigitsOnly(txtStudentId.Text);
        string cleanContact = KeepDigitsOnly(txtContact.Text, 13);

        if (string.IsNullOrWhiteSpace(cleanId))
        {
            await DisplayAlertAsync("Validation", "Student ID must contain numbers only.", "OK");
            return;
        }

        if (cleanContact.Length > 13)
        {
            await DisplayAlertAsync("Validation", "Contact number cannot exceed 13 digits.", "OK");
            return;
        }

        SessionService.FullName = txtName.Text?.Trim() ?? string.Empty;
        SessionService.UserId = cleanId;
        SessionService.Program = txtProgram.Text?.Trim() ?? string.Empty;
        SessionService.BirthDate = dpBirthDate.Date ?? new DateTime(2008, 6, 15);
        SessionService.Bloc = txtBloc.Text?.Trim() ?? string.Empty;
        SessionService.YearLevel = txtYearLevel.Text?.Trim() ?? string.Empty;
        SessionService.Gender = pckGender.SelectedItem?.ToString() ?? "Male";
        SessionService.Contact = cleanContact;
        SessionService.Email = txtEmail.Text?.Trim() ?? string.Empty;

        SetEditMode(false);
        await LoadStudentInfoAsync();
        RefreshProfileVisuals();
    }

    private async void OnDeleteStudentClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Delete Record", "Clear this student record?", "Delete", "Cancel");
        if (!confirm)
            return;

        SessionService.FullName = string.Empty;
        SessionService.Program = string.Empty;
        SessionService.Bloc = string.Empty;
        SessionService.YearLevel = string.Empty;
        SessionService.Gender = "Male";
        SessionService.Contact = string.Empty;
        SessionService.Email = string.Empty;
        SessionService.BirthDate = null;

        SetEditMode(false);
        await LoadStudentInfoAsync();
        RefreshProfileVisuals();
    }

    // --- RIGHT SLIDE-OUT PROFILE DRAWER ---

    private async void OnToggleProfileDrawerTapped(object? sender, TappedEventArgs e)
    {
        if (!ProfileDrawerOverlay.IsVisible)
        {
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

    // --- DRAWER BUTTONS ---

    private async void OnViewProfileClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();
        RefreshProfileVisuals();
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

            string destPath = Path.Combine(FileSystem.AppDataDirectory, $"student_profile{ext}");

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
                File.Delete(SessionService.PhotoPath);
        }
        catch
        {
            // Ignore file lock issues
        }

        SessionService.PhotoPath = string.Empty;
        RefreshProfileVisuals();
    }

    private void OnTogglePasswordVisibilityClicked(object? sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        btnTogglePasswordVisibility.Text = _isPasswordVisible ? "Hide" : "Show";
        lblProfilePassword.Text = _isPasswordVisible
            ? _actualPassword
            : new string('•', Math.Max(8, _actualPassword.Length));
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();
        await DisplayAlertAsync("Change Password", "Change password hook ready.", "OK");
    }

    private void OnToggleThemeClicked(object? sender, EventArgs e)
    {
        SessionService.IsDarkMode = !SessionService.IsDarkMode;
        ThemeService.ApplyTheme(this, btnThemeToggle);
    }

    private async void OnLogOutClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();

        bool confirm = await DisplayAlertAsync("Log Out", "Are you sure you want to log out?", "Yes", "Cancel");
        if (!confirm)
            return;

        SessionService.Logout();
        await Shell.Current.GoToAsync("//RolePage");
    }
}