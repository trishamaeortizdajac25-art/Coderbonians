using System.Globalization;

namespace BULi_MAUI;

public static class SessionService
{
    private const string KeyIsLoggedIn = "buli_is_logged_in";
    private const string KeyRole = "buli_user_role";
    private const string KeyUserId = "buli_user_id";
    private const string KeyFullName = "buli_full_name";
    private const string KeyEmail = "buli_email";
    private const string KeyContact = "buli_contact";
    private const string KeyProgram = "buli_program";
    private const string KeyYearLevel = "buli_year_level";
    private const string KeyBloc = "buli_bloc";
    private const string KeyGender = "buli_gender";
    private const string KeyBirthDate = "buli_birth_date";
    private const string KeyPhotoPath = "buli_photo_path";

    public static bool IsLoggedIn
    {
        get => Preferences.Default.Get(KeyIsLoggedIn, false);
        set => Preferences.Default.Set(KeyIsLoggedIn, value);
    }

    public static string Role
    {
        get => Preferences.Default.Get(KeyRole, string.Empty);
        set => Preferences.Default.Set(KeyRole, value);
    }

    public static string UserId
    {
        get => Preferences.Default.Get(KeyUserId, string.Empty);
        set => Preferences.Default.Set(KeyUserId, value);
    }

    public static string FullName
    {
        get => Preferences.Default.Get(KeyFullName, string.Empty);
        set => Preferences.Default.Set(KeyFullName, value);
    }

    public static string Email
    {
        get => Preferences.Default.Get(KeyEmail, string.Empty);
        set => Preferences.Default.Set(KeyEmail, value);
    }

    public static string Contact
    {
        get => Preferences.Default.Get(KeyContact, string.Empty);
        set => Preferences.Default.Set(KeyContact, value);
    }

    public static string Program
    {
        get => Preferences.Default.Get(KeyProgram, string.Empty);
        set => Preferences.Default.Set(KeyProgram, value);
    }

    public static string YearLevel
    {
        get => Preferences.Default.Get(KeyYearLevel, string.Empty);
        set => Preferences.Default.Set(KeyYearLevel, value);
    }

    public static string Bloc
    {
        get => Preferences.Default.Get(KeyBloc, string.Empty);
        set => Preferences.Default.Set(KeyBloc, value);
    }

    public static string Gender
    {
        get => Preferences.Default.Get(KeyGender, string.Empty);
        set => Preferences.Default.Set(KeyGender, value);
    }

    public static DateTime? BirthDate
    {
        get
        {
            string raw = Preferences.Default.Get(KeyBirthDate, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsed)
                ? parsed
                : null;
        }
        set
        {
            if (value.HasValue)
                Preferences.Default.Set(KeyBirthDate, value.Value.ToString("o", CultureInfo.InvariantCulture));
            else
                Preferences.Default.Remove(KeyBirthDate);
        }
    }

    public static string PhotoPath
    {
        get => Preferences.Default.Get(KeyPhotoPath, string.Empty);
        set => Preferences.Default.Set(KeyPhotoPath, value);
    }

    public static void LoginAsTeacher()
    {
        IsLoggedIn = true;
        Role = "Teacher";
        if (string.IsNullOrWhiteSpace(FullName))
            FullName = "Admin";
    }

    public static void LoginAsStudent(string userId, string fullName = "")
    {
        IsLoggedIn = true;
        Role = "Student";
        UserId = userId;
        if (!string.IsNullOrWhiteSpace(fullName))
            FullName = fullName;
    }

    public static string GetInitials() => GetInitials(FullName);

    public static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "U";

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0][..1].ToUpperInvariant();

        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    public static void Logout()
    {
        Preferences.Default.Remove(KeyIsLoggedIn);
        Preferences.Default.Remove(KeyRole);
        Preferences.Default.Remove(KeyUserId);
        Preferences.Default.Remove(KeyFullName);
        Preferences.Default.Remove(KeyEmail);
        Preferences.Default.Remove(KeyContact);
        Preferences.Default.Remove(KeyProgram);
        Preferences.Default.Remove(KeyYearLevel);
        Preferences.Default.Remove(KeyBloc);
        Preferences.Default.Remove(KeyGender);
        Preferences.Default.Remove(KeyBirthDate);
        Preferences.Default.Remove(KeyPhotoPath);
    }

    private const string KeyIsDarkMode = "buli_is_dark_mode";

    public static bool IsDarkMode
    {
        get => Preferences.Default.Get(KeyIsDarkMode, true);
        set => Preferences.Default.Set(KeyIsDarkMode, value);
    }
}