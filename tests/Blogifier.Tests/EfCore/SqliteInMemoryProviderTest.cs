using Blogifier.Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Data.Common;

namespace Blogifier.Tests.EfCore
{
    public class SqliteInMemoryProviderTest : ProviderTest, IDisposable
    {
        private readonly DbConnection _connection;

        public SqliteInMemoryProviderTest() : base(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(CreateInMemoryDatabase())
            .Options)
        {
            _connection = RelationalOptionsExtension.Extract(ContextOptions).Connection;
        }

        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");

            connection.Open();

            return connection;
        }

        public void Dispose() => _connection.Dispose();
    }
}
