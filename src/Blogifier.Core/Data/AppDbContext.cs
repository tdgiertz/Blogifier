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
        public DbSet<FileDescriptor> FileDescriptors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Post>()
                .HasMany(p => p.Categories)
                .WithMany(c => c.Posts)
                .UsingEntity(b => b.ToTable("PostCategory"));

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

            modelBuilder.Entity<Post>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<Post>().Property(p => p.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Post>().ToTable("Post");
            modelBuilder.Entity<Blog>().Property(b => b.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Blog>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<Blog>().ToTable("Blog");
            modelBuilder.Entity<Author>().Property(a => a.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Author>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<Author>().ToTable("Author");
            modelBuilder.Entity<Category>().Property(c => c.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Category>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<Subscriber>().Property(s => s.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Subscriber>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<Subscriber>().ToTable("Subscriber");
            modelBuilder.Entity<Newsletter>().Property(n => n.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<Newsletter>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<Newsletter>().ToTable("Newsletter");
            modelBuilder.Entity<MailSetting>().Property(n => n.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<MailSetting>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<MailSetting>().ToTable("MailSetting");
            modelBuilder.Entity<FileDescriptor>().Property(n => n.DateUpdated).HasDefaultValueSql(sql);
            modelBuilder.Entity<FileDescriptor>().Property(p => p.Id).ValueGeneratedNever();
            modelBuilder.Entity<FileDescriptor>().ToTable("FileDescriptor");
        }
    }
}
