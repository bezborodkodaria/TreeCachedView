namespace TreeEditor.Data;

public sealed class TreeNode
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public TreeNode? Parent { get; set; }
    public string Value { get; set; } = "";
}
