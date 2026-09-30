namespace TreeEditor.Contracts;

public record TreeItem(long Id, long? ParentId, string Value, bool HasChildren);
public record TreePage(IReadOnlyList<TreeItem> Nodes, bool HasMore);
public record LoadedItem(long Id, long? ParentId, string Value, IReadOnlyList<long> Path);
public record ApplyRequest(List<long> Deleted, List<Edit> Edited, List<Addition> Added);
public record Edit(long Id, string Value);
public record Addition(string TempId, long? ParentId, string? ParentTempId, string Value);
public record ApplyResult(Dictionary<string, long> Created);
