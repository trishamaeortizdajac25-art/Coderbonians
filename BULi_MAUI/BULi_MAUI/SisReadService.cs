using MySqlConnector;

namespace BULi_MAUI;

public static class SisReadService
{
    private const string DatabaseName = "sis_db";
    private const string DbUser = "root";
    private const string DbPassword = "nunez26*7";

    // Update YOUR_PC_LAN_IP to your PC's Wi-Fi IPv4 (e.g., 192.168.1.45) for physical Android devices
    private const string LanServerIp = "LAGAY_IP4";

    private static string? _activeConnectionString;

    private static string BuildConnectionString(string server) =>
        $"Server={server};Port=3306;Database={DatabaseName};User ID={DbUser};Password={DbPassword};Connection Timeout=3;";

    public static string ConnectionString
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_activeConnectionString))
                return _activeConnectionString;

#if ANDROID
            if (!string.IsNullOrWhiteSpace(LanServerIp) && LanServerIp != "LAGAY_IP4")
                return BuildConnectionString(LanServerIp);
            else
                return BuildConnectionString("10.0.2.2");
#else
            return BuildConnectionString("127.0.0.1");
#endif
        }
        set => _activeConnectionString = value;
    }

    public static async Task<MySqlConnection> OpenConnectionAsync()
    {
        if (!string.IsNullOrWhiteSpace(_activeConnectionString))
        {
            var cachedConn = new MySqlConnection(_activeConnectionString);
            await cachedConn.OpenAsync();
            return cachedConn;
        }

        string[] candidates;

#if ANDROID
        if (!string.IsNullOrWhiteSpace(LanServerIp) && LanServerIp != "LAGAY_IP4")
        {
            candidates =
            [
                BuildConnectionString(LanServerIp),
                BuildConnectionString("10.0.2.2"),
                BuildConnectionString("127.0.0.1")
            ];
        }
        else
        {
            candidates =
            [
                BuildConnectionString("10.0.2.2"),
                BuildConnectionString("127.0.0.1")
            ];
        }
#else
        if (!string.IsNullOrWhiteSpace(LanServerIp) && LanServerIp != "LAGAY_IP4")
        {
            candidates =
            [
                BuildConnectionString("127.0.0.1"),
                BuildConnectionString(LanServerIp)
            ];
        }
        else
        {
            candidates =
            [
                BuildConnectionString("127.0.0.1")
            ];
        }
#endif

        Exception? lastError = null;

        foreach (string candidate in candidates)
        {
            try
            {
                var conn = new MySqlConnection(candidate);
                await conn.OpenAsync();
                _activeConnectionString = candidate;
                return conn;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        if (lastError is not null)
            throw lastError;

        var fallback = new MySqlConnection(ConnectionString);
        await fallback.OpenAsync();
        return fallback;
    }

    public static async Task<List<StudentRecord>> GetStudentsAsync()
    {
        var list = new List<StudentRecord>();

        await using var conn = await OpenConnectionAsync();

        const string sql = @"
            SELECT v.student_id, v.student_number, v.first_name, v.middle_name, v.last_name,
                   v.full_name, v.gender, v.year_level, v.program, v.block, v.birthdate, v.age,
                   v.contact_number, v.email, v.address, v.status, s.profile_picture
            FROM vw_student_information v
            INNER JOIN students s ON s.student_id = v.student_id
            ORDER BY v.student_id DESC;";

        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        int picOrdinal = reader.GetOrdinal("profile_picture");

        while (await reader.ReadAsync())
        {
            list.Add(new StudentRecord
            {
                StudentId = reader.GetUInt32("student_id"),
                StudentNumber = reader.IsDBNull(reader.GetOrdinal("student_number")) ? string.Empty : reader.GetString("student_number"),
                FirstName = reader.GetString("first_name"),
                MiddleName = reader.IsDBNull(reader.GetOrdinal("middle_name")) ? null : reader.GetString("middle_name"),
                LastName = reader.GetString("last_name"),
                FullName = reader.GetString("full_name"),
                Gender = reader.IsDBNull(reader.GetOrdinal("gender")) ? string.Empty : reader.GetString("gender"),
                YearLevel = reader.IsDBNull(reader.GetOrdinal("year_level")) ? string.Empty : reader.GetString("year_level"),
                Program = reader.IsDBNull(reader.GetOrdinal("program")) ? string.Empty : reader.GetString("program"),
                Block = reader.IsDBNull(reader.GetOrdinal("block")) ? string.Empty : reader.GetString("block"),
                Birthdate = reader.IsDBNull(reader.GetOrdinal("birthdate")) ? null : reader.GetDateTime("birthdate"),
                Age = reader.IsDBNull(reader.GetOrdinal("age")) ? null : reader.GetInt32("age"),
                ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? string.Empty : reader.GetString("contact_number"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                Status = reader.GetString("status"),
                ProfilePictureBytes = reader.IsDBNull(picOrdinal) ? null : (byte[])reader["profile_picture"]
            });
        }

        return list;
    }

    public static async Task<Dictionary<string, int>> GetProgramCountsAsync()
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        await using var conn = await OpenConnectionAsync();

        const string sql = "SELECT program_name, total_students FROM vw_program_counts;";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            counts[reader.GetString("program_name")] = reader.GetInt32("total_students");
        }

        return counts;
    }

    public static async Task<List<(string YearLevel, int Total)>> GetYearLevelCountsAsync()
    {
        var rows = new List<(string, int)>();

        await using var conn = await OpenConnectionAsync();

        const string sql = "SELECT year_level, total_students FROM vw_year_level_counts ORDER BY sort_order;";
        await using var cmd = new MySqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rows.Add((reader.GetString("year_level"), reader.GetInt32("total_students")));
        }

        return rows;
    }
}