using Microsoft.EntityFrameworkCore;

namespace TreeEditor.Data;

public sealed class DatabaseSeeder
{
    private readonly TreeDbContext _db;

    public DatabaseSeeder(TreeDbContext db) => _db = db;

    public async Task InitializeAsync()
    {
        await _db.Database.EnsureCreatedAsync();
        if (!await _db.Nodes.AnyAsync()) await SeedAsync();
    }

    public async Task ResetAsync()
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.Nodes.ExecuteDeleteAsync();
        await SeedAsync();
        await transaction.CommitAsync();
    }

    private async Task SeedAsync()
    {
        var work = new TreeNode { Value = "Work" };
        var projects = new TreeNode { Value = "Projects", Parent = work };
        var editor = new TreeNode { Value = "Tree editor", Parent = projects };
        var notes = new TreeNode { Value = "Notes", Parent = work };
        var home = new TreeNode { Value = "Home" };
        var rooms = new TreeNode { Value = "Rooms", Parent = home };
        var kitchen = new TreeNode { Value = "Kitchen", Parent = rooms };

        _db.Nodes.AddRange(
            work, projects, editor,
            new TreeNode { Value = "API", Parent = editor },
            new TreeNode { Value = "UI", Parent = editor },
            new TreeNode { Value = "Website", Parent = projects },
            notes,
            new TreeNode { Value = "Ideas", Parent = notes },
            home, rooms, kitchen,
            new TreeNode { Value = "Cabinet", Parent = kitchen },
            new TreeNode { Value = "Bedroom", Parent = rooms });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }
}
