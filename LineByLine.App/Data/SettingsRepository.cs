namespace LineByLine.App.Data;

public class SettingsRepository
{
    public string? Get(string key)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = @key";
        cmd.Parameters.AddWithValue("@key", key);
        return cmd.ExecuteScalar() as string;
    }

    public void Set(string key, string value)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO settings (key, value) VALUES (@key, @value) ON CONFLICT(key) DO UPDATE SET value = excluded.value";
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);
        cmd.ExecuteNonQuery();
    }

    public int GetInt(string key, int defaultValue)
    {
        var raw = Get(key);
        return raw != null && int.TryParse(raw, out var v) ? v : defaultValue;
    }

    public void SetInt(string key, int value) => Set(key, value.ToString());
}
