using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BULi_MAUI;

public partial class d1_Records : ContentPage
{
    private readonly List<StudentRecord> _allStudents = new();
    private readonly ObservableCollection<StudentRecord> _filteredStudents = new();

    private static readonly HashSet<string> SurnamePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "dela", "del", "delos", "san", "santa", "sto"
    };

    private bool _isSanitizingStudentId;

    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode != value)
            {
                _isEditMode = value;
                OnPropertyChanged();
            }
        }
    }

    private Color _panelBackgroundColor = Color.FromArgb("#0D1B2A");
    public Color PanelBackgroundColor
    {
        get => _panelBackgroundColor;
        set { _panelBackgroundColor = value; OnPropertyChanged(); }
    }

    private Color _cardBackgroundColor = Color.FromArgb("#15263A");
    public Color CardBackgroundColor
    {
        get => _cardBackgroundColor;
        set { _cardBackgroundColor = value; OnPropertyChanged(); }
    }

    private Color _cardStrokeColor = Color.FromArgb("#22364D");
    public Color CardStrokeColor
    {
        get => _cardStrokeColor;
        set { _cardStrokeColor = value; OnPropertyChanged(); }
    }

    private Color _primaryTextColor = Colors.White;
    public Color PrimaryTextColor
    {
        get => _primaryTextColor;
        set { _primaryTextColor = value; OnPropertyChanged(); }
    }

    private Color _secondaryTextColor = Color.FromArgb("#8FA3B8");
    public Color SecondaryTextColor
    {
        get => _secondaryTextColor;
        set { _secondaryTextColor = value; OnPropertyChanged(); }
    }

    private Color _accentBlueColor = Color.FromArgb("#87CEEB");
    public Color AccentBlueColor
    {
        get => _accentBlueColor;
        set { _accentBlueColor = value; OnPropertyChanged(); }
    }

    public d1_Records()
    {
        InitializeComponent();
        studentCollectionView.ItemsSource = _filteredStudents;
        pickerFilterStatus.SelectedIndex = 0;
        pickerFilterProgram.SelectedIndex = 0;
        pickerFilterYear.SelectedIndex = 0;

        txtStudentId.Keyboard = Keyboard.Telephone;
        txtStudentId.TextChanged -= OnStudentIdTextChanged;
        txtStudentId.TextChanged += OnStudentIdTextChanged;
        txtStudentId.Loaded -= OnStudentIdLoaded;
        txtStudentId.Loaded += OnStudentIdLoaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplyRecordsTheme();
        await LoadRecordsAsync();
    }

    private void OnStudentIdLoaded(object? sender, EventArgs e)
    {
        txtStudentId.Keyboard = Keyboard.Telephone;
        txtStudentId.TextChanged -= OnStudentIdTextChanged;
        txtStudentId.TextChanged += OnStudentIdTextChanged;
    }

    private static string KeepDigitsAndDashes(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        return new string(raw.Where(c => char.IsDigit(c) || c == '-').ToArray());
    }

    private void OnStudentIdTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isSanitizingStudentId || sender is not Entry entry || !entry.IsEnabled)
            return;

        string current = e.NewTextValue ?? string.Empty;
        string sanitized = KeepDigitsAndDashes(current);

        if (current != sanitized)
        {
            _isSanitizingStudentId = true;
            entry.Text = sanitized;
            entry.CursorPosition = sanitized.Length;
            _isSanitizingStudentId = false;
        }
    }

    private void ApplyRecordsTheme()
    {
        bool isDark = SessionService.IsDarkMode;
        ThemeService.ApplyTheme(this);

        var panelBg = isDark ? Color.FromArgb("#0D1B2A") : Color.FromArgb("#FFFFFF");
        var cardBg = isDark ? Color.FromArgb("#15263A") : Color.FromArgb("#F1F5F9");
        var stroke = isDark ? Color.FromArgb("#22364D") : Color.FromArgb("#CBD5E1");
        var primaryText = isDark ? Colors.White : Color.FromArgb("#0F172A");
        var secondaryText = isDark ? Color.FromArgb("#8FA3B8") : Color.FromArgb("#475569");
        var labelText = isDark ? Color.FromArgb("#B0C4DE") : Color.FromArgb("#334155");
        var detailText = isDark ? Color.FromArgb("#DCE6F2") : Color.FromArgb("#1E293B");
        var placeholderText = isDark ? Color.FromArgb("#6E859E") : Color.FromArgb("#64748B");
        var linkBlue = isDark ? Color.FromArgb("#87CEEB") : Color.FromArgb("#0284C7");

        PanelBackgroundColor = panelBg;
        CardBackgroundColor = cardBg;
        CardStrokeColor = stroke;
        PrimaryTextColor = primaryText;
        SecondaryTextColor = secondaryText;
        AccentBlueColor = linkBlue;

        lblPageTitle.TextColor = primaryText;
        lblRecordSummary.TextColor = secondaryText;

        SearchBarBorder.BackgroundColor = cardBg;
        SearchBarBorder.Stroke = new SolidColorBrush(stroke);
        txtSearchRecords.TextColor = primaryText;
        txtSearchRecords.PlaceholderColor = placeholderText;
        btnClearSearch.TextColor = secondaryText;

        if (!IsEditMode)
        {
            btnEditMode.BackgroundColor = cardBg;
            btnEditMode.TextColor = primaryText;
            btnEditMode.BorderColor = stroke;
        }

        btnFilterToggle.BackgroundColor = cardBg;
        btnFilterToggle.TextColor = linkBlue;
        btnFilterToggle.BorderColor = stroke;

        btnSelectAll.BackgroundColor = cardBg;
        btnSelectAll.BorderColor = stroke;

        FilterPanel.BackgroundColor = panelBg;
        FilterPanel.Stroke = new SolidColorBrush(stroke);
        lblFilterHeader.TextColor = secondaryText;
        lblFilterStatus.TextColor = labelText;
        lblFilterProgram.TextColor = labelText;
        lblFilterYear.TextColor = labelText;
        lblFilterBlock.TextColor = labelText;

        Picker[] pickers = [pickerFilterStatus, pickerFilterProgram, pickerFilterYear, pickerStatus, pickerCrudProgram];
        foreach (var picker in pickers)
        {
            picker.BackgroundColor = cardBg;
            picker.TextColor = primaryText;
            picker.TitleColor = placeholderText;
        }

        Entry[] entries = [txtFilterBlock, txtStudentId, txtStudentName, txtBloc];
        foreach (var entry in entries)
        {
            entry.BackgroundColor = cardBg;
            entry.TextColor = primaryText;
            entry.PlaceholderColor = placeholderText;
        }

        btnResetFilters.BackgroundColor = cardBg;
        btnResetFilters.TextColor = labelText;

        StudentDetailCard.BackgroundColor = panelBg;
        StudentDetailCard.Stroke = new SolidColorBrush(stroke);
        DetailAvatarBorder.BackgroundColor = cardBg;
        DetailDivider.Color = stroke;
        lblDetailInitials.TextColor = primaryText;
        lblDetailName.TextColor = primaryText;
        lblDetailStudentId.TextColor = linkBlue;

        Label[] detailLabels =
        [
            lblDetailProgram,
            lblDetailYearBlock,
            lblDetailGenderBirthdate,
            lblDetailContact,
            lblDetailEmail,
            lblDetailAddress
        ];
        foreach (var lbl in detailLabels)
            lbl.TextColor = detailText;

        btnReturnToRecords.BackgroundColor = cardBg;
        btnReturnToRecords.TextColor = primaryText;
        btnReturnToRecords.BorderColor = stroke;

        CrudFormCard.BackgroundColor = panelBg;
        CrudFormCard.Stroke = new SolidColorBrush(stroke);
        lblCrudTitle.TextColor = primaryText;
        btnCloseCrud.TextColor = secondaryText;
        lblCrudSelectionHint.TextColor = secondaryText;

        Label[] crudLabels = [lblCrudStudentId, lblCrudStudentName, lblCrudBloc, lblCrudStatus, lblCrudProgram];
        foreach (var lbl in crudLabels)
            lbl.TextColor = labelText;
    }

    private async Task LoadRecordsAsync()
    {
        try
        {
            var rows = await SisReadService.GetStudentsAsync();
            foreach (var old in _allStudents)
                old.PropertyChanged -= OnStudentItemPropertyChanged;

            _allStudents.Clear();
            foreach (var student in rows)
            {
                student.PropertyChanged += OnStudentItemPropertyChanged;
                _allStudents.Add(student);
            }

            ApplyFilters();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Database Error", ex.Message, "OK");
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        btnClearSearch.IsVisible = !string.IsNullOrEmpty(e.NewTextValue);
        ApplyFilters();
    }

    private void OnClearSearchClicked(object? sender, EventArgs e)
    {
        txtSearchRecords.Text = string.Empty;
        btnClearSearch.IsVisible = false;
        ApplyFilters();
    }

    private void OnToggleFiltersClicked(object? sender, EventArgs e)
    {
        FilterPanel.IsVisible = !FilterPanel.IsVisible;
        btnFilterToggle.Text = FilterPanel.IsVisible ? "Filters ^" : "Filters v";
    }

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        ApplyFilters();
    }

    private void OnResetFiltersClicked(object? sender, EventArgs e)
    {
        txtSearchRecords.Text = string.Empty;
        btnClearSearch.IsVisible = false;
        pickerFilterStatus.SelectedIndex = 0;
        pickerFilterProgram.SelectedIndex = 0;
        pickerFilterYear.SelectedIndex = 0;
        txtFilterBlock.Text = string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        string search = txtSearchRecords.Text?.Trim() ?? string.Empty;
        string status = pickerFilterStatus.SelectedItem?.ToString() ?? "All";
        string program = pickerFilterProgram.SelectedItem?.ToString() ?? "All";
        string year = pickerFilterYear.SelectedItem?.ToString() ?? "All";
        string block = txtFilterBlock.Text?.Trim() ?? string.Empty;

        IEnumerable<StudentRecord> query = _allStudents;

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                (!string.IsNullOrWhiteSpace(s.DisplayName) && s.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.StudentNumber) && s.StudentNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.Program) && s.Program.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.YearLevel) && s.YearLevel.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.Block) && s.Block.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.Email) && s.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(s.ContactNumber) && s.ContactNumber.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (status != "All")
            query = query.Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase));

        if (program != "All")
            query = query.Where(s => s.Program.Equals(program, StringComparison.OrdinalIgnoreCase));

        if (year != "All")
            query = query.Where(s => s.YearLevel.Equals(year, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(block))
            query = query.Where(s => s.Block.Contains(block, StringComparison.OrdinalIgnoreCase));

        _filteredStudents.Clear();
        foreach (var item in query)
            _filteredStudents.Add(item);

        lblRecordSummary.Text = $"{_filteredStudents.Count} Records";
    }

    private void OnStudentCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not StudentRecord student)
            return;

        if (IsEditMode)
        {
            student.IsSelected = !student.IsSelected;
            return;
        }

        lblDetailInitials.Text = student.Initials;
        imgDetailPhoto.Source = student.PhotoSource;
        lblDetailName.Text = student.DisplayName;
        lblDetailStudentId.Text = $"Student ID: {student.StudentNumber}";
        lblDetailProgram.Text = $"Program: {student.Program}";
        lblDetailYearBlock.Text = $"Year & Block: {student.YearLevel} - Block {student.Block}";
        lblDetailGenderBirthdate.Text = $"Gender & Birthdate: {student.Gender} | {student.BirthdateDisplay}";
        lblDetailContact.Text = $"Contact Number: {(string.IsNullOrWhiteSpace(student.ContactNumber) ? "Not set" : student.ContactNumber)}";
        lblDetailEmail.Text = $"Email: {(string.IsNullOrWhiteSpace(student.Email) ? "Not set" : student.Email)}";
        lblDetailAddress.Text = $"Address: {(string.IsNullOrWhiteSpace(student.Address) ? "Not set" : student.Address)}";
        lblDetailStatus.Text = $"Status: {student.Status}";

        StudentDetailOverlay.IsVisible = true;
    }

    private void OnReturnToRecordsClicked(object? sender, EventArgs e)
    {
        StudentDetailOverlay.IsVisible = false;
    }

    private void OnToggleEditModeClicked(object? sender, EventArgs e)
    {
        IsEditMode = !IsEditMode;
        btnEditMode.Text = IsEditMode ? "Done" : "Edit";
        btnEditMode.BackgroundColor = IsEditMode ? Color.FromArgb("#FFA500") : CardBackgroundColor;
        btnEditMode.TextColor = IsEditMode ? Color.FromArgb("#0D1B2A") : PrimaryTextColor;
        btnEditMode.BorderColor = IsEditMode ? Color.FromArgb("#FFA500") : CardStrokeColor;

        btnAddNew.Text = IsEditMode ? "Update" : "+ Add New";
        btnSelectAll.IsVisible = IsEditMode;

        if (!IsEditMode)
        {
            foreach (var s in _allStudents)
                s.IsSelected = false;

            btnDeleteSelected.IsVisible = false;
        }
    }

    private void OnStudentItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StudentRecord.IsSelected))
        {
            int selectedCount = _allStudents.Count(s => s.IsSelected);
            btnDeleteSelected.IsVisible = IsEditMode && selectedCount > 0;
            btnDeleteSelected.Text = selectedCount > 0 ? $"Delete Selected ({selectedCount})" : "Delete Selected";
        }
    }

    private void OnSelectAllClicked(object? sender, EventArgs e)
    {
        bool anyUnselected = _filteredStudents.Any(s => !s.IsSelected);
        foreach (var s in _filteredStudents)
            s.IsSelected = anyUnselected;
    }

    private static string? NormalizeStatus(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return raw.Trim().ToLowerInvariant() switch
        {
            "active" => "Active",
            "dropped" => "Dropped",
            "on leave" => "On Leave",
            "graduated" => "Graduated",
            _ => null
        };
    }

    private static (string FirstName, string? MiddleName, string LastName, string BaseUsername) ParseStudentNameAndUsername(string fullName)
    {
        string[] parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return ("Student", null, "User", "user.student");

        if (parts.Length == 1)
        {
            string cleanSingle = CleanToken(parts[0]);
            string fallback = string.IsNullOrEmpty(cleanSingle) ? "student" : cleanSingle;
            return (parts[0], null, parts[0], $"{fallback}.{fallback}");
        }

        int lastStartIndex = parts.Length - 1;

        if (parts.Length >= 3)
        {
            string prevClean = CleanToken(parts[^2]);
            if (SurnamePrefixes.Contains(prevClean))
            {
                lastStartIndex = parts.Length - 2;
                if (parts.Length >= 4 && CleanToken(parts[^3]).Equals("de", StringComparison.OrdinalIgnoreCase))
                    lastStartIndex = parts.Length - 3;
            }
        }

        string firstName = parts[0];
        string? middleName = lastStartIndex > 1
            ? string.Join(" ", parts[1..lastStartIndex])
            : null;
        string lastName = string.Join(" ", parts[lastStartIndex..]);

        string givenCombined = string.IsNullOrWhiteSpace(middleName)
            ? CleanToken(firstName)
            : $"{CleanToken(firstName)}{CleanToken(middleName)}";
        string surnameCombined = CleanToken(lastName);

        if (string.IsNullOrEmpty(givenCombined)) givenCombined = "student";
        if (string.IsNullOrEmpty(surnameCombined)) surnameCombined = "user";

        return (firstName, middleName, lastName, $"{surnameCombined}.{givenCombined}");
    }

    private static string CleanToken(string input) =>
        new string(input.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private async void OnOpenAddModalClicked(object? sender, EventArgs e)
    {
        if (IsEditMode)
        {
            var selected = _allStudents.Where(s => s.IsSelected).ToList();
            if (selected.Count == 0)
            {
                await DisplayAlertAsync("No Selection", "Please select at least one student record to update.", "OK");
                return;
            }

            btnAddStudent.IsVisible = false;
            btnUpdateStudent.IsVisible = true;
            btnDeleteStudent.IsVisible = true;

            if (selected.Count > 1)
            {
                lblCrudTitle.Text = $"Batch Update ({selected.Count} Students)";
                txtStudentId.IsEnabled = false;
                txtStudentId.Text = "(Multiple Selected)";
                txtStudentName.Text = "(Multiple Selected)";
                txtStudentName.IsEnabled = false;
                txtBloc.Text = string.Empty;
                pickerCrudProgram.SelectedItem = null;
                pickerStatus.SelectedItem = null;
            }
            else
            {
                var student = selected[0];
                lblCrudTitle.Text = "Update Student Record";
                txtStudentId.IsEnabled = true;
                txtStudentId.Text = KeepDigitsAndDashes(student.StudentNumber);
                txtStudentName.Text = student.DisplayName;
                txtBloc.Text = student.Block;
                pickerCrudProgram.SelectedItem = d2_SetUp.TryNormalizeProgram(student.Program, out string normProg) ? normProg : null;
                pickerStatus.SelectedItem = NormalizeStatus(student.Status) ?? "Active";
                txtStudentName.IsEnabled = true;
            }
        }
        else
        {
            btnAddStudent.IsVisible = true;
            btnUpdateStudent.IsVisible = false;
            btnDeleteStudent.IsVisible = false;

            lblCrudTitle.Text = "Add Student Record";
            txtStudentId.IsEnabled = true;
            txtStudentId.Text = string.Empty;
            txtStudentName.Text = string.Empty;
            txtBloc.Text = string.Empty;
            pickerCrudProgram.SelectedItem = null;
            pickerStatus.SelectedItem = "Active";
            txtStudentName.IsEnabled = true;
        }

        CrudFormOverlay.IsVisible = true;
    }

    private void OnCloseCrudOverlayClicked(object? sender, EventArgs e)
    {
        CrudFormOverlay.IsVisible = false;
    }

    private async void OnAddStudentClicked(object? sender, EventArgs e)
    {
        string studentNumber = KeepDigitsAndDashes(txtStudentId.Text);
        string fullName = txtStudentName.Text?.Trim() ?? string.Empty;
        string bloc = d2_SetUp.KeepSingleBlocLetter(txtBloc.Text);
        string program = pickerCrudProgram.SelectedItem?.ToString() ?? string.Empty;
        string status = pickerStatus.SelectedItem?.ToString() ?? "Active";

        if (string.IsNullOrWhiteSpace(studentNumber) || string.IsNullOrWhiteSpace(fullName))
        {
            await DisplayAlertAsync("Missing Fields", "Please enter both Student ID (numbers and dashes only) and Student Name.", "OK");
            return;
        }

        var (firstName, middleName, lastName, baseUsername) = ParseStudentNameAndUsername(fullName);
        const string defaultPassword = "student123";

        btnAddStudent.IsEnabled = false;
        try
        {
            var newRecord = new StudentRecord
            {
                StudentNumber = studentNumber,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                YearLevel = string.Empty,
                Program = program,
                Block = bloc,
                Gender = string.Empty,
                ContactNumber = string.Empty,
                Email = null,
                Address = null,
                Birthdate = null,
                Status = status
            };

            var result = await SisWriteService.AddStudentWithAutoUsernameAsync(newRecord, baseUsername, defaultPassword);
            if (result.Success)
            {
                CrudFormOverlay.IsVisible = false;
                await LoadRecordsAsync();
                await DisplayAlertAsync(
                    "Account Created",
                    $"Student added!\nUsername: {result.Username}\nPassword: {defaultPassword}",
                    "OK");
            }
            else
            {
                await DisplayAlertAsync("Insert Error", result.ErrorMessage ?? "Could not create student record.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Insert Error", ex.Message, "OK");
        }
        finally
        {
            btnAddStudent.IsEnabled = true;
        }
    }

    private async void OnUpdateStudentClicked(object? sender, EventArgs e)
    {
        var selected = _allStudents.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
            return;

        string? chosenProgram = pickerCrudProgram.SelectedItem?.ToString();
        string? chosenStatus = pickerStatus.SelectedItem?.ToString();

        btnUpdateStudent.IsEnabled = false;
        try
        {
            if (selected.Count == 1)
            {
                var student = selected[0];
                string fullName = txtStudentName.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    await DisplayAlertAsync("Missing Fields", "Student Name cannot be empty.", "OK");
                    return;
                }

                string cleanId = KeepDigitsAndDashes(txtStudentId.Text);
                string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                student.StudentNumber = string.IsNullOrWhiteSpace(cleanId) ? student.StudentNumber : cleanId;
                student.FirstName = nameParts.Length > 0 ? nameParts[0] : student.FirstName;
                student.LastName = nameParts.Length > 1 ? nameParts[^1] : student.LastName;
                student.MiddleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;
                student.FullName = string.Empty;
                student.Block = string.IsNullOrWhiteSpace(txtBloc.Text) ? student.Block : d2_SetUp.KeepSingleBlocLetter(txtBloc.Text);
                if (!string.IsNullOrWhiteSpace(chosenProgram))
                    student.Program = chosenProgram;
                if (!string.IsNullOrWhiteSpace(chosenStatus))
                    student.Status = chosenStatus;

                await SisWriteService.UpdateStudentProfileAsync(student);
            }
            else
            {
                foreach (var student in selected)
                {
                    if (!string.IsNullOrWhiteSpace(txtBloc.Text))
                        student.Block = d2_SetUp.KeepSingleBlocLetter(txtBloc.Text);
                    if (!string.IsNullOrWhiteSpace(chosenProgram))
                        student.Program = chosenProgram;
                    if (!string.IsNullOrWhiteSpace(chosenStatus))
                        student.Status = chosenStatus;

                    await SisWriteService.UpdateStudentProfileAsync(student);
                }
            }

            CrudFormOverlay.IsVisible = false;
            await LoadRecordsAsync();
            await DisplayAlertAsync("Updated", "Student record(s) updated in database!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Update Error", ex.Message, "OK");
        }
        finally
        {
            btnUpdateStudent.IsEnabled = true;
        }
    }

    private async void OnDeleteStudentClicked(object? sender, EventArgs e)
    {
        var selected = _allStudents.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
            return;

        bool confirm = await DisplayAlertAsync("Delete Record", $"Delete {selected.Count} selected student record(s)?", "Delete", "Cancel");
        if (!confirm)
            return;

        btnDeleteStudent.IsEnabled = false;
        try
        {
            foreach (var student in selected)
                await SisWriteService.DeleteStudentAsync(student.StudentId);

            CrudFormOverlay.IsVisible = false;
            await LoadRecordsAsync();
            await DisplayAlertAsync("Deleted", "Record(s) removed from database.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Delete Error", ex.Message, "OK");
        }
        finally
        {
            btnDeleteStudent.IsEnabled = true;
        }
    }

    private async void OnDeleteSelectedClicked(object? sender, EventArgs e)
    {
        var selected = _allStudents.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
            return;

        bool confirm = await DisplayAlertAsync("Delete Selected", $"Are you sure you want to delete {selected.Count} student(s)?", "Delete", "Cancel");
        if (!confirm)
            return;

        try
        {
            foreach (var student in selected)
                await SisWriteService.DeleteStudentAsync(student.StudentId);

            btnDeleteSelected.IsVisible = false;
            await LoadRecordsAsync();
            await DisplayAlertAsync("Deleted", $"{selected.Count} student record(s) deleted.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Delete Error", ex.Message, "OK");
        }
    }
}