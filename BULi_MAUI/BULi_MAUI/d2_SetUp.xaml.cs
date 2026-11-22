using System.Globalization;

namespace BULi_MAUI;

public partial class d2_SetUp : ContentPage
{
    private int _step = 1;
    private const int TotalSteps = 3;
    private const int MaxContactLength = 11;
    private string _selectedPhotoPath = string.Empty;
    private bool _hasBirthDateSelected;
    private bool _isFilteringInput;
    private bool _suppressDateSelected;

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
        _suppressDateSelected = true;
        BirthDatePicker.MaximumDate = DateTime.Today;

        if (SessionService.IsLoggedIn && SessionService.Role == "Student")
        {
            NameEntry.Text = ToTitleCaseName(SessionService.FullName);
            string cachedDbId = Preferences.Default.Get("StudentSessionDbId", string.Empty);
            StudentIdEntry.Text = SessionService.UserId == cachedDbId ? string.Empty : SessionService.UserId;
            ProgramPicker.SelectedItem = TryNormalizeProgram(SessionService.Program, out string normProg) ? normProg : null;
            YearPicker.SelectedItem = NormalizeYearLevel(SessionService.YearLevel);
            BlocEntry.Text = KeepSingleBlocLetter(SessionService.Bloc);
            GenderPicker.SelectedItem = string.IsNullOrWhiteSpace(SessionService.Gender) ? null : SessionService.Gender;
            ContactEntry.Text = SessionService.Contact;
            EmailEntry.Text = SessionService.Email;

            if (SessionService.BirthDate.HasValue && SessionService.BirthDate.Value.Year >= 10)
            {
                _hasBirthDateSelected = true;
                BirthDatePicker.Date = SessionService.BirthDate.Value;
                BirthDateGhostEntry.Text = SessionService.BirthDate.Value.ToString("MM/dd/yyyy");
                UpdateAge(SessionService.BirthDate.Value);
            }
            else
            {
                ResetBirthDateDefault();
            }

            if (!string.IsNullOrWhiteSpace(SessionService.PhotoPath) && File.Exists(SessionService.PhotoPath))
            {
                _selectedPhotoPath = SessionService.PhotoPath;
                SetAvatarPhoto(_selectedPhotoPath);
            }
            else
            {
                SetAvatarPhoto(null);
            }
        }
        else
        {
            ResetBirthDateDefault();
            SetAvatarPhoto(null);
        }

