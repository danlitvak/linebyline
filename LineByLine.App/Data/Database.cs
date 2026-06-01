using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace LineByLine.App.Data;

public static class Database
{
    public static string GetVaultPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LineByLine");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "linebyline.vault.db");
    }

    public static SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection($"Data Source={GetVaultPath()}");
        conn.Open();
        return conn;
    }

    public static void EnsureSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            PRAGMA journal_mode=WAL;

            CREATE TABLE IF NOT EXISTS vault_meta (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                kdf_name TEXT NOT NULL,
                kdf_salt BLOB NOT NULL,
                kdf_iterations INTEGER NOT NULL,
                kdf_memory_kib INTEGER,
                kdf_parallelism INTEGER,
                password_check_nonce BLOB NOT NULL,
                password_check_ciphertext BLOB NOT NULL,
                created_at TEXT NOT NULL,
                schema_version INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS notebooks (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                is_default INTEGER NOT NULL,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS pages (
                id TEXT PRIMARY KEY,
                notebook_id TEXT NOT NULL,
                title TEXT NOT NULL,
                created_at TEXT NOT NULL,
                FOREIGN KEY (notebook_id) REFERENCES notebooks(id)
            );

            CREATE TABLE IF NOT EXISTS entries (
                id TEXT PRIMARY KEY,
                notebook_id TEXT NOT NULL,
                page_id TEXT,
                created_at TEXT NOT NULL,
                unlock_at TEXT NOT NULL,
                encrypted_text BLOB NOT NULL,
                nonce BLOB NOT NULL,
                sealed_preview TEXT NOT NULL,
                preview_version INTEGER NOT NULL,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (notebook_id) REFERENCES notebooks(id),
                FOREIGN KEY (page_id) REFERENCES pages(id)
            );

            CREATE INDEX IF NOT EXISTS idx_entries_created_at ON entries(created_at DESC);
            CREATE INDEX IF NOT EXISTS idx_entries_unlock_at ON entries(unlock_at);
            CREATE INDEX IF NOT EXISTS idx_entries_notebook_page ON entries(notebook_id, page_id);

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }
}
