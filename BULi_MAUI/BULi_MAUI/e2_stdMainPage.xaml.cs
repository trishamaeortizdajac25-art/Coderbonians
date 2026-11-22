using System.Globalization;
using MySqlConnector;

namespace BULi_MAUI;

public partial class e2_stdMainPage : ContentPage
{
    private bool _isEditing;
    private bool _hasBirthDate;
    private bool _suppressDateSelected;
    private string _actualPassword = string.Empty;
    private string _dbUsername = string.Empty;
    private string _dbAddress = string.Empty;
    private string _dbStatus = "Active";
    private bool _isPasswordVisible;

    public e2_stdMainPage()
    {
        InitializeComponent();
        _suppressDateSelected = true;
        dpBirthDate.MaximumDate = DateTime.Today;
        _suppressDateSelected = false;

        txtAge.TextChanged += OnAgeTextChanged;
        txtName.TextChanged += OnLettersOnlyTextChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyStudentThemes();
        await LoadStudentInfoAsync();
        RefreshProfileVisuals();
    }

    private void ApplyStudentThemes()
    {
        bool isDark = SessionService.IsDarkMode;
        ThemeService.ApplyTheme(this, btnThemeToggle);

        var cardBg = isDark ? Color.FromArgb("#0D1B2A") : Color.FromArgb("#FFFFFF");
        var altCardBg = isDark ? Color.FromArgb("#15263A") : Color.FromArgb("#F1F5F9");
        var drawerBg = isDark ? Color.FromArgb("#0D1B2A") : Color.FromArgb("#F8FAFC");
        var strokeColor = isDark ? Color.FromArgb("#22364D") : Color.FromArgb("#CBD5E1");
        var primaryText = isDark ? Colors.White : Color.FromArgb("#0F172A");
        var linkBlue = isDark ? Color.FromArgb("#87CEEB") : Color.FromArgb("#0284C7");

        StudentInfoCard.BackgroundColor = cardBg;
        StudentInfoCard.Stroke = new SolidColorBrush(strokeColor);
        CardPhotoPlusBadge.Stroke = new SolidColorBrush(cardBg);

        ProfileDrawerPanel.BackgroundColor = drawerBg;
        ProfileDrawerPanel.Stroke = new SolidColorBrush(strokeColor);

        ViewProfileCard.BackgroundColor = cardBg;
        ViewProfileCard.Stroke = new SolidColorBrush(strokeColor);

        ProfileUsernameBorder.BackgroundColor = altCardBg;
        ProfileUsernameBorder.Stroke = new SolidColorBrush(strokeColor);

        ProfilePasswordBorder.BackgroundColor = altCardBg;
        ProfilePasswordBorder.Stroke = new SolidColorBrush(strokeColor);

        lblDrawerTitle.TextColor = primaryText;
        lblModalTitle.TextColor = primaryText;
        lblProfileUsername.TextColor = primaryText;
        lblProfilePassword.TextColor = primaryText;

        btnViewProfile.BackgroundColor = altCardBg;
        btnViewProfile.BorderColor = strokeColor;
        btnViewProfile.TextColor = primaryText;

        btnChangePassword.BackgroundColor = altCardBg;
        btnChangePassword.BorderColor = strokeColor;
        btnChangePassword.TextColor = primaryText;

        btnThemeToggle.Text = isDark ? "Light Mode" : "Dark Mode";
        btnThemeToggle.BackgroundColor = altCardBg;
        btnThemeToggle.BorderColor = strokeColor;
        btnThemeToggle.TextColor = linkBlue;

        Entry[] entries = [txtName, txtStudentId, txtBloc, txtContact, txtEmail, txtAddress, txtBirthDateGhost, txtAge];
        foreach (var entry in entries)
        {
            entry.BackgroundColor = altCardBg;
            entry.TextColor = primaryText;
            entry.PlaceholderColor = isDark ? Color.FromArgb("#6C8299") : Color.FromArgb("#64748B");
        }

        Picker[] pickers = [pckProgram, pckYearLevel, pckGender];
        foreach (var picker in pickers)
        {
            picker.BackgroundColor = altCardBg;
            picker.TextColor = primaryText;
            picker.TitleColor = isDark ? Color.FromArgb("#8FA3B8") : Color.FromArgb("#475569");
        }

        ApplyStatusBadgeStyle(_dbStatus, isDark, altCardBg);
    }

