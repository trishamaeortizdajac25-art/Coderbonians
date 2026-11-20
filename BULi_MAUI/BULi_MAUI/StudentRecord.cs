using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BULi_MAUI;

public class StudentRecord : INotifyPropertyChanged
{
    private bool _isSelected;

    public uint StudentId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string YearLevel { get; set; } = string.Empty;
    public string Program { get; set; } = string.Empty;
    public string Block { get; set; } = string.Empty;
    public DateTime? Birthdate { get; set; }
    public int? Age { get; set; }
    public string ContactNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "Active";
    public byte[]? ProfilePictureBytes { get; set; }

    public ImageSource? PhotoSource => ProfilePictureBytes is { Length: > 0 }
        ? ImageSource.FromStream(() => new MemoryStream(ProfilePictureBytes))
        : null;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public string DisplayName => !string.IsNullOrWhiteSpace(FullName)
        ? FullName
        : string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string Initials => SessionService.GetInitials(DisplayName);
    public string SubtitleText => $"ID: {StudentNumber} | {ProgramShortCode} - {YearLevel} (Block {Block})";
    public string BirthdateDisplay => Birthdate.HasValue ? Birthdate.Value.ToString("MMM dd, yyyy") : "N/A";

    public string ProgramShortCode => Program switch
    {
        "BS Information Technology" => "BSIT",
        "BS Computer Science" => "BSCS",
        "BS Meteorology" => "BSMET",
        "BS Biology" => "BSBIO",
        "BS Chemistry" => "BSCHEM",
        _ => Program
    };

    public Color StatusTextColor => Status switch
    {
        "Active" => Color.FromArgb("#FFA500"),
        "Graduated" => Color.FromArgb("#87CEEB"),
        "On Leave" => Color.FromArgb("#B0C4DE"),
        _ => Color.FromArgb("#FF6B6B")
    };

    public Color StatusBadgeBackground => Status switch
    {
        "Active" => Color.FromArgb("#26FFA500"),
        "Graduated" => Color.FromArgb("#2687CEEB"),
        "On Leave" => Color.FromArgb("#26B0C4DE"),
        _ => Color.FromArgb("#26FF6B6B")
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}