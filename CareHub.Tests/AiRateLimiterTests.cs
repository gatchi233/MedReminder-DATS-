using CareHub.Api.Services;
using Xunit;

namespace CareHub.Tests;

public class AiRateLimiterTests
{
    [Fact]
    public void FirstRequest_IsAllowed()
    {
        var limiter = new AiRateLimiter();
        var result = limiter.TryAcquire("user1");
        Assert.Null(result);
    }

    [Fact]
    public void GlobalRpm_BlocksAfterLimit()
    {
        var limiter = new AiRateLimiter(globalRpm: 3, globalRpd: 1000, perUserRpm: 100, perUserRpd: 1000);

        for (int i = 0; i < 3; i++)
            Assert.Null(limiter.TryAcquire($"user{i}"));

        var result = limiter.TryAcquire("user99");
        Assert.NotNull(result);
        Assert.Contains("requests/min", result);
    }

    [Fact]
    public void GlobalRpd_BlocksAfterLimit()
    {
        var limiter = new AiRateLimiter(globalRpm: 1000, globalRpd: 5, perUserRpm: 1000, perUserRpd: 1000);

        for (int i = 0; i < 5; i++)
            Assert.Null(limiter.TryAcquire($"user{i}"));

        var result = limiter.TryAcquire("user99");
        Assert.NotNull(result);
        Assert.Contains("daily limit", result);
    }

    [Fact]
    public void PerUserRpm_BlocksOnlyThatUser()
    {
        var limiter = new AiRateLimiter(globalRpm: 1000, globalRpd: 1000, perUserRpm: 2, perUserRpd: 1000);

        Assert.Null(limiter.TryAcquire("alice"));
        Assert.Null(limiter.TryAcquire("alice"));

        // Alice is blocked
        var result = limiter.TryAcquire("alice");
        Assert.NotNull(result);
        Assert.Contains("Slow down", result);

        // Bob is not blocked
        Assert.Null(limiter.TryAcquire("bob"));
    }

    [Fact]
    public void PerUserRpd_BlocksOnlyThatUser()
    {
        var limiter = new AiRateLimiter(globalRpm: 1000, globalRpd: 1000, perUserRpm: 1000, perUserRpd: 3);

        for (int i = 0; i < 3; i++)
            Assert.Null(limiter.TryAcquire("alice"));

        var result = limiter.TryAcquire("alice");
        Assert.NotNull(result);
        Assert.Contains("daily limit", result);

        // Bob still has quota
        Assert.Null(limiter.TryAcquire("bob"));
    }

    [Fact]
    public void MultipleUsers_IndependentLimits()
    {
        var limiter = new AiRateLimiter(globalRpm: 100, globalRpd: 100, perUserRpm: 2, perUserRpd: 100);

        Assert.Null(limiter.TryAcquire("user1"));
        Assert.Null(limiter.TryAcquire("user1"));
        Assert.NotNull(limiter.TryAcquire("user1")); // blocked

        Assert.Null(limiter.TryAcquire("user2"));
        Assert.Null(limiter.TryAcquire("user2"));
        Assert.NotNull(limiter.TryAcquire("user2")); // blocked

        Assert.Null(limiter.TryAcquire("user3")); // still fine
    }

    [Fact]
    public void DefaultLimits_MatchGroqFreeTier()
    {
        // Verify default constructor matches documented Groq limits
        var limiter = new AiRateLimiter();

        // Should allow at least 10 requests from one user (perUserRpm=10)
        for (int i = 0; i < 10; i++)
            Assert.Null(limiter.TryAcquire("testuser"));

        // 11th should be blocked by per-user RPM
        var result = limiter.TryAcquire("testuser");
        Assert.NotNull(result);
        Assert.Contains("Slow down", result);
    }

    [Fact]
    public void ZeroRequests_NoSideEffects()
    {
        // Just creating a limiter and not calling TryAcquire should be fine
        var limiter = new AiRateLimiter(globalRpm: 1, globalRpd: 1, perUserRpm: 1, perUserRpd: 1);

        // First request allowed
        Assert.Null(limiter.TryAcquire("user1"));

        // Second blocked by all limits
        var result = limiter.TryAcquire("user1");
        Assert.NotNull(result);
    }

    [Fact]
    public void GlobalLimit_AffectsAllUsers()
    {
        var limiter = new AiRateLimiter(globalRpm: 2, globalRpd: 1000, perUserRpm: 100, perUserRpd: 1000);

        Assert.Null(limiter.TryAcquire("alice"));
        Assert.Null(limiter.TryAcquire("bob"));

        // Global RPM hit — even a new user is blocked
        var result = limiter.TryAcquire("charlie");
        Assert.NotNull(result);
        Assert.Contains("busy", result);
    }
}
