namespace NeuroBlog.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
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

            // Paging top-level comments of an article: WHERE ArticleId = @a AND
            // ParentCommentId IS NULL ORDER BY CreatedAt, Id. Composite covers the
            // filter + sort so the page is read straight from the index.
            comment.HasIndex(c => new { c.ArticleId, c.ParentCommentId, c.CreatedAt, c.Id });

            // Paging direct replies of a comment: WHERE ParentCommentId = @p
            // ORDER BY CreatedAt, Id. Also serves the self-referencing FK lookups.
            comment.HasIndex(c => new { c.ParentCommentId, c.CreatedAt, c.Id });

            comment.HasOne(c => c.ParentComment)
                   .WithMany(c => c.Replies)
                   .HasForeignKey(c => c.ParentCommentId)
                   .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
