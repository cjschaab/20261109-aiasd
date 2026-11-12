using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TodoManager.Tests;

public sealed class IdentityDependencyTests
{
    [Fact]
    public void IdentityAndSqliteEntityFrameworkTypesAreAvailable()
    {
        Assert.NotNull(typeof(IdentityDbContext<>).Assembly);
        Assert.NotNull(typeof(SqliteDbContextOptionsBuilderExtensions).Assembly);
    }
}
