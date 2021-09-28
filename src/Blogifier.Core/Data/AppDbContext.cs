using Blogifier.Shared;
using Microsoft.EntityFrameworkCore;

namespace Blogifier.Core.Data
{
    public class AppDbContext : DbContext
    {
        protected readonly DbContextOptions<AppDbContext> _options;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            _options = options;
        }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<Newsletter> Newsletters { get; set; }
        public DbSet<MailSetting> MailSettings { get; set; }
        // public DbSet<PostCategory> PostCategories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // modelBuilder
            //     .Entity<Post>()
            //     .Ignore(p => p.Categories)
            //     .Ignore(p => p.Blog);
            // modelBuilder
            //     .Entity<Category>()
            //     .Ignore(p => p.Posts);

            modelBuilder
                .Entity<Post>()
                .HasMany(p => p.Categories)
                .WithMany(c => c.Posts)
                .UsingEntity(b => b.ToTable("PostCategory"));

            // modelBuilder.Entity<PostCategory>()
            //     .HasKey(t => new { t.PostId, t.CategoryId });

            // modelBuilder.Entity<PostCategory>()
            //     .HasOne(pt => pt.Post)
            //     .WithMany(p => p.PostCategories)
            //     .HasForeignKey(pt => pt.PostId);

            // modelBuilder.Entity<PostCategory>()
            //     .HasOne(pt => pt.Category)
            //     .WithMany(t => t.PostCategories)
            //     .HasForeignKey(pt => pt.CategoryId);

            string sql = "getdate()";

            if (_options.Extensions != null)
            {
                foreach (var ext in _options.Extensions)
                {
                    if (ext.GetType().ToString().StartsWith("Microsoft.EntityFrameworkCore.Sqlite"))
                    {
                        sql = "DATE('now')";
                        break;
                    }
                }
            }

            modelBuilder.Entity<Post>().HasKey(p => p.Id);
            modelBuilder.Entity<Post>().ToTable("Post");
            modelBuilder.Entity<Post>().Property(p => p.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Blog>().Property(b => b.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Blog>().ToTable("Blog");
            modelBuilder.Entity<Author>().Property(a => a.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Author>().ToTable("Author");
            modelBuilder.Entity<Category>().Property(c => c.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<Subscriber>().Property(s => s.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Subscriber>().ToTable("Subscriber");
            modelBuilder.Entity<Newsletter>().Property(n => n.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Newsletter>().ToTable("Newsletter");
            modelBuilder.Entity<MailSetting>().Property(n => n.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<MailSetting>().ToTable("MailSetting");
        }
    }
}
