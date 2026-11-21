namespace BULi_MAUI;

public partial class d2_SetUp : ContentPage
{
    private int _step = 1;
    private const int TotalSteps = 3;
    private const int MaxContactLength = 11;
    private string _selectedPhotoPath = string.Empty;
    private bool _isFilteringInput;

    private readonly string[] _titles = { "Personal details", "Academic details", "Contact details" };
    private readonly string[] _subtitles =
    {
        "Tell us a bit about yourself.",
        "Your school information.",
        "How we can reach you."
    };

    public d2_SetUp()
    {
        InitializeComponent();
        BirthDatePicker.MaximumDate = DateTime.Today;

        if (SessionService.IsLoggedIn && SessionService.Role == "Student")
        {
            NameEntry.Text = SessionService.FullName;
            // Only pre-fill StudentIdEntry if it isn't the internal auto-increment DB id
            string cachedDbId = Preferences.Default.Get("StudentSessionDbId", string.Empty);
            StudentIdEntry.Text = SessionService.UserId == cachedDbId ? string.Empty : SessionService.UserId;
            ProgramEntry.Text = SessionService.Program;
            YearPicker.SelectedItem = string.IsNullOrWhiteSpace(SessionService.YearLevel) ? null : SessionService.YearLevel;
            BlocEntry.Text = SessionService.Bloc;
            GenderPicker.SelectedItem = string.IsNullOrWhiteSpace(SessionService.Gender) ? null : SessionService.Gender;
            ContactEntry.Text = SessionService.Contact;
            EmailEntry.Text = SessionService.Email;
            BirthDatePicker.Date = SessionService.BirthDate ?? DateTime.Today.AddYears(-18);

            if (!string.IsNullOrWhiteSpace(SessionService.PhotoPath) && File.Exists(SessionService.PhotoPath))
            {
                _selectedPhotoPath = SessionService.PhotoPath;
                ProfileImage.Source = ImageSource.FromFile(_selectedPhotoPath);
            }
        }
        else
        {
            BirthDatePicker.Date = DateTime.Today.AddYears(-18);
        }

        UpdateAge(BirthDatePicker.Date);
    }

    // --- REAL-TIME INPUT FILTERING & RED INLINE ERROR MESSAGES ---

