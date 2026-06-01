using System;
using System.Collections.Generic;
using LineByLine.App.Models;

namespace LineByLine.App.Data;

public class EntryRepository
{
    public string? GetDefaultNotebookId()
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id FROM notebooks WHERE is_default = 1 LIMIT 1";
        return cmd.ExecuteScalar() as string;
    }

    public void InsertEntry(Entry entry)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO entries
                (id, notebook_id, page_id, created_at, unlock_at,
                 encrypted_text, nonce, sealed_preview, preview_version, is_deleted)
            VALUES
                (@id, @notebook_id, @page_id, @created_at, @unlock_at,
                 @encrypted_text, @nonce, @sealed_preview, @preview_version, 0)
            """;

        cmd.Parameters.AddWithValue("@id", entry.Id);
        cmd.Parameters.AddWithValue("@notebook_id", entry.NotebookId);
        cmd.Parameters.AddWithValue("@page_id", entry.PageId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@created_at", entry.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@unlock_at", entry.UnlockAt.ToString("O"));
        cmd.Parameters.AddWithValue("@encrypted_text", entry.EncryptedText);
        cmd.Parameters.AddWithValue("@nonce", entry.Nonce);
        cmd.Parameters.AddWithValue("@sealed_preview", entry.SealedPreview);
        cmd.Parameters.AddWithValue("@preview_version", entry.PreviewVersion);
        cmd.ExecuteNonQuery();
    }

    public List<Entry> GetRecentEntries(string notebookId, string? pageId, int limit = 30)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();

        var pageFilter = pageId == null
            ? "AND page_id IS NULL"
            : "AND page_id = @page_id";

        cmd.CommandText = $"""
            SELECT id, notebook_id, page_id, created_at, unlock_at,
                   encrypted_text, nonce, sealed_preview, preview_version
            FROM entries
            WHERE is_deleted = 0
              AND notebook_id = @notebook_id
              {pageFilter}
            ORDER BY created_at ASC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@notebook_id", notebookId);
        if (pageId != null) cmd.Parameters.AddWithValue("@page_id", pageId);
        cmd.Parameters.AddWithValue("@limit", limit);
        return ReadEntries(cmd);
    }

    // Soft-deletes the most recent entry in the given context. Returns its id, or null if nothing to delete.
    public string? DeleteLast(string notebookId, string? pageId)
    {
        using var conn = Database.OpenConnection();

        var pageFilter = pageId == null ? "AND page_id IS NULL" : "AND page_id = @page_id";

        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = $"""
            SELECT id FROM entries
            WHERE is_deleted = 0 AND notebook_id = @notebook_id {pageFilter}
            ORDER BY created_at DESC LIMIT 1
            """;
        selectCmd.Parameters.AddWithValue("@notebook_id", notebookId);
        if (pageId != null) selectCmd.Parameters.AddWithValue("@page_id", pageId);
        var id = selectCmd.ExecuteScalar() as string;
        if (id is null) return null;

        using var deleteCmd = conn.CreateCommand();
        deleteCmd.CommandText = "UPDATE entries SET is_deleted = 1 WHERE id = @id";
        deleteCmd.Parameters.AddWithValue("@id", id);
        deleteCmd.ExecuteNonQuery();

        return id;
    }

    public int GetEntryCount(string notebookId, string? pageId)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        var pageFilter = pageId == null ? "AND page_id IS NULL" : "AND page_id = @page_id";
        cmd.CommandText = $"SELECT COUNT(*) FROM entries WHERE is_deleted = 0 AND notebook_id = @nb {pageFilter}";
        cmd.Parameters.AddWithValue("@nb", notebookId);
        if (pageId != null) cmd.Parameters.AddWithValue("@page_id", pageId);
        return (int)(long)(cmd.ExecuteScalar() ?? 0L);
    }

    public List<Entry> GetDeletedEntries(string notebookId, string? pageId, int limit = 10)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        var pageFilter = pageId == null ? "AND page_id IS NULL" : "AND page_id = @page_id";
        cmd.CommandText = $"""
            SELECT id, notebook_id, page_id, created_at, unlock_at,
                   encrypted_text, nonce, sealed_preview, preview_version
            FROM entries
            WHERE is_deleted = 1 AND notebook_id = @nb {pageFilter}
            ORDER BY created_at DESC
            LIMIT @limit
            """;
        cmd.Parameters.AddWithValue("@nb", notebookId);
        if (pageId != null) cmd.Parameters.AddWithValue("@page_id", pageId);
        cmd.Parameters.AddWithValue("@limit", limit);
        return ReadEntries(cmd);
    }

    public string? RestoreLast(string notebookId, string? pageId)
    {
        using var conn = Database.OpenConnection();
        var pageFilter = pageId == null ? "AND page_id IS NULL" : "AND page_id = @page_id";

        using var selectCmd = conn.CreateCommand();
        selectCmd.CommandText = $"SELECT id FROM entries WHERE is_deleted = 1 AND notebook_id = @nb {pageFilter} ORDER BY created_at DESC LIMIT 1";
        selectCmd.Parameters.AddWithValue("@nb", notebookId);
        if (pageId != null) selectCmd.Parameters.AddWithValue("@page_id", pageId);
        var id = selectCmd.ExecuteScalar() as string;
        if (id is null) return null;

        using var restoreCmd = conn.CreateCommand();
        restoreCmd.CommandText = "UPDATE entries SET is_deleted = 0 WHERE id = @id";
        restoreCmd.Parameters.AddWithValue("@id", id);
        restoreCmd.ExecuteNonQuery();
        return id;
    }

    // Returns entries whose unlock_at has passed, oldest first.
    public List<Entry> GetUnlockedEntries(string? notebookId = null)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        var nbFilter = notebookId == null ? "" : "AND notebook_id = @nb";
        cmd.CommandText = $"""
            SELECT id, notebook_id, page_id, created_at, unlock_at,
                   encrypted_text, nonce, sealed_preview, preview_version
            FROM entries
            WHERE is_deleted = 0
              AND unlock_at <= @now
              {nbFilter}
            ORDER BY created_at ASC
            """;
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("O"));
        if (notebookId != null) cmd.Parameters.AddWithValue("@nb", notebookId);
        return ReadEntries(cmd);
    }

    private static List<Entry> ReadEntries(Microsoft.Data.Sqlite.SqliteCommand cmd)
    {
        var entries = new List<Entry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new Entry
            {
                Id = reader.GetString(0),
                NotebookId = reader.GetString(1),
                PageId = reader.IsDBNull(2) ? null : reader.GetString(2),
                CreatedAt = DateTimeOffset.Parse(reader.GetString(3)),
                UnlockAt = DateTimeOffset.Parse(reader.GetString(4)),
                EncryptedText = (byte[])reader[5],
                Nonce = (byte[])reader[6],
                SealedPreview = reader.GetString(7),
                PreviewVersion = reader.GetInt32(8),
                IsDeleted = false,
            });
        }
        return entries;
    }
}
