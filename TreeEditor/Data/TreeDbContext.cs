using Microsoft.EntityFrameworkCore;

namespace TreeEditor.Data;

public sealed class TreeDbContext : DbContext
{
    public TreeDbContext(DbContextOptions<TreeDbContext> options) : base(options) { }
    public DbSet<TreeNode> Nodes => Set<TreeNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var node = modelBuilder.Entity<TreeNode>();
        node.ToTable("nodes");
        node.HasKey(x => x.Id);
        node.Property(x => x.Id).HasColumnName("id");
        node.Property(x => x.ParentId).HasColumnName("parent_id");
        node.Property(x => x.Value).HasColumnName("value").IsRequired();
        node.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Cascade);
        node.HasIndex(x => x.ParentId).HasDatabaseName("ix_nodes_parent_id");
    }
}
