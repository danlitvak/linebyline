using System;
using System.Collections.Generic;
using LineByLine.App.Models;

namespace LineByLine.App.Data;

public class NotebookRepository
{
    public List<Notebook> GetAll()
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, is_default, created_at FROM notebooks ORDER BY created_at ASC";

        var list = new List<Notebook>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Notebook
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                IsDefault = reader.GetInt32(2) == 1,
                CreatedAt = DateTimeOffset.Parse(reader.GetString(3)),
            });
        }
        return list;
    }

    public Notebook? GetByName(string name)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, is_default, created_at FROM notebooks WHERE name = @name COLLATE NOCASE LIMIT 1";
        cmd.Parameters.AddWithValue("@name", name);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Notebook
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            IsDefault = reader.GetInt32(2) == 1,
            CreatedAt = DateTimeOffset.Parse(reader.GetString(3)),
        };
    }

    public Notebook Create(string name)
    {
        var notebook = new Notebook
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            IsDefault = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO notebooks (id, name, is_default, created_at) VALUES (@id, @name, 0, @created_at)";
        cmd.Parameters.AddWithValue("@id", notebook.Id);
        cmd.Parameters.AddWithValue("@name", notebook.Name);
        cmd.Parameters.AddWithValue("@created_at", notebook.CreatedAt.ToString("O"));
        cmd.ExecuteNonQuery();

        return notebook;
    }

    public List<Page> GetPages(string notebookId)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, notebook_id, title, created_at FROM pages WHERE notebook_id = @nb ORDER BY created_at ASC";
        cmd.Parameters.AddWithValue("@nb", notebookId);

        var list = new List<Page>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Page
            {
                Id = reader.GetString(0),
                NotebookId = reader.GetString(1),
                Title = reader.GetString(2),
                CreatedAt = DateTimeOffset.Parse(reader.GetString(3)),
            });
        }
        return list;
    }

    public Page? GetPageByTitle(string notebookId, string title)
    {
        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, notebook_id, title, created_at FROM pages WHERE notebook_id = @nb AND title = @title COLLATE NOCASE LIMIT 1";
        cmd.Parameters.AddWithValue("@nb", notebookId);
        cmd.Parameters.AddWithValue("@title", title);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Page
        {
            Id = reader.GetString(0),
            NotebookId = reader.GetString(1),
            Title = reader.GetString(2),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(3)),
        };
    }

    public Page CreatePage(string notebookId, string title)
    {
        var page = new Page
        {
            Id = Guid.NewGuid().ToString(),
            NotebookId = notebookId,
            Title = title,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        using var conn = Database.OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO pages (id, notebook_id, title, created_at) VALUES (@id, @nb, @title, @created_at)";
        cmd.Parameters.AddWithValue("@id", page.Id);
        cmd.Parameters.AddWithValue("@nb", page.NotebookId);
        cmd.Parameters.AddWithValue("@title", page.Title);
        cmd.Parameters.AddWithValue("@created_at", page.CreatedAt.ToString("O"));
        cmd.ExecuteNonQuery();

        return page;
    }
}
