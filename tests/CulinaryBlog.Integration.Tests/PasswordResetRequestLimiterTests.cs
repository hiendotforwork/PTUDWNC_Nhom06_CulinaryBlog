namespace CulinaryBlog.Integration.Tests;

using CulinaryBlog.API.Services;
using Xunit;

public sealed class PasswordResetRequestLimiterTests
{
    [Fact]
    public async Task PasswordResetCooldownIsSeparateFromEmailConfirmationCooldown()
    {
        var email = Guid.NewGuid().ToString("N") + "@example.invalid";
        var reset = new InMemoryPasswordResetRequestLimiter();
        var confirmation = new InMemoryEmailConfirmationResendLimiter();

        Assert.True(await reset.TryAcquireAsync(email));
        Assert.False(await reset.TryAcquireAsync(email));
        Assert.True(await confirmation.TryAcquireAsync(email));
        Assert.False(await confirmation.TryAcquireAsync(email));
    }
}
