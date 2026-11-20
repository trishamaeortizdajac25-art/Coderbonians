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
        await DisplayAlertAsync("Add Student", "Backend INSERT hook ready.", "OK");
        CrudFormOverlay.IsVisible = false;
    }

    private async void OnUpdateStudentClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Update Student", "Backend UPDATE hook ready.", "OK");
        CrudFormOverlay.IsVisible = false;
    }

    private async void OnDeleteStudentClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Delete Student", "Backend DELETE hook ready.", "OK");
        CrudFormOverlay.IsVisible = false;
    }

    private async void OnDeleteSelectedClicked(object? sender, EventArgs e)
    {
        int count = _allStudents.Count(s => s.IsSelected);
        if (count == 0)
            return;

        await DisplayAlertAsync("Delete Selected", $"Backend batch DELETE hook ready for {count} student(s).", "OK");
    }
}