using System.Security.Cryptography;
using System.Text;
using MySqlConnector;

namespace BULi_MAUI;

public static class SisWriteService
{
    private static string ConnStr => SisReadService.ConnectionString;

    // Converts plain text password to SHA-256 lowercase hex (matches MySQL SHA2(..., 256))
    public static string HashPassword(string plainPassword)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // 1. STUDENT SIGN UP (c2_stdCreate)
    public static async Task<(bool Success, uint StudentId, string Message)> RegisterStudentAsync(
        string username, string password)
    {
        try
        {
            await using var conn = new MySqlConnection(ConnStr);
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO students (first_name, last_name, username, password_hash)
                VALUES ('New', 'Student', @user, @hash);
                SELECT LAST_INSERT_ID();";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@user", username.Trim());
            cmd.Parameters.AddWithValue("@hash", HashPassword(password));

            object? scalar = await cmd.ExecuteScalarAsync();
            uint newId = Convert.ToUInt32(scalar);
            return (true, newId, "Account created!");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return (false, 0, "That username is already taken.");
        }
        catch (Exception ex)
        {
            return (false, 0, $"Database error: {ex.Message}");
        }
    }

    // 2. STUDENT SIGN IN (b2_stdSignIn)
    public static async Task<StudentRecord?> AuthenticateStudentAsync(string username, string password)
    {
        await using var conn = new MySqlConnection(ConnStr);
        await conn.OpenAsync();

        const string sql = @"
            SELECT student_id, student_number, first_name, middle_name, last_name,
                   gender, year_level, program, block, birthdate, contact_number, email, address
            FROM students
            WHERE username = @user AND password_hash = @hash AND is_deleted = 0
            LIMIT 1;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@user", username.Trim());
        cmd.Parameters.AddWithValue("@hash", HashPassword(password));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        string fn = reader.GetString("first_name");
        string? mn = reader.IsDBNull(reader.GetOrdinal("middle_name")) ? null : reader.GetString("middle_name");
        string ln = reader.GetString("last_name");

        return new StudentRecord
        {
            StudentId = reader.GetUInt32("student_id"),
            StudentNumber = reader.IsDBNull(reader.GetOrdinal("student_number")) ? "" : reader.GetString("student_number"),
            FirstName = fn,
            MiddleName = mn,
            LastName = ln,
            FullName = string.IsNullOrWhiteSpace(mn) ? $"{fn} {ln}" : $"{fn} {mn} {ln}",
            Gender = reader.IsDBNull(reader.GetOrdinal("gender")) ? "" : reader.GetString("gender"),
            YearLevel = reader.IsDBNull(reader.GetOrdinal("year_level")) ? "" : reader.GetString("year_level"),
            Program = reader.IsDBNull(reader.GetOrdinal("program")) ? "" : reader.GetString("program"),
            Block = reader.IsDBNull(reader.GetOrdinal("block")) ? "" : reader.GetString("block"),
            Birthdate = reader.IsDBNull(reader.GetOrdinal("birthdate")) ? null : reader.GetDateTime("birthdate"),
            ContactNumber = reader.IsDBNull(reader.GetOrdinal("contact_number")) ? "" : reader.GetString("contact_number"),
            Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
            Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address")
        };
    }

    // 3. TEACHER SIGN IN (b1_tcrSignIn)
    public static async Task<(bool Success, uint TeacherId, string FullName)> AuthenticateTeacherAsync(string username, string password)
    {
        await using var conn = new MySqlConnection(ConnStr);
        await conn.OpenAsync();

        const string sql = @"
            SELECT teacher_id, first_name, last_name
            FROM teachers
            WHERE username = @user AND password_hash = @hash AND is_active = 1
            LIMIT 1;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@user", username.Trim());
        cmd.Parameters.AddWithValue("@hash", HashPassword(password));

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return (false, 0, string.Empty);

        uint id = reader.GetUInt32("teacher_id");
        string fullName = $"{reader.GetString("first_name")} {reader.GetString("last_name")}";
        return (true, id, fullName);
    }

    // 4. TEACHER CHANGE PASSWORD (c1_HomePage)
    public static async Task<(bool Success, string Message)> ChangeTeacherPasswordAsync(uint teacherId, string oldPassword, string newPassword)
    {
        await using var conn = new MySqlConnection(ConnStr);
        await conn.OpenAsync();

        const string sql = @"
            UPDATE teachers
            SET password_hash = @newHash
            WHERE teacher_id = @id AND password_hash = @oldHash;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@newHash", HashPassword(newPassword));
        cmd.Parameters.AddWithValue("@oldHash", HashPassword(oldPassword));
        cmd.Parameters.AddWithValue("@id", teacherId);

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0
            ? (true, "Password updated successfully!")
            : (false, "Current password is incorrect.");
    }

    // 5. STUDENT SELF-EDIT (e2_stdMainPage & d2_SetUp)
    // 5. STUDENT SELF-EDIT (e2_stdMainPage & d2_SetUp)
    public static async Task<bool> UpdateStudentProfileAsync(StudentRecord s, string? oldStudentNumber = null)
    {
        await using var conn = new MySqlConnection(ConnStr);
        await conn.OpenAsync();

        const string sql = @"
            UPDATE students
            SET student_number = COALESCE(NULLIF(@snum, ''), student_number),
                first_name = @fn, middle_name = @mn, last_name = @ln,
                gender = @gender, birthdate = @bday, year_level = @yl,
                program = @prog, block = @block, contact_number = @contact,
                email = @email, address = @addr
            WHERE student_id = @id
               OR (@oldSnum IS NOT NULL AND student_number = @oldSnum)
               OR (@email IS NOT NULL AND email = @email);";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@snum", string.IsNullOrWhiteSpace(s.StudentNumber) ? DBNull.Value : s.StudentNumber);
        cmd.Parameters.AddWithValue("@oldSnum", string.IsNullOrWhiteSpace(oldStudentNumber) ? DBNull.Value : oldStudentNumber);
        cmd.Parameters.AddWithValue("@fn", s.FirstName);
        cmd.Parameters.AddWithValue("@mn", string.IsNullOrWhiteSpace(s.MiddleName) ? DBNull.Value : s.MiddleName);
        cmd.Parameters.AddWithValue("@ln", s.LastName);
        cmd.Parameters.AddWithValue("@gender", string.IsNullOrWhiteSpace(s.Gender) ? DBNull.Value : s.Gender);
        cmd.Parameters.AddWithValue("@bday", s.Birthdate.HasValue ? s.Birthdate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@yl", string.IsNullOrWhiteSpace(s.YearLevel) ? DBNull.Value : s.YearLevel);
        cmd.Parameters.AddWithValue("@prog", string.IsNullOrWhiteSpace(s.Program) ? DBNull.Value : s.Program);
        cmd.Parameters.AddWithValue("@block", string.IsNullOrWhiteSpace(s.Block) ? DBNull.Value : s.Block);
        cmd.Parameters.AddWithValue("@contact", string.IsNullOrWhiteSpace(s.ContactNumber) ? DBNull.Value : s.ContactNumber);
        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(s.Email) ? DBNull.Value : s.Email);
        cmd.Parameters.AddWithValue("@addr", string.IsNullOrWhiteSpace(s.Address) ? DBNull.Value : s.Address);
        cmd.Parameters.AddWithValue("@id", s.StudentId);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // 6. SOFT DELETE STUDENT (e2_stdMainPage & d1_Records)
    public static async Task<bool> DeleteStudentAsync(uint studentId)
    {
        await using var conn = new MySqlConnection(ConnStr);
        await conn.OpenAsync();

        const string sql = "UPDATE students SET is_deleted = 1 WHERE student_id = @id;";
        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", studentId);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }
    // 8. STUDENT CHANGE PASSWORD (e2_stdMainPage)
    public static async Task<(bool Success, string Message)> ChangeStudentPasswordAsync(
        uint studentId, string? studentNumber, string oldPassword, string newPassword)
    {
        await using var conn = new MySqlConnection(ConnStr);
        await conn.OpenAsync();

        const string sql = @"
            UPDATE students
            SET password_hash = @newHash
            WHERE (student_id = @id OR (@snum IS NOT NULL AND student_number = @snum))
              AND password_hash = @oldHash
              AND is_deleted = 0;";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@newHash", HashPassword(newPassword));
        cmd.Parameters.AddWithValue("@oldHash", HashPassword(oldPassword));
        cmd.Parameters.AddWithValue("@id", studentId);
        cmd.Parameters.AddWithValue("@snum", string.IsNullOrWhiteSpace(studentNumber) ? DBNull.Value : studentNumber);

        int rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0
            ? (true, "Password updated successfully!")
            : (false, "Current password is incorrect.");
    }

    // 7. TEACHER INSERT RECORD (d1_Records)
    public static async Task<bool> AddStudentRecordAsync(StudentRecord s, string username, string password)
    {
        await using var conn = new MySqlConnection(ConnStr);
        await conn.OpenAsync();

        const string sql = @"
            INSERT INTO students
                (student_number, first_name, middle_name, last_name, gender, birthdate,
                 year_level, program, block, contact_number, email, address, status, username, password_hash)
            VALUES
                (@snum, @fn, @mn, @ln, @gender, @bday,
                 @yl, @prog, @block, @contact, @email, @addr, @status, @user, @hash);";

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@snum", string.IsNullOrWhiteSpace(s.StudentNumber) ? DBNull.Value : s.StudentNumber);
        cmd.Parameters.AddWithValue("@fn", s.FirstName);
        cmd.Parameters.AddWithValue("@mn", string.IsNullOrWhiteSpace(s.MiddleName) ? DBNull.Value : s.MiddleName);
        cmd.Parameters.AddWithValue("@ln", s.LastName);
        cmd.Parameters.AddWithValue("@gender", string.IsNullOrWhiteSpace(s.Gender) ? DBNull.Value : s.Gender);
        cmd.Parameters.AddWithValue("@bday", s.Birthdate.HasValue ? s.Birthdate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@yl", string.IsNullOrWhiteSpace(s.YearLevel) ? DBNull.Value : s.YearLevel);
        cmd.Parameters.AddWithValue("@prog", string.IsNullOrWhiteSpace(s.Program) ? DBNull.Value : s.Program);
        cmd.Parameters.AddWithValue("@block", string.IsNullOrWhiteSpace(s.Block) ? DBNull.Value : s.Block);
        cmd.Parameters.AddWithValue("@contact", string.IsNullOrWhiteSpace(s.ContactNumber) ? DBNull.Value : s.ContactNumber);
        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(s.Email) ? DBNull.Value : s.Email);
        cmd.Parameters.AddWithValue("@addr", string.IsNullOrWhiteSpace(s.Address) ? DBNull.Value : s.Address);
        cmd.Parameters.AddWithValue("@status", string.IsNullOrWhiteSpace(s.Status) ? "Active" : s.Status);
        cmd.Parameters.AddWithValue("@user", username);
        cmd.Parameters.AddWithValue("@hash", HashPassword(password));

        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}