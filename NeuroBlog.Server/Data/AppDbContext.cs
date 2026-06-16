namespace NeuroBlog.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public const string CommentIdSequence = "CommentIds";

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Comment Ids are drawn from this sequence (via nextval) before insert, so
        // the materialized Path can embed the Id without a second round trip.
        modelBuilder.HasSequence<long>(CommentIdSequence);

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
            // We allocate the Id ourselves from the sequence before insert.
            comment.Property(c => c.Id).ValueGeneratedNever();
            comment.Property(c => c.Author).HasMaxLength(ContentLimits.MaxUsernameLength).IsRequired();
            comment.Property(c => c.Content).HasMaxLength(ContentLimits.MaxCommentLength);

            // Materialized path as raw bytes (bytea): the concatenated 8-byte
            // big-endian Ids of every ancestor and the comment itself. bytea
            // compares byte-by-byte, which is exactly the order we want.
            comment.Property(c => c.Path).IsRequired();

            // Paging top-level comments of an article: WHERE ArticleId = @a AND
            // ParentCommentId IS NULL ORDER BY CreatedAt, Id. Composite covers the
            // filter + sort so the page is read straight from the index.
            comment.HasIndex(c => new { c.ArticleId, c.ParentCommentId, c.CreatedAt, c.Id });

            // Paging direct replies of a comment: WHERE ParentCommentId = @p
            // ORDER BY CreatedAt, Id. Also serves the self-referencing FK lookups.
            comment.HasIndex(c => new { c.ParentCommentId, c.CreatedAt, c.Id });

            // Depth-first first page: WHERE ArticleId = @a ORDER BY Path LIMIT 100,
            // read straight from the index (no recursion, no over-read).
            comment.HasIndex(c => new { c.ArticleId, c.Path });

            comment.HasOne(c => c.ParentComment)
                   .WithMany(c => c.Replies)
                   .HasForeignKey(c => c.ParentCommentId)
                   .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