    private static string NormalizeStatus(string? rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
            return "Active";

        return rawStatus.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "dropped" => "Dropped",
            "graduated" => "Graduated",
            "on leave" or "onleave" or "on_leave" => "On Leave",
            _ => "Active"
        };
    }

    private void ApplyStatusBadgeStyle(string? rawStatus, bool isDark, Color fallbackBg)
    {
        string normalized = NormalizeStatus(rawStatus);
        _dbStatus = normalized;
        lblStatus.Text = normalized;

        (Color textColor, Color badgeBg) = normalized switch
        {
            "Dropped" => (
                isDark ? Color.FromArgb("#FF6B6B") : Color.FromArgb("#B91C1C"),
                isDark ? Color.FromArgb("#3A151B") : Color.FromArgb("#FEE2E2")
            ),
            "Graduated" => (
                isDark ? Color.FromArgb("#87CEEB") : Color.FromArgb("#0369A1"),
                isDark ? Color.FromArgb("#152E47") : Color.FromArgb("#E0F2FE")
            ),
            "On Leave" => (
                isDark ? Color.FromArgb("#FFA500") : Color.FromArgb("#B45309"),
                isDark ? Color.FromArgb("#382810") : Color.FromArgb("#FEF3C7")
            ),
            _ => (
                isDark ? Color.FromArgb("#4ADE80") : Color.FromArgb("#15803D"),
                isDark ? Color.FromArgb("#133322") : Color.FromArgb("#DCFCE7")
            )
        };

        lblStatus.TextColor = textColor;
        StatusBadgeBorder.BackgroundColor = badgeBg != Colors.Transparent ? badgeBg : fallbackBg;
        StatusBadgeBorder.Stroke = new SolidColorBrush(textColor);
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        int age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;
        return age;
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

    private void OnBlocTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        string filtered = d2_SetUp.KeepSingleBlocLetter(e.NewTextValue);
        if (entry.Text != filtered)
            entry.Text = filtered;
    }

    private async Task LoadStudentExtraFromDbAsync(string idOrNumber)
    {
        try
        {
            await using var conn = new MySqlConnection(SisReadService.ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT username, address, status
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
                _dbStatus = !reader.IsDBNull(2) ? NormalizeStatus(reader.GetString(2)) : "Active";
            }
            else
            {
                _dbStatus = "Active";
            }
        }
        catch
        {
            _dbStatus = "Active";
        }
    }

    private async Task LoadStudentInfoAsync()
    {
        try
        {
            const string placeholder = "Not yet updated";

            string fullName = d2_SetUp.ToTitleCaseName(SessionService.FullName);
            string studentId = KeepStudentIdChars(SessionService.UserId);
            string? normalizedProgram = d2_SetUp.TryNormalizeProgram(SessionService.Program, out string normProg) ? normProg : null;
            string bloc = d2_SetUp.KeepSingleBlocLetter(SessionService.Bloc);
            string? yearLevel = d2_SetUp.NormalizeYearLevel(SessionService.YearLevel);
            string contact = KeepDigitsOnly(SessionService.Contact, 11);
            string gender = SessionService.Gender?.Trim() ?? string.Empty;
            string email = SessionService.Email?.Trim() ?? string.Empty;

            await LoadStudentExtraFromDbAsync(studentId);
            string address = _dbAddress.Trim();

            bool isDark = SessionService.IsDarkMode;
            var altCardBg = isDark ? Color.FromArgb("#15263A") : Color.FromArgb("#F1F5F9");
            ApplyStatusBadgeStyle(_dbStatus, isDark, altCardBg);

            _hasBirthDate = SessionService.BirthDate.HasValue && SessionService.BirthDate.Value.Year >= 10;
            string birthDateText = _hasBirthDate ? SessionService.BirthDate!.Value.ToString("MM/dd/yyyy") : "mm/dd/yyyy";
            string ageText = _hasBirthDate ? CalculateAge(SessionService.BirthDate!.Value).ToString() : string.Empty;

            lblHeaderName.Text = string.IsNullOrWhiteSpace(fullName) ? "Student" : fullName;
            lblHeaderSub.Text = string.IsNullOrWhiteSpace(studentId) ? "ID: Not yet updated" : $"ID: {studentId}";

            lblName.Text = string.IsNullOrWhiteSpace(fullName) ? placeholder : fullName;
            lblStudentId.Text = string.IsNullOrWhiteSpace(studentId) ? placeholder : studentId;
            lblProgram.Text = normalizedProgram ?? placeholder;
            lblBirthDate.Text = birthDateText;
            lblAge.Text = string.IsNullOrWhiteSpace(ageText) ? placeholder : ageText;
            lblBloc.Text = string.IsNullOrWhiteSpace(bloc) ? placeholder : bloc;
            lblYearLevel.Text = string.IsNullOrWhiteSpace(yearLevel) ? placeholder : yearLevel;
            lblGender.Text = string.IsNullOrWhiteSpace(gender) ? placeholder : gender;
            lblContact.Text = string.IsNullOrWhiteSpace(contact) ? placeholder : contact;
            lblEmail.Text = string.IsNullOrWhiteSpace(email) ? placeholder : email;
            lblAddress.Text = string.IsNullOrWhiteSpace(address) ? placeholder : address;

            _suppressDateSelected = true;
            if (_hasBirthDate)
            {
                dpBirthDate.Date = SessionService.BirthDate!.Value;
                txtBirthDateGhost.Text = SessionService.BirthDate!.Value.ToString("MM/dd/yyyy");
            }
            else
            {
                dpBirthDate.Date = DateTime.Today.AddYears(-18);
                txtBirthDateGhost.Text = string.Empty;
            }
            _suppressDateSelected = false;

            txtName.Text = fullName;
            txtStudentId.Text = studentId;
            pckProgram.SelectedItem = normalizedProgram;
            txtAge.Text = ageText;
            txtBloc.Text = bloc;
            pckYearLevel.SelectedItem = yearLevel;
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
        CardPhotoPlusBadge.IsVisible = editing;

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
        pckProgram.IsVisible = editing;
        editBirthDateContainer.IsVisible = editing;
        txtAge.IsVisible = editing;
        txtBloc.IsVisible = editing;
        pckYearLevel.IsVisible = editing;
        pckGender.IsVisible = editing;
        txtContact.IsVisible = editing;
        txtEmail.IsVisible = editing;
        txtAddress.IsVisible = editing;

        if (editing)
        {
            txtStudentId.Keyboard = Keyboard.Text;
            txtAge.IsReadOnly = false;
            txtContact.MaxLength = 11;
            txtName.Focus();
        }
    }

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
        if (_suppressDateSelected)
            return;

        if (e.NewDate is DateTime selected)
        {
            _hasBirthDate = true;
            txtBirthDateGhost.Text = selected.ToString("MM/dd/yyyy");
            txtAge.Text = CalculateAge(selected).ToString();
        }
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

    private async void OnCardPhotoTapped(object? sender, TappedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(SessionService.PhotoPath) && File.Exists(SessionService.PhotoPath))
        {
            string action = await DisplayActionSheetAsync("Student Photo", "Cancel", null, "Change Photo", "Remove Photo");
            if (action == "Remove Photo")
            {
                OnRemoveProfileImageClicked(sender, e);
                return;
            }
            if (action != "Change Photo")
                return;
        }

        await PickAndSaveProfilePhotoAsync();
    }

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
        string fullName = d2_SetUp.ToTitleCaseName(KeepLettersOnly(txtName.Text));
        string program = pckProgram.SelectedItem?.ToString() ?? string.Empty;

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

        if (!string.IsNullOrWhiteSpace(cleanAgeText) && int.TryParse(cleanAgeText, out int typedAge) && typedAge < 10)
        {
            await DisplayAlertAsync("Invalid Age", "Age below 10 is not acceptable.", "OK");
            return;
        }

        DateTime? birthDate = null;
        if (_hasBirthDate && dpBirthDate.Date.HasValue)
        {
            DateTime chosenDate = dpBirthDate.Date.Value;
            if (chosenDate.Year < 10 || chosenDate.Date > DateTime.Today || CalculateAge(chosenDate) < 10)
            {
                await DisplayAlertAsync("Invalid Birth Date", "Birth date/year cannot equate to an age below 10.", "OK");
                return;
            }
            birthDate = chosenDate;
        }

        int currentShownAge = birthDate.HasValue ? CalculateAge(birthDate.Value) : -1;
        if (int.TryParse(cleanAgeText, out int editedAge) && editedAge >= 10 && editedAge != currentShownAge)
        {
            DateTime baseDate = birthDate ?? DateTime.Today;
            int targetYear = Math.Max(10, DateTime.Today.Year - editedAge);
            int day = Math.Min(baseDate.Day, DateTime.DaysInMonth(targetYear, baseDate.Month));
            birthDate = new DateTime(targetYear, baseDate.Month, day);
            _hasBirthDate = true;
            _suppressDateSelected = true;
            dpBirthDate.Date = birthDate.Value;
            txtBirthDateGhost.Text = birthDate.Value.ToString("MM/dd/yyyy");
            _suppressDateSelected = false;
        }

        string rawYear = pckYearLevel.SelectedItem?.ToString() ?? string.Empty;
        string? normalizedYear = d2_SetUp.NormalizeYearLevel(rawYear);
        if (!string.IsNullOrWhiteSpace(rawYear) && normalizedYear is null)
        {
            await DisplayAlertAsync("Invalid Year Level", "Year level must be from 1st Year to 6th Year only.", "OK");
            return;
        }

        if (!string.IsNullOrEmpty(cleanContact) && cleanContact.Length != 11)
        {
            await DisplayAlertAsync("Validation", "Contact number must be 11 digits.", "OK");
            return;
        }

        string bloc = d2_SetUp.KeepSingleBlocLetter(txtBloc.Text);
        string yearLevel = normalizedYear ?? string.Empty;
        string gender = pckGender.SelectedItem?.ToString() ?? string.Empty;
        string email = txtEmail.Text?.Trim() ?? string.Empty;
        string address = txtAddress.Text?.Trim() ?? string.Empty;

        List<string> missingFields = [];
        if (string.IsNullOrWhiteSpace(program)) missingFields.Add("Program");
        if (!birthDate.HasValue) missingFields.Add("Birth Date");
        if (string.IsNullOrWhiteSpace(bloc)) missingFields.Add("Bloc");
        if (string.IsNullOrWhiteSpace(yearLevel)) missingFields.Add("Year Level");
        if (string.IsNullOrWhiteSpace(gender)) missingFields.Add("Gender");
        if (string.IsNullOrWhiteSpace(cleanContact)) missingFields.Add("Contact");
        if (string.IsNullOrWhiteSpace(email)) missingFields.Add("Email");
        if (string.IsNullOrWhiteSpace(address)) missingFields.Add("Address");

        if (missingFields.Count > 0)
        {
            // accept = "Later" (saves and exits edit mode), cancel = "OK" (stays in editing mode)
            bool saveLater = await DisplayAlertAsync(
                "Incomplete Information",
                $"If your details are not complete, your information may not fully reflect on the teacher side.\n\nMissing: {string.Join(", ", missingFields)}\n\nAre you sure you're done?",
                accept: "No",
                cancel: "Yes");

            if (!saveLater)
                return;
        }

        string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string firstName = nameParts.Length > 0 ? nameParts[0] : "Student";
        string lastName = nameParts.Length > 1 ? nameParts[^1] : "User";
        string? middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;

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
                Address = address,
                Status = NormalizeStatus(_dbStatus)
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

    private async void OnToggleProfileDrawerTapped(object? sender, TappedEventArgs e)
    {
        if (!ProfileDrawerOverlay.IsVisible)
        {
            ApplyStudentThemes();
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

    private async void OnViewProfileClicked(object? sender, EventArgs e)
    {
        await CloseProfileDrawerAsync();
        await LoadStudentExtraFromDbAsync(KeepStudentIdChars(SessionService.UserId));
        RefreshProfileVisuals();
        ApplyStudentThemes();
        ViewProfileOverlay.IsVisible = true;
    }

    private void OnCloseViewProfileClicked(object? sender, EventArgs e)
    {
        ViewProfileOverlay.IsVisible = false;
    }

    private async void OnAddProfileImageClicked(object? sender, EventArgs e)
    {
        await PickAndSaveProfilePhotoAsync();
    }

    private async Task PickAndSaveProfilePhotoAsync()
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
        ApplyStudentThemes();
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