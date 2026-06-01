using System;
using System.Security.Cryptography;
using LineByLine.App.Crypto;
using LineByLine.App.Data;
using Microsoft.Data.Sqlite;

namespace LineByLine.App.Services;

public class VaultService
{
    private const string CheckPlaintext = "LINE_BY_LINE_CHECK_V1";

    private byte[]? _activeKey;

    public bool IsUnlocked => _activeKey != null;

    public byte[] GetActiveKey()
    {
        if (_activeKey is null)
            throw new InvalidOperationException("Vault is locked.");
        return _activeKey;
    }

    public bool VaultExists()
    {
        var path = Database.GetVaultPath();
        if (!System.IO.File.Exists(path))
            return false;

        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='vault_meta'";
        var count = (long)(cmd.ExecuteScalar() ?? 0L);
        if (count == 0) return false;

        cmd.CommandText = "SELECT COUNT(*) FROM vault_meta";
        count = (long)(cmd.ExecuteScalar() ?? 0L);
        return count > 0;
    }

    // Creates the vault DB, vault_meta row, and default Journal notebook.
    public void CreateVault(string passphrase)
    {
        using var conn = Database.OpenConnection();
        Database.EnsureSchema(conn);

        var salt = KeyDeriver.GenerateSalt();
        var key = KeyDeriver.DeriveKey(passphrase, salt, KeyDeriver.Iterations);
        var (nonce, ciphertext) = AesGcmCrypto.EncryptString(CheckPlaintext, key);

        using var tx = conn.BeginTransaction();

        using var metaCmd = conn.CreateCommand();
        metaCmd.CommandText = """
            INSERT INTO vault_meta
                (id, kdf_name, kdf_salt, kdf_iterations, password_check_nonce, password_check_ciphertext, created_at, schema_version)
            VALUES
                (1, @kdf_name, @kdf_salt, @kdf_iter, @nonce, @cipher, @created_at, 1)
            """;
        metaCmd.Parameters.AddWithValue("@kdf_name", KeyDeriver.KdfName);
        metaCmd.Parameters.AddWithValue("@kdf_salt", salt);
        metaCmd.Parameters.AddWithValue("@kdf_iter", KeyDeriver.Iterations);
        metaCmd.Parameters.AddWithValue("@nonce", nonce);
        metaCmd.Parameters.AddWithValue("@cipher", ciphertext);
        metaCmd.Parameters.AddWithValue("@created_at", DateTimeOffset.UtcNow.ToString("O"));
        metaCmd.ExecuteNonQuery();

        using var nbCmd = conn.CreateCommand();
        nbCmd.CommandText = """
            INSERT INTO notebooks (id, name, is_default, created_at)
            VALUES (@id, 'Journal', 1, @created_at)
            """;
        nbCmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        nbCmd.Parameters.AddWithValue("@created_at", DateTimeOffset.UtcNow.ToString("O"));
        nbCmd.ExecuteNonQuery();

        tx.Commit();

        _activeKey = key;
    }

    // Returns true and sets active key on success; returns false on wrong passphrase.
    public bool Unlock(string passphrase)
    {
        using var conn = Database.OpenConnection();
        Database.EnsureSchema(conn); // apply any new tables to existing vaults

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT kdf_salt, kdf_iterations, password_check_nonce, password_check_ciphertext
            FROM vault_meta WHERE id = 1
            """;

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return false;

        var salt = (byte[])reader["kdf_salt"];
        var iterations = reader.GetInt32(reader.GetOrdinal("kdf_iterations"));
        var nonce = (byte[])reader["password_check_nonce"];
        var cipher = (byte[])reader["password_check_ciphertext"];

        var key = KeyDeriver.DeriveKey(passphrase, salt, iterations);

        try
        {
            var check = AesGcmCrypto.DecryptString(nonce, cipher, key);
            if (check != CheckPlaintext)
                return false;

            _activeKey = key;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public void Lock()
    {
        if (_activeKey != null)
        {
            // Zero out key bytes before releasing
            Array.Clear(_activeKey, 0, _activeKey.Length);
            _activeKey = null;
        }
    }
}
