using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BULi_MAUI;

public partial class d1_Records : ContentPage
{
    private readonly List<StudentRecord> _allStudents = new();
    private readonly ObservableCollection<StudentRecord> _filteredStudents = new();

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

    public d1_Records()
    {
        InitializeComponent();
        studentCollectionView.ItemsSource = _filteredStudents;
        pickerFilterStatus.SelectedIndex = 0;
        pickerFilterProgram.SelectedIndex = 0;
        pickerFilterYear.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ThemeService.ApplyTheme(this);

        base.OnAppearing();
        await LoadRecordsAsync();
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
        pickerFilterStatus.SelectedIndex = 0;
        pickerFilterProgram.SelectedIndex = 0;
        pickerFilterYear.SelectedIndex = 0;
        txtFilterBlock.Text = string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        string status = pickerFilterStatus.SelectedItem?.ToString() ?? "All";
        string program = pickerFilterProgram.SelectedItem?.ToString() ?? "All";
        string year = pickerFilterYear.SelectedItem?.ToString() ?? "All";
        string block = txtFilterBlock.Text?.Trim() ?? string.Empty;

        IEnumerable<StudentRecord> query = _allStudents;

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
        btnEditMode.BackgroundColor = IsEditMode ? Color.FromArgb("#FFA500") : Color.FromArgb("#15263A");
        btnEditMode.TextColor = IsEditMode ? Color.FromArgb("#0D1B2A") : Colors.White;

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

    private void OnOpenAddModalClicked(object? sender, EventArgs e)
    {
        var selected = _allStudents.Where(s => s.IsSelected).ToList();

        if (selected.Count > 1)
        {
            lblCrudTitle.Text = $"Batch Update ({selected.Count} Students)";
            txtStudentId.Text = "(Multiple Selected)";
            txtStudentName.Text = "(Multiple Selected)";
            txtStudentId.IsEnabled = false;
            txtStudentName.IsEnabled = false;
            txtBloc.Text = string.Empty;
            txtProgram.Text = string.Empty;
            btnAddStudent.IsEnabled = false;
            btnUpdateStudent.IsEnabled = true;
            btnDeleteStudent.IsEnabled = true;
        }
        else if (selected.Count == 1)
        {
            var student = selected[0];
            lblCrudTitle.Text = "Update Student Record";
            txtStudentId.Text = student.StudentNumber;
            txtStudentName.Text = student.DisplayName;
            txtBloc.Text = student.Block;
            txtProgram.Text = student.Program;
            txtStudentId.IsEnabled = true;
            txtStudentName.IsEnabled = true;
            btnAddStudent.IsEnabled = false;
            btnUpdateStudent.IsEnabled = true;
            btnDeleteStudent.IsEnabled = true;
        }
        else
        {
            lblCrudTitle.Text = "Add Student Record";
            txtStudentId.Text = string.Empty;
            txtStudentName.Text = string.Empty;
            txtBloc.Text = string.Empty;
            txtProgram.Text = string.Empty;
            txtStudentId.IsEnabled = true;
            txtStudentName.IsEnabled = true;
            btnAddStudent.IsEnabled = true;
            btnUpdateStudent.IsEnabled = false;
            btnDeleteStudent.IsEnabled = false;
        }

        CrudFormOverlay.IsVisible = true;
    }

    private void OnCloseCrudOverlayClicked(object? sender, EventArgs e)
    {
        CrudFormOverlay.IsVisible = false;
    }

    private async void OnAddStudentClicked(object? sender, EventArgs e)
    {
        string studentNumber = txtStudentId.Text?.Trim() ?? string.Empty;
        string fullName = txtStudentName.Text?.Trim() ?? string.Empty;
        string bloc = string.IsNullOrWhiteSpace(txtBloc.Text) ? "A" : txtBloc.Text.Trim().ToUpperInvariant();
        string program = string.IsNullOrWhiteSpace(txtProgram.Text) ? "BS Information Technology" : txtProgram.Text.Trim();

        if (string.IsNullOrWhiteSpace(studentNumber) || string.IsNullOrWhiteSpace(fullName))
        {
            await DisplayAlertAsync("Missing Fields", "Please enter both Student ID and Student Name.", "OK");
            return;
        }

        string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string firstName = nameParts.Length > 0 ? nameParts[0] : "Student";
        string lastName = nameParts.Length > 1 ? nameParts[^1] : "User";
        string? middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;

        string autoUsername = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}.{Random.Shared.Next(100, 999)}";

        btnAddStudent.IsEnabled = false;
        try
        {
            var newRecord = new StudentRecord
            {
                StudentNumber = studentNumber,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                YearLevel = "1st Year",
                Program = program,
                Block = bloc,
                Status = "Active"
            };

            bool added = await SisWriteService.AddStudentRecordAsync(newRecord, autoUsername, "student123");
            if (added)
            {
                CrudFormOverlay.IsVisible = false;
                await LoadRecordsAsync();
                await DisplayAlertAsync("Added", $"Student added!\nDefault login: {autoUsername} / student123", "OK");
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

                string[] nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                student.StudentNumber = txtStudentId.Text?.Trim() ?? student.StudentNumber;
                student.FirstName = nameParts.Length > 0 ? nameParts[0] : student.FirstName;
                student.LastName = nameParts.Length > 1 ? nameParts[^1] : student.LastName;
                student.MiddleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;
                student.Block = string.IsNullOrWhiteSpace(txtBloc.Text) ? student.Block : txtBloc.Text.Trim().ToUpperInvariant();
                student.Program = string.IsNullOrWhiteSpace(txtProgram.Text) ? student.Program : txtProgram.Text.Trim();

                await SisWriteService.UpdateStudentProfileAsync(student);
            }
            else
            {
                // Batch update Bloc and/or Program for all selected students
                foreach (var student in selected)
                {
                    if (!string.IsNullOrWhiteSpace(txtBloc.Text))
                        student.Block = txtBloc.Text.Trim().ToUpperInvariant();
                    if (!string.IsNullOrWhiteSpace(txtProgram.Text))
                        student.Program = txtProgram.Text.Trim();

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