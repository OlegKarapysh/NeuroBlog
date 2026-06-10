namespace NeuroBlog.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(article =>
        {
            article.HasKey(a => a.Id);
            article.Property(a => a.Title).HasMaxLength(ContentLimits.MaxArticleTitleLength);
            article.Property(a => a.Author).HasMaxLength(ContentLimits.MaxUsernameLength).IsRequired();
            article.Property(a => a.Html).IsRequired();
            article.HasIndex(a => a.CreatedAt);

            article.HasMany(a => a.Comments)
                   .WithOne(c => c.Article)
                   .HasForeignKey(c => c.ArticleId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(comment =>
        {
            comment.HasKey(c => c.Id);
            comment.Property(c => c.Author).HasMaxLength(ContentLimits.MaxUsernameLength).IsRequired();
            comment.Property(c => c.Content).HasMaxLength(ContentLimits.MaxCommentLength);
            comment.HasIndex(c => c.ArticleId);
            comment.HasIndex(c => c.ParentCommentId);

            comment.HasOne(c => c.ParentComment)
                   .WithMany(c => c.Replies)
                   .HasForeignKey(c => c.ParentCommentId)
                   .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
