using Microsoft.EntityFrameworkCore;
using TreeEditor.Contracts;

namespace TreeEditor.Data;

public sealed class TreeRepository
{
    private readonly TreeDbContext _db;

    public TreeRepository(TreeDbContext db) => _db = db;

    public async Task<TreePage> GetChildrenAsync(long? parentId, int offset)
    {
        var nodes = await _db.Nodes.AsNoTracking()
            .Where(node => node.ParentId == parentId)
            .OrderBy(node => node.Id)
            .Skip(offset)
            .Take(21)
            .Select(node => new TreeItem(node.Id, node.ParentId, node.Value,
                _db.Nodes.Any(child => child.ParentId == node.Id)))
            .ToListAsync();

        return new TreePage(nodes.Take(20).ToList(), nodes.Count > 20);
    }

    public async Task<LoadedItem?> LoadAsync(long id)
    {
        var path = new List<long>();
        TreeNode? selected = null;
        long? currentId = id;

        while (currentId is not null)
        {
            var node = await _db.Nodes.AsNoTracking()
                .Where(item => item.Id == currentId)
                .Select(item => new TreeNode { Id = item.Id, ParentId = item.ParentId, Value = item.Value })
                .SingleOrDefaultAsync();
            if (node is null) return null;
            selected ??= node;
            path.Add(node.Id);
            currentId = node.ParentId;
        }

        path.Reverse();
        return new LoadedItem(selected!.Id, selected.ParentId, selected.Value, path);
    }

    public async Task<ApplyResult> ApplyAsync(ApplyRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var id in request.Deleted.Distinct())
            await _db.Nodes.Where(node => node.Id == id).ExecuteDeleteAsync();

        foreach (var edit in request.Edited)
        {
            var node = await _db.Nodes.FindAsync(edit.Id)
                ?? throw new InvalidOperationException("An edited element no longer exists.");
            node.Value = edit.Value.Trim();
        }
        await _db.SaveChangesAsync();

        var created = new Dictionary<string, long>();
        foreach (var addition in request.Added)
        {
            var parentId = addition.ParentId ?? created.GetValueOrDefault(addition.ParentTempId ?? "");
            if (parentId == 0 || !await _db.Nodes.AnyAsync(node => node.Id == parentId))
                throw new InvalidOperationException("The parent element no longer exists.");

            var node = new TreeNode { ParentId = parentId, Value = addition.Value.Trim() };
            _db.Nodes.Add(node);
            await _db.SaveChangesAsync();
            created.Add(addition.TempId, node.Id);
        }

        await transaction.CommitAsync();
        return new ApplyResult(created);
    }
}
