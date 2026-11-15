using FastDrop.MVC.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace FastDrop.MVC.Tests;

public class RoleSeedSecurityTests
{
    [Fact]
    public void RoleMigrationSeedsDefinitionsWithoutPromotingOrApprovingEmployeeIdentities()
    {
        var operations = new SeedRoles().UpOperations;
        var definitions = Assert.Single(operations.OfType<InsertDataOperation>());
        Assert.Equal("Role", definitions.Table);
        Assert.Equal(3, definitions.Values.GetLength(0));
        Assert.DoesNotContain(operations, operation => operation is SqlOperation or UpdateDataOperation);
    }
}
