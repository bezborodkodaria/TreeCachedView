using TreeEditor.Contracts;
using TreeEditor.Data;

namespace TreeEditor.Services;

public sealed class TreeService
{
    private readonly TreeRepository _repository;
    private readonly DatabaseSeeder _seeder;

    public TreeService(TreeRepository repository, DatabaseSeeder seeder)
    {
        _repository = repository;
        _seeder = seeder;
    }

    public Task<TreePage> GetChildrenAsync(long? parentId, int offset) =>
        _repository.GetChildrenAsync(parentId, Math.Max(0, offset));

    public Task<LoadedItem?> LoadAsync(long id) => _repository.LoadAsync(id);

    public Task ResetAsync() => _seeder.ResetAsync();

    public Task<ApplyResult> ApplyAsync(ApplyRequest request)
    {
        if (request.Deleted is null || request.Edited is null || request.Added is null)
            throw new InvalidOperationException("The change list is incomplete.");
        if (request.Edited.Any(edit => string.IsNullOrWhiteSpace(edit.Value)) ||
            request.Added.Any(addition => string.IsNullOrWhiteSpace(addition.Value) || string.IsNullOrWhiteSpace(addition.TempId)))
            throw new InvalidOperationException("Every element needs a value.");
        if (request.Added.Select(addition => addition.TempId).Distinct().Count() != request.Added.Count)
            throw new InvalidOperationException("New element IDs must be unique.");

        return _repository.ApplyAsync(request);
    }
}
