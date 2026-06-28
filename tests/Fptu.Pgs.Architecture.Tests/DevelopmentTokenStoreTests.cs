using Fptu.Pgs.Contracts;
using Fptu.Pgs.Identity.Api.Application;

namespace Fptu.Pgs.Architecture.Tests;

public sealed class DevelopmentTokenStoreTests
{
    [Fact]
    public void AdminToken_AllowsAdminOperations()
    {
        var store = new DevelopmentTokenStore();
        var tokens = store.Issue(Guid.NewGuid(), UserRole.Admin);

        Assert.True(store.IsAdmin($"Bearer {tokens.AccessToken}"));
    }

    [Fact]
    public void TeacherToken_DoesNotAllowAdminOperations()
    {
        var store = new DevelopmentTokenStore();
        var tokens = store.Issue(Guid.NewGuid(), UserRole.Teacher);

        Assert.False(store.IsAdmin($"Bearer {tokens.AccessToken}"));
        Assert.False(store.IsAdmin(null));
    }
}