        RefreshAvatarInitials();
        _suppressDateSelected = false;
    }

    private void ResetBirthDateDefault()
    {
        _hasBirthDateSelected = false;
        BirthDatePicker.Date = DateTime.Today.AddYears(-18);
        BirthDateGhostEntry.Text = string.Empty;
        AgeLabel.Text = string.Empty;
        AgeLabel.IsVisible = false;
    }

    internal static string KeepSingleBlocLetter(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        char? letter = raw.FirstOrDefault(char.IsLetter);
        return letter.HasValue ? char.ToUpperInvariant(letter.Value).ToString() : string.Empty;
    }

    internal static string? NormalizeYearLevel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string cleaned = raw.Trim().ToLowerInvariant();
        return cleaned switch
        {
            "1" or "1st" or "1st year" or "first year" => "1st Year",
            "2" or "2nd" or "2nd year" or "second year" => "2nd Year",
            "3" or "3rd" or "3rd year" or "third year" => "3rd Year",
            "4" or "4th" or "4th year" or "fourth year" => "4th Year",
            "5" or "5th" or "5th year" or "fifth year" => "5th Year",
            "6" or "6th" or "6th year" or "sixth year" => "6th Year",
            _ => null
        };
    }

    internal static string ToTitleCaseName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string cleaned = string.Join(" ", raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
    }

    internal static bool TryNormalizeProgram(string? raw, out string normalizedProgram)
    {
        normalizedProgram = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string p = string.Join(" ", raw.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (p is "bsit" or "it" or "info tech" or "information technology" or "bs information technology" or "bs info tech")
        {
            normalizedProgram = "BS Information Technology";
            return true;
        }
        if (p is "bscs" or "cs" or "comsci" or "comp sci" or "computer science" or "bs computer science" or "bs comsci" or "bs comp sci")
        {
            normalizedProgram = "BS Computer Science";
            return true;
        }
        if (p is "bsmet" or "met" or "meteorology" or "bs meteorology")
        {
            normalizedProgram = "BS Meteorology";
            return true;
        }
        if (p is "bsbio" or "bio" or "biologi" or "biology" or "bs biology" or "bs biologi")
        {
            normalizedProgram = "BS Biology";
            return true;
        }
        if (p is "bschem" or "chem" or "chemistry" or "bs chemistry")
        {
            normalizedProgram = "BS Chemistry";
            return true;
        }

        return false;
    }

    internal static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        int age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age))
            age--;
        return age;
    }

    private void OnBlocTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isFilteringInput)
            return;

        string filtered = KeepSingleBlocLetter(e.NewTextValue);
        if (BlocEntry.Text != filtered)
        {
            _isFilteringInput = true;
            BlocEntry.Text = filtered;
            BlocEntry.CursorPosition = filtered.Length;
            _isFilteringInput = false;
        }
    }

    private void RefreshAvatarInitials()
    {
        AvatarInitialsLabel.Text = SessionService.GetInitials(NameEntry.Text);
    }

    private void SetAvatarPhoto(string? path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            ProfileImage.Source = ImageSource.FromFile(path);
            ProfileImage.IsVisible = true;
            AvatarInitialsLabel.IsVisible = false;
        }
        else
        {
            ProfileImage.Source = null;
            ProfileImage.IsVisible = false;
            AvatarInitialsLabel.IsVisible = true;
        }
    }

    private void OnNameTextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshAvatarInitials();
    }

    private void OnStudentIdTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isFilteringInput)
            return;

        string input = e.NewTextValue ?? string.Empty;
        string filtered = new string(input.Where(c => char.IsDigit(c) || c == '-').ToArray());

        if (filtered != input)
        {
            _isFilteringInput = true;
            StudentIdEntry.Text = filtered;
            StudentIdEntry.CursorPosition = filtered.Length;
            _isFilteringInput = false;

            ShowFieldError(StudentIdBorder, StudentIdErrorLabel, "Numbers and '-' only.");
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
        string cleanId = new string((StudentIdEntry.Text ?? string.Empty).Where(c => char.IsDigit(c) || c == '-').ToArray());
        string fullName = ToTitleCaseName(NameEntry.Text);

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

    private bool TryGetValidatedBirthDate(out DateTime? birthDate, out string? errorMessage)
    {
        birthDate = null;
        errorMessage = null;

        if (_hasBirthDateSelected && BirthDatePicker.Date.HasValue)
        {
            DateTime chosen = BirthDatePicker.Date.Value;
            if (chosen.Year < 10 || chosen.Date > DateTime.Today || CalculateAge(chosen) < 10)
            {
                errorMessage = "Birth date/year cannot equate to an age below 10.";
                return false;
            }

            birthDate = chosen;
        }

        return true;
    }

    private async Task<bool> Validate()
    {
        if (_step == 1)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text))
            {
                await DisplayAlertAsync("Missing information", "Please enter your full name.", "OK");
                return false;
            }

            if (!TryGetValidatedBirthDate(out _, out string? dateError))
            {
                await DisplayAlertAsync("Invalid birth date", dateError!, "OK");
                return false;
            }
        }

        if (_step == 2)
        {
            string id = StudentIdEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id) || !id.Any(char.IsDigit) || !id.All(c => char.IsDigit(c) || c == '-'))
            {
                ShowFieldError(StudentIdBorder, StudentIdErrorLabel, "Please enter a valid Student ID (numbers and '-' only).");
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
            if (!TryGetValidatedBirthDate(out DateTime? birthDate, out string? dateError))
            {
                await DisplayAlertAsync("Invalid birth date", dateError!, "OK");
                return;
            }

            string fullName = ToTitleCaseName(NameEntry.Text);
            string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string firstName = nameParts.Length > 0 ? nameParts[0] : "Student";
            string lastName = nameParts.Length > 1 ? nameParts[^1] : "User";
            string? middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;

            string studentNumber = new string((StudentIdEntry.Text ?? string.Empty).Where(c => char.IsDigit(c) || c == '-').ToArray());
            string program = ProgramPicker.SelectedItem?.ToString() ?? string.Empty;
            string yearLevel = YearPicker.SelectedItem?.ToString() ?? string.Empty;
            string bloc = KeepSingleBlocLetter(BlocEntry.Text);
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

    private void OnBirthDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (_suppressDateSelected)
            return;

        if (e.NewDate is DateTime selected)
        {
            _hasBirthDateSelected = true;
            BirthDateGhostEntry.Text = selected.ToString("MM/dd/yyyy");
            UpdateAge(selected);
        }
    }

    private void UpdateAge(DateTime? birthDate)
    {
        if (!_hasBirthDateSelected || birthDate is null)
        {
            AgeLabel.Text = string.Empty;
            AgeLabel.IsVisible = false;
            return;
        }

        int age = CalculateAge(birthDate.Value);
        AgeLabel.Text = $"Age: {age}";
        AgeLabel.IsVisible = true;
    }

    private async void OnPhotoTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_selectedPhotoPath))
            {
                string action = await DisplayActionSheetAsync("Profile Photo", "Cancel", null, "Change Photo", "Remove Photo");
                if (action == "Remove Photo")
                {
                    _selectedPhotoPath = string.Empty;
                    SessionService.PhotoPath = string.Empty;
                    SetAvatarPhoto(null);
                    RefreshAvatarInitials();
                    return;
                }
                if (action != "Change Photo")
                    return;
            }

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
            SessionService.PhotoPath = destPath;
            SetAvatarPhoto(destPath);
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Photo", "Couldn't open your photos.", "OK");
        }
    }
}