    private void OnStudentIdTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isFilteringInput)
            return;

        string input = e.NewTextValue ?? string.Empty;
        string filtered = new string(input.Where(char.IsDigit).ToArray());

        if (filtered != input)
        {
            _isFilteringInput = true;
            StudentIdEntry.Text = filtered;
            StudentIdEntry.CursorPosition = filtered.Length;
            _isFilteringInput = false;

            ShowFieldError(StudentIdBorder, StudentIdErrorLabel, "Numbers only (0-9).");
        }
        else
        {
            ClearFieldError(StudentIdBorder, StudentIdErrorLabel);
        }
    }

    private void OnContactTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isFilteringInput)
            return;

        string input = e.NewTextValue ?? string.Empty;
        bool hadInvalidChars = input.Any(c => !char.IsDigit(c));
        string digits = new string(input.Where(char.IsDigit).ToArray());
        bool exceededLength = digits.Length > MaxContactLength;
        string filtered = exceededLength ? digits[..MaxContactLength] : digits;

        if (filtered != input)
        {
            _isFilteringInput = true;
            ContactEntry.Text = filtered;
            ContactEntry.CursorPosition = filtered.Length;
            _isFilteringInput = false;
        }

        if (hadInvalidChars)
        {
            ShowFieldError(ContactBorder, ContactErrorLabel, "Letters and symbols are not allowed. Numbers only.");
        }
        else if (exceededLength)
        {
            ShowFieldError(ContactBorder, ContactErrorLabel, "Maximum of 11 digits only.");
        }
        else
        {
            ClearFieldError(ContactBorder, ContactErrorLabel);
        }
    }

    private static void ShowFieldError(Border border, Label errorLabel, string message)
    {
        border.Stroke = Color.FromArgb("#FF6B6B");
        errorLabel.Text = message;
        errorLabel.IsVisible = true;
    }

    private static void ClearFieldError(Border border, Label errorLabel)
    {
        border.Stroke = Color.FromArgb("#22364D");
        errorLabel.Text = string.Empty;
        errorLabel.IsVisible = false;
    }

    // --- STEP NAVIGATION & VALIDATION ---

    private async Task ShowStep()
    {
        Step1.IsVisible = _step == 1;
        Step2.IsVisible = _step == 2;
        Step3.IsVisible = _step == 3;

        StepLabel.Text = $"STEP {_step} OF {TotalSteps}";
        TitleLabel.Text = _titles[_step - 1];
        SubtitleLabel.Text = _subtitles[_step - 1];
        BackButton.IsVisible = _step > 1;
        NextButton.Text = _step == TotalSteps ? "Finish" : "Continue";

        await Progress.ProgressTo((double)_step / TotalSteps, 350, Easing.CubicOut);
    }

    private async void OnNextClicked(object? sender, EventArgs e)
    {
        if (!await Validate())
            return;

        if (_step < TotalSteps)
        {
            _step++;
            await ShowStep();
        }
        else
        {
            await SaveProfile();
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (_step > 1)
        {
            _step--;
            await ShowStep();
        }
    }

    private async void OnSkipTapped(object? sender, TappedEventArgs e)
    {
        // Keep everything empty so e2_stdMainPage displays "Not yet updated"
        string cleanId = new string((StudentIdEntry.Text ?? string.Empty).Where(char.IsDigit).ToArray());
        string fullName = NameEntry.Text?.Trim() ?? string.Empty;

        SessionService.LoginAsStudent(cleanId, fullName);
        SessionService.Program = string.Empty;
        SessionService.YearLevel = string.Empty;
        SessionService.Bloc = string.Empty;
        SessionService.Gender = string.Empty;
        SessionService.Contact = string.Empty;
        SessionService.Email = string.Empty;
        SessionService.BirthDate = null;

        await Shell.Current.GoToAsync("//StudentMain");
    }

    private async Task<bool> Validate()
    {
        if (_step == 1 && string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            await DisplayAlertAsync("Missing information", "Please enter your full name.", "OK");
            return false;
        }

        if (_step == 2)
        {
            string id = StudentIdEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id) || !id.All(char.IsDigit))
            {
                ShowFieldError(StudentIdBorder, StudentIdErrorLabel, "Please enter a valid numeric Student ID.");
                return false;
            }
        }

        if (_step == 3)
        {
            string contact = ContactEntry.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(contact) && contact.Length != 11)
            {
                ShowFieldError(ContactBorder, ContactErrorLabel, "Contact number must be 11 digits.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(EmailEntry.Text) && !EmailEntry.Text.Contains('@'))
            {
                await DisplayAlertAsync("Invalid email", "Please enter a valid email address.", "OK");
                return false;
            }
        }

        return true;
    }

    private async Task SaveProfile()
    {
        NextButton.IsEnabled = false;
        try
        {
            string fullName = NameEntry.Text?.Trim() ?? string.Empty;
            string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string firstName = nameParts.Length > 0 ? nameParts[0] : "Student";
            string lastName = nameParts.Length > 1 ? nameParts[^1] : "User";
            string? middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;

            string studentNumber = new string((StudentIdEntry.Text ?? string.Empty).Where(char.IsDigit).ToArray());
            string program = ProgramEntry.Text?.Trim() ?? string.Empty;
            string yearLevel = YearPicker.SelectedItem?.ToString() ?? string.Empty;
            string bloc = BlocEntry.Text?.Trim().ToUpperInvariant() ?? string.Empty;
            DateTime? birthDate = BirthDatePicker.Date;
            string gender = GenderPicker.SelectedItem?.ToString() ?? string.Empty;
            string contact = ContactEntry.Text?.Trim() ?? string.Empty;
            string email = EmailEntry.Text?.Trim() ?? string.Empty;
            string address = AddressEntry.Text?.Trim() ?? string.Empty;

            string dbIdRaw = Preferences.Default.Get("StudentSessionDbId", SessionService.UserId);
            uint.TryParse(dbIdRaw, out uint dbStudentId);

            var record = new StudentRecord
            {
                StudentId = dbStudentId,
                StudentNumber = studentNumber,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                Gender = gender,
                Birthdate = birthDate,
                YearLevel = yearLevel,
                Program = program,
                Block = bloc,
                ContactNumber = contact,
                Email = email,
                Address = address
            };

            bool updated = await SisWriteService.UpdateStudentProfileAsync(record, dbIdRaw);
            if (!updated)
            {
                await DisplayAlertAsync("Save Error", "Could not save profile to the database. Please try again.", "OK");
                return;
            }

            // Set SessionService.UserId to the StudentNumber so e2_stdMainPage displays and updates cleanly
            SessionService.LoginAsStudent(studentNumber, fullName);
            SessionService.Program = program;
            SessionService.YearLevel = yearLevel;
            SessionService.Bloc = bloc;
            SessionService.BirthDate = birthDate;
            SessionService.Gender = gender;
            SessionService.Contact = contact;
            SessionService.Email = email;

            if (!string.IsNullOrWhiteSpace(_selectedPhotoPath))
                SessionService.PhotoPath = _selectedPhotoPath;

            await DisplayAlertAsync("Success", "Student profile saved!", "OK");
            await Shell.Current.GoToAsync("//StudentMain");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Save Error", ex.Message, "OK");
        }
        finally
        {
            NextButton.IsEnabled = true;
        }
    }

    private void OnBirthDateSelected(object? sender, DateChangedEventArgs e) => UpdateAge(e.NewDate);

    private void UpdateAge(DateTime? birthDate)
    {
        if (birthDate is null)
            return;

        var today = DateTime.Today;
        int age = today.Year - birthDate.Value.Year;
        if (birthDate.Value.Date > today.AddYears(-age))
            age--;

        AgeLabel.Text = $"Age: {age}";
    }

    private async void OnPhotoTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                SelectionLimit = 1
            });

            var photo = photos?.FirstOrDefault();
            if (photo is null)
                return;

            var destPath = Path.Combine(FileSystem.AppDataDirectory, $"profile_{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}");
            using (var sourceStream = await photo.OpenReadAsync())
            using (var destStream = File.Create(destPath))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            _selectedPhotoPath = destPath;
            ProfileImage.Source = ImageSource.FromFile(destPath);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Photo", "Couldn't open your photos.", "OK");
        }
    }
}