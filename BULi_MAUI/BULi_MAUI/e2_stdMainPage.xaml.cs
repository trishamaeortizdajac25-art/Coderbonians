using MySqlConnector;

namespace BULi_MAUI;

public partial class e2_stdMainPage : ContentPage
{
    private bool _isEditing;
    private string _actualPassword = string.Empty;
    private string _dbUsername = string.Empty;
    private string _dbAddress = string.Empty;
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

    private static string KeepStudentIdChars(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return new string(input.Where(c => char.IsDigit(c) || c == '-').ToArray());
    }

    private static string KeepLettersOnly(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return new string(input.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
    }

    private async Task LoadStudentExtraFromDbAsync(string idOrNumber)
    {
        try
        {
            await using var conn = new MySqlConnection(SisReadService.ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT username, address
                FROM students
                WHERE (student_id = @id OR student_number = @snum) AND COALESCE(is_deleted, 0) = 0
                LIMIT 1;";

            await using var cmd = new MySqlCommand(sql, conn);
            uint.TryParse(idOrNumber, out uint parsedId);
            cmd.Parameters.AddWithValue("@id", parsedId);
            cmd.Parameters.AddWithValue("@snum", idOrNumber);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                if (!reader.IsDBNull(0))
                    _dbUsername = reader.GetString(0);
                if (!reader.IsDBNull(1))
                    _dbAddress = reader.GetString(1);
            }
        }
        catch
        {
            // Fallback handled in UI
        }
    }

    private async Task LoadStudentInfoAsync()
    {
        try
        {
            const string placeholder = "Not yet updated";

            string fullName = SessionService.FullName?.Trim() ?? string.Empty;
            string studentId = KeepStudentIdChars(SessionService.UserId);
            string program = SessionService.Program?.Trim() ?? string.Empty;
            string bloc = SessionService.Bloc?.Trim() ?? string.Empty;
            string yearLevel = SessionService.YearLevel?.Trim() ?? string.Empty;
            string contact = KeepDigitsOnly(SessionService.Contact, 11);
            string gender = SessionService.Gender?.Trim() ?? string.Empty;
            string email = SessionService.Email?.Trim() ?? string.Empty;

            await LoadStudentExtraFromDbAsync(studentId);
            string address = _dbAddress.Trim();

            bool hasBirthDate = SessionService.BirthDate.HasValue;
            DateTime birthDate = SessionService.BirthDate ?? DateTime.Today;
            string birthDateText = hasBirthDate ? birthDate.ToString("yyyy-MM-dd") : string.Empty;
            string ageText = hasBirthDate ? CalculateAge(birthDate).ToString() : string.Empty;

            lblHeaderName.Text = string.IsNullOrWhiteSpace(fullName) ? "Student" : fullName;
            lblHeaderSub.Text = string.IsNullOrWhiteSpace(studentId) ? "ID: Not yet updated" : $"ID: {studentId}";

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
            lblAddress.Text = string.IsNullOrWhiteSpace(address) ? placeholder : address;

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
            txtAddress.Text = address;
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
        lblAddress.IsVisible = !editing;

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
        txtAddress.IsVisible = editing;

        if (editing)
        {
            txtStudentId.Keyboard = Keyboard.Text;
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

        string filtered = entry == txtStudentId
            ? KeepStudentIdChars(e.NewTextValue)
            : KeepDigitsOnly(e.NewTextValue);

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

        if (string.IsNullOrWhiteSpace(_dbUsername))
            _dbUsername = Preferences.Default.Get("StudentSessionUsername", string.Empty);

        if (string.IsNullOrEmpty(_actualPassword))
            _actualPassword = Preferences.Default.Get("StudentSessionPassword", string.Empty);

        lblProfileUsername.Text = !string.IsNullOrWhiteSpace(_dbUsername)
            ? _dbUsername
            : (!string.IsNullOrWhiteSpace(SessionService.Email) ? SessionService.Email : SessionService.UserId);

        _isPasswordVisible = false;
        btnTogglePasswordVisibility.Text = "Show";
        lblProfilePassword.Text = !string.IsNullOrEmpty(_actualPassword)
            ? new string('•', Math.Max(8, _actualPassword.Length))
            : "••••••••";
    }

    // --- EDIT / SAVE & DELETE BUTTONS ---

    private async void OnEditOrSaveClicked(object? sender, EventArgs e)
    {
        if (!_isEditing)
        {
            SetEditMode(true);
            return;
        }

        string cleanId = KeepStudentIdChars(txtStudentId.Text).Trim();
        string cleanContact = KeepDigitsOnly(txtContact.Text, 11);
        string cleanAgeText = KeepDigitsOnly(txtAge.Text, 2);
        string fullName = KeepLettersOnly(txtName.Text).Trim();
        string program = KeepLettersOnly(txtProgram.Text).Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            await DisplayAlertAsync("Validation", "Name must contain letters only and cannot be empty.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(cleanId) || !cleanId.Any(char.IsDigit))
        {
            await DisplayAlertAsync("Validation", "Student ID must contain numbers and '-' only.", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(cleanContact) && cleanContact.Length != 11)
        {
            await DisplayAlertAsync("Validation", "Contact number must be 11 digits.", "OK");
            return;
        }

        DateTime? birthDate = null;
        if (int.TryParse(cleanAgeText, out int editedAge) && editedAge > 0)
        {
            DateTime baseDate = dpBirthDate.Date ?? DateTime.Today;
            if (CalculateAge(baseDate) != editedAge)
            {
                int targetYear = DateTime.Today.Year - editedAge;
                int day = Math.Min(baseDate.Day, DateTime.DaysInMonth(targetYear, baseDate.Month));
                birthDate = new DateTime(targetYear, baseDate.Month, day);
            }
            else
            {
                birthDate = baseDate;
            }
        }
        else if (SessionService.BirthDate.HasValue)
        {
            birthDate = dpBirthDate.Date;
        }

        string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string firstName = nameParts.Length > 0 ? nameParts[0] : "Student";
        string lastName = nameParts.Length > 1 ? nameParts[^1] : "User";
        string? middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;

        string bloc = txtBloc.Text?.Trim() ?? string.Empty;
        string yearLevel = txtYearLevel.Text?.Trim() ?? string.Empty;
        string gender = pckGender.SelectedItem?.ToString() ?? string.Empty;
        string email = txtEmail.Text?.Trim() ?? string.Empty;
        string address = txtAddress.Text?.Trim() ?? string.Empty;

        btnEditOrSave.IsEnabled = false;
        try
        {
            string previousId = KeepStudentIdChars(SessionService.UserId);
            string dbIdRaw = Preferences.Default.Get("StudentSessionDbId", previousId);
            uint.TryParse(dbIdRaw, out uint dbStudentId);

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
                Email = email,
                Address = address
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
            _dbAddress = address;

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
            string dbIdRaw = Preferences.Default.Get("StudentSessionDbId", SessionService.UserId);
            if (uint.TryParse(dbIdRaw, out uint dbStudentId) && dbStudentId > 0)
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
        await LoadStudentExtraFromDbAsync(KeepStudentIdChars(SessionService.UserId));
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
            _actualPassword = Preferences.Default.Get("StudentSessionPassword", string.Empty);

        _isPasswordVisible = !_isPasswordVisible;
        btnTogglePasswordVisibility.Text = _isPasswordVisible ? "Hide" : "Show";

        if (_isPasswordVisible)
        {
            lblProfilePassword.Text = !string.IsNullOrEmpty(_actualPassword)
                ? _actualPassword
                : "(No cached password — re-login once)";
        }
        else
        {
            lblProfilePassword.Text = new string('•', Math.Max(8, _actualPassword.Length));
        }
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
            string cleanId = KeepStudentIdChars(SessionService.UserId);
            string dbIdRaw = Preferences.Default.Get("StudentSessionDbId", cleanId);
            uint.TryParse(dbIdRaw, out uint dbStudentId);

            var result = await SisWriteService.ChangeStudentPasswordAsync(dbStudentId, cleanId, oldPassword, newPassword);
            if (result.Success)
            {
                _actualPassword = newPassword;
                Preferences.Default.Set("StudentSessionPassword", newPassword);
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

        Preferences.Default.Remove("StudentSessionPassword");
        Preferences.Default.Remove("StudentSessionDbId");
        SessionService.Logout();
        await Shell.Current.GoToAsync("//RolePage");
    }
}