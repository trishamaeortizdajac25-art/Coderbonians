using MySqlConnector;

namespace BULi_MAUI;

public partial class e2_stdMainPage : ContentPage
{
    private bool _isEditing;
    private string _actualPassword = string.Empty;
    private string _dbUsername = string.Empty;
    private bool _isPasswordVisible;

    public e2_stdMainPage()
    {
        InitializeComponent();
        dpBirthDate.MaximumDate = DateTime.Today;

        txtAge.TextChanged += OnAgeTextChanged;
        txtName.TextChanged += OnLettersOnlyTextChanged;
        txtProgram.TextChanged += OnLettersOnlyTextChanged;
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

    private static string KeepLettersOnly(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return new string(input.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
    }

    private async Task LoadStudentUsernameFromDbAsync(string idOrNumber)
    {
        try
        {
            await using var conn = new MySqlConnection(SisReadService.ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT username
                FROM students
                WHERE (student_id = @id OR student_number = @snum) AND is_deleted = 0
                LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            uint.TryParse(idOrNumber, out uint parsedId);
            cmd.Parameters.AddWithValue("@id", parsedId);
            cmd.Parameters.AddWithValue("@snum", idOrNumber);

            object? result = await cmd.ExecuteScalarAsync();
            if (result is string uname && !string.IsNullOrWhiteSpace(uname))
                _dbUsername = uname;
        }
        catch
        {
            // Fallback handled in RefreshProfileVisuals
        }
    }

    private async Task LoadStudentInfoAsync()
    {
        try
        {
            const string placeholder = "Not yet updated";

            string fullName = SessionService.FullName?.Trim() ?? string.Empty;
            string studentId = KeepDigitsOnly(SessionService.UserId);
            string program = SessionService.Program?.Trim() ?? string.Empty;
            string bloc = SessionService.Bloc?.Trim() ?? string.Empty;
            string yearLevel = SessionService.YearLevel?.Trim() ?? string.Empty;
            string contact = KeepDigitsOnly(SessionService.Contact, 11);
            string gender = SessionService.Gender?.Trim() ?? string.Empty;
            string email = SessionService.Email?.Trim() ?? string.Empty;

            bool hasBirthDate = SessionService.BirthDate.HasValue;
            DateTime birthDate = SessionService.BirthDate ?? DateTime.Today;
            string birthDateText = hasBirthDate ? birthDate.ToString("yyyy-MM-dd") : string.Empty;
            string ageText = hasBirthDate ? CalculateAge(birthDate).ToString() : string.Empty;

            lblHeaderName.Text = string.IsNullOrWhiteSpace(fullName) ? "Student" : fullName;
            lblHeaderSub.Text = $"ID: {studentId}";

            // View labels show "Not yet updated" when empty
            lblName.Text = string.IsNullOrWhiteSpace(fullName) ? placeholder : fullName;
            lblStudentId.Text = string.IsNullOrWhiteSpace(studentId) ? placeholder : studentId;
            lblProgram.Text = string.IsNullOrWhiteSpace(program) ? placeholder : program;
            lblBirthDate.Text = string.IsNullOrWhiteSpace(birthDateText) ? placeholder : birthDateText;
            lblAge.Text = string.IsNullOrWhiteSpace(ageText) ? placeholder : ageText;
            lblBloc.Text = string.IsNullOrWhiteSpace(bloc) ? placeholder : bloc;
            lblYearLevel.Text = string.IsNullOrWhiteSpace(yearLevel) ? placeholder : yearLevel;
            lblGender.Text = string.IsNullOrWhiteSpace(gender) ? placeholder : gender;
            lblContact.Text = string.IsNullOrWhiteSpace(contact) ? placeholder : contact;
            lblEmail.Text = string.IsNullOrWhiteSpace(email) ? placeholder : email;

            // Edit fields remain clean/empty so the student can type immediately
            txtName.Text = fullName;
            txtStudentId.Text = studentId;
            txtProgram.Text = program;
            dpBirthDate.Date = birthDate;
            txtAge.Text = ageText;
            txtBloc.Text = bloc;
            txtYearLevel.Text = yearLevel;
            pckGender.SelectedItem = string.IsNullOrWhiteSpace(gender) ? null : gender;
            txtContact.Text = contact;
            txtEmail.Text = email;

            await LoadStudentUsernameFromDbAsync(studentId);
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
        {
            txtAge.IsReadOnly = false;
            txtAge.TextColor = Colors.White;
            txtContact.MaxLength = 11;
            txtName.Focus();
        }
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

    private void OnAgeTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        string filtered = KeepDigitsOnly(e.NewTextValue, 2);
        if (entry.Text != filtered)
            entry.Text = filtered;
    }

    private void OnContactTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        string filtered = KeepDigitsOnly(e.NewTextValue, 11);
        if (entry.Text != filtered)
            entry.Text = filtered;
    }

    private void OnLettersOnlyTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        string filtered = KeepLettersOnly(e.NewTextValue);
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

        lblProfileUsername.Text = !string.IsNullOrWhiteSpace(_dbUsername)
            ? _dbUsername
            : (!string.IsNullOrWhiteSpace(SessionService.Email) ? SessionService.Email : SessionService.UserId);

        _isPasswordVisible = false;
        btnTogglePasswordVisibility.Text = "Show";
        lblProfilePassword.Text = !string.IsNullOrEmpty(_actualPassword)
            ? new string('•', Math.Max(8, _actualPassword.Length))
            : "•••••••• (SHA-256 Protected)";
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
        string cleanContact = KeepDigitsOnly(txtContact.Text, 11);
        string cleanAgeText = KeepDigitsOnly(txtAge.Text, 2);
        string fullName = KeepLettersOnly(txtName.Text).Trim();
        string program = KeepLettersOnly(txtProgram.Text).Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            await DisplayAlertAsync("Validation", "Name must contain letters only and cannot be empty.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(cleanId))
        {
            await DisplayAlertAsync("Validation", "Student ID must contain numbers only.", "OK");
            return;
        }

        if (cleanContact.Length != 11)
        {
            await DisplayAlertAsync("Validation", "Contact number must be exactly 11 digits.", "OK");
            return;
        }

        if (!int.TryParse(cleanAgeText, out int editedAge) || cleanAgeText.Length == 0)
        {
            await DisplayAlertAsync("Validation", "Age must be a 2-digit number.", "OK");
            return;
        }

        DateTime birthDate = dpBirthDate.Date ?? new DateTime(2008, 6, 15);
        if (CalculateAge(birthDate) != editedAge)
        {
            int targetYear = DateTime.Today.Year - editedAge;
            int day = Math.Min(birthDate.Day, DateTime.DaysInMonth(targetYear, birthDate.Month));
            birthDate = new DateTime(targetYear, birthDate.Month, day);
        }

        string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string firstName = nameParts.Length > 0 ? nameParts[0] : "Student";
        string lastName = nameParts.Length > 1 ? nameParts[^1] : "User";
        string? middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;

        string bloc = txtBloc.Text?.Trim() ?? string.Empty;
        string yearLevel = txtYearLevel.Text?.Trim() ?? string.Empty;
        string gender = pckGender.SelectedItem?.ToString() ?? "Male";
        string email = txtEmail.Text?.Trim() ?? string.Empty;

        btnEditOrSave.IsEnabled = false;
        try
        {
            string previousId = KeepDigitsOnly(SessionService.UserId);
            uint.TryParse(previousId, out uint dbStudentId);

            var record = new StudentRecord
            {
                StudentId = dbStudentId,
                StudentNumber = cleanId,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Gender = gender,
                Birthdate = birthDate,
                YearLevel = yearLevel,
                Program = program,
                Block = bloc,
                ContactNumber = cleanContact,
                Email = email
            };

            bool updated = await SisWriteService.UpdateStudentProfileAsync(record, previousId);
            if (!updated)
            {
                await DisplayAlertAsync("Save Error", "Could not update record in the database.", "OK");
                return;
            }

            SessionService.UserId = cleanId;
            SessionService.FullName = fullName;
            SessionService.Program = program;
            SessionService.BirthDate = birthDate;
            SessionService.Bloc = bloc;
            SessionService.YearLevel = yearLevel;
            SessionService.Gender = gender;
            SessionService.Contact = cleanContact;
            SessionService.Email = email;

            SetEditMode(false);
            await LoadStudentInfoAsync();
            RefreshProfileVisuals();
            await DisplayAlertAsync("Saved", "Profile updated!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Database Error", ex.Message, "OK");
        }
        finally
        {
            btnEditOrSave.IsEnabled = true;
        }
    }

    private async void OnDeleteStudentClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Delete Account", "Are you sure you want to delete your student record?", "Delete", "Cancel");
        if (!confirm)
            return;

        try
        {
            if (uint.TryParse(SessionService.UserId, out uint dbStudentId) && dbStudentId > 0)
            {
                await SisWriteService.DeleteStudentAsync(dbStudentId);
            }

            SessionService.Logout();
            await DisplayAlertAsync("Deleted", "Your student record has been removed.", "OK");
            await Shell.Current.GoToAsync("//RolePage");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Delete Error", ex.Message, "OK");
        }
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
        await LoadStudentUsernameFromDbAsync(KeepDigitsOnly(SessionService.UserId));
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
        if (string.IsNullOrEmpty(_actualPassword))
        {
            DisplayAlertAsync("Encrypted Password", "Passwords are stored using SHA-256 encryption. Use 'Change Password' in the menu to set a new one.", "OK");
            return;
        }

        _isPasswordVisible = !_isPasswordVisible;
        btnTogglePasswordVisibility.Text = _isPasswordVisible ? "Hide" : "Show";
        lblProfilePassword.Text = _isPasswordVisible
            ? _actualPassword
            : new string('•', Math.Max(8, _actualPassword.Length));
    }

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();

        string? oldPassword = await DisplayPromptAsync(
            "Change Password",
            "Enter your current password:",
            accept: "Next",
            cancel: "Cancel",
            placeholder: "Current password");

        if (string.IsNullOrWhiteSpace(oldPassword))
            return;

        string? newPassword = await DisplayPromptAsync(
            "Change Password",
            "Enter your new password:",
            accept: "Save",
            cancel: "Cancel",
            placeholder: "New password");

        if (string.IsNullOrWhiteSpace(newPassword))
            return;

        try
        {
            string cleanId = KeepDigitsOnly(SessionService.UserId);
            uint.TryParse(cleanId, out uint dbStudentId);

            var result = await SisWriteService.ChangeStudentPasswordAsync(dbStudentId, cleanId, oldPassword, newPassword);
            if (result.Success)
            {
                _actualPassword = newPassword;
                RefreshProfileVisuals();
            }

            await DisplayAlertAsync(result.Success ? "Success" : "Failed", result.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Database Error", ex.Message, "OK");
        }
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