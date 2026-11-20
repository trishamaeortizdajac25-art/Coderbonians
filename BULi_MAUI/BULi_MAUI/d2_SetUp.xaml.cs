namespace BULi_MAUI;

public partial class d2_SetUp : ContentPage
{
    private int _step = 1;
    private const int TotalSteps = 3;
    private const int MaxContactLength = 13;
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
            StudentIdEntry.Text = SessionService.UserId;
            ProgramEntry.Text = SessionService.Program;
            YearPicker.SelectedItem = SessionService.YearLevel;
            BlocEntry.Text = SessionService.Bloc;
            GenderPicker.SelectedItem = SessionService.Gender;
            ContactEntry.Text = SessionService.Contact;
            EmailEntry.Text = SessionService.Email;
            BirthDatePicker.Date = SessionService.BirthDate;

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
        // Allow only digits (0-9) and hyphen (-)
        string filtered = new string(input.Where(c => char.IsDigit(c) || c == '-').ToArray());

        if (filtered != input)
        {
            _isFilteringInput = true;
            StudentIdEntry.Text = filtered;
            StudentIdEntry.CursorPosition = filtered.Length;
            _isFilteringInput = false;

            ShowFieldError(StudentIdBorder, StudentIdErrorLabel, "Letters are not allowed. Use numbers and '-' only.");
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
        bool hadInvalidChars = false;
        bool exceededLength = false;

        // Allow digits (0-9) and a leading '+' only
        var chars = new List<char>(MaxContactLength);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsDigit(c) || (c == '+' && i == 0 && chars.Count == 0))
            {
                if (chars.Count < MaxContactLength)
                    chars.Add(c);
                else
                    exceededLength = true;
            }
            else
            {
                hadInvalidChars = true;
            }
        }

        string filtered = new string(chars.ToArray());

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
        else if (exceededLength || filtered.Length == MaxContactLength)
        {
            ShowFieldError(ContactBorder, ContactErrorLabel, "Maximum of 13 digits reached.");
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
        SessionService.LoginAsStudent(
            string.IsNullOrWhiteSpace(StudentIdEntry.Text) ? "2026-0001" : StudentIdEntry.Text.Trim(),
            string.IsNullOrWhiteSpace(NameEntry.Text) ? "Student User" : NameEntry.Text.Trim());

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
            if (string.IsNullOrWhiteSpace(id) || !id.Any(char.IsDigit))
            {
                ShowFieldError(StudentIdBorder, StudentIdErrorLabel, "Please enter a valid numeric Student ID.");
                return false;
            }
        }

        if (_step == 3)
        {
            string contact = ContactEntry.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(contact) && (contact.Length < 11 || contact.Length > MaxContactLength))
            {
                ShowFieldError(ContactBorder, ContactErrorLabel, "Contact number must be 11 to 13 digits.");
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
        SessionService.LoginAsStudent(StudentIdEntry.Text.Trim(), NameEntry.Text.Trim());
        SessionService.Program = string.IsNullOrWhiteSpace(ProgramEntry.Text) ? "BS Computer Science" : ProgramEntry.Text.Trim();
        SessionService.YearLevel = YearPicker.SelectedItem?.ToString() ?? "1st Year";
        SessionService.Bloc = string.IsNullOrWhiteSpace(BlocEntry.Text) ? "A" : BlocEntry.Text.Trim().ToUpperInvariant();
        SessionService.BirthDate = BirthDatePicker.Date ?? DateTime.Today.AddYears(-18);
        SessionService.Gender = GenderPicker.SelectedItem?.ToString() ?? "Prefer not to say";
        SessionService.Contact = ContactEntry.Text?.Trim() ?? string.Empty;
        SessionService.Email = EmailEntry.Text?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(_selectedPhotoPath))
            SessionService.PhotoPath = _selectedPhotoPath;

        await DisplayAlertAsync("Success", "Student profile saved!", "OK");
        await Shell.Current.GoToAsync("//StudentMain");
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