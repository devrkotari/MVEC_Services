using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mvec.Contracts.Events;
using Mvec.Identity.Api.Application.Contracts;
using Mvec.Identity.Api.Domain;
using Xunit;

namespace Mvec.Identity.Tests;

public class AuthServiceTests
{
    private static RegisterRequest NewBuyer(string email = "buyer@example.com") =>
        new(email, "Passw0rd!", "Test", "Buyer", UserType.Buyer);

    [Fact]
    public async Task Register_new_email_succeeds_and_publishes_UserRegistered()
    {
        using var h = new AuthTestHarness();

        var result = await h.Auth.RegisterAsync(NewBuyer());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("buyer@example.com");
        result.Value.UserType.Should().Be(UserType.Buyer);
        h.Events.Published.Should().ContainSingle().Which.Should().BeOfType<UserRegistered>();
    }

    [Fact]
    public async Task Register_assigns_matching_role()
    {
        using var h = new AuthTestHarness();

        var result = await h.Auth.RegisterAsync(NewBuyer());

        result.IsSuccess.Should().BeTrue();
        var buyerRole = await h.Db.Roles.SingleAsync(r => r.Name == "Buyer");
        var link = await h.Db.UserRoles.SingleAsync(ur => ur.UserId == result.Value!.Id);
        link.RoleId.Should().Be(buyerRole.Id);
    }

    [Fact]
    public async Task Register_duplicate_email_is_rejected_with_conflict()
    {
        using var h = new AuthTestHarness();
        await h.Auth.RegisterAsync(NewBuyer());

        var second = await h.Auth.RegisterAsync(NewBuyer("BUYER@example.com")); // case-insensitive

        second.IsSuccess.Should().BeFalse();
        second.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task Register_admin_type_is_rejected()
    {
        using var h = new AuthTestHarness();

        var result = await h.Auth.RegisterAsync(
            new RegisterRequest("admin@example.com", "Passw0rd!", "Ad", "Min", UserType.Admin));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("validation");
    }

    [Fact]
    public async Task Login_with_valid_credentials_issues_tokens()
    {
        using var h = new AuthTestHarness();
        await h.Auth.RegisterAsync(NewBuyer());

        var result = await h.Auth.LoginAsync(new LoginRequest("buyer@example.com", "Passw0rd!"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Tokens.Should().NotBeNull();
        result.Value.Tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_with_wrong_password_fails()
    {
        using var h = new AuthTestHarness();
        await h.Auth.RegisterAsync(NewBuyer());

        var result = await h.Auth.LoginAsync(new LoginRequest("buyer@example.com", "WrongPass1!"));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Suspended_user_cannot_log_in()
    {
        using var h = new AuthTestHarness();
        await h.Auth.RegisterAsync(NewBuyer());
        var user = await h.Db.Users.SingleAsync();
        user.Suspend();
        await h.Db.SaveChangesAsync();

        var result = await h.Auth.LoginAsync(new LoginRequest("buyer@example.com", "Passw0rd!"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("account_suspended");
    }

    [Fact]
    public async Task Refresh_rotates_token_and_old_token_is_rejected_on_reuse()
    {
        using var h = new AuthTestHarness();
        await h.Auth.RegisterAsync(NewBuyer());
        var login = await h.Auth.LoginAsync(new LoginRequest("buyer@example.com", "Passw0rd!"));
        var original = login.Value!.Tokens!.RefreshToken;

        var rotated = await h.Auth.RefreshAsync(original);
        rotated.IsSuccess.Should().BeTrue();
        rotated.Value!.RefreshToken.Should().NotBe(original);

        var reuse = await h.Auth.RefreshAsync(original);
        reuse.IsSuccess.Should().BeFalse();

        var again = await h.Auth.RefreshAsync(rotated.Value.RefreshToken);
        again.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_login_currently_completes_directly_two_factor_challenge_disabled()
    {
        using var h = new AuthTestHarness();
        var (admin, _) = await SeedAdminWith2faAsync(h);

        var login = await h.Auth.LoginAsync(new LoginRequest(admin.Email, "Admin#12345"));

        login.IsSuccess.Should().BeTrue();
        login.Value!.TwoFactorRequired.Should().BeFalse();
        login.Value.Tokens.Should().NotBeNull();
    }

    [Fact]
    public async Task CompleteTwoFactorAsync_with_valid_totp_issues_tokens()
    {
        using var h = new AuthTestHarness();
        var (admin, secret) = await SeedAdminWith2faAsync(h);

        var challenge = h.Jwt.CreateTwoFactorChallenge(admin);
        var code = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret)).ComputeTotp();

        var completed = await h.Auth.CompleteTwoFactorAsync(new TwoFactorLoginRequest(challenge, code));

        completed.IsSuccess.Should().BeTrue();
        completed.Value!.Tokens.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        using var h = new AuthTestHarness();
        await h.Auth.RegisterAsync(NewBuyer());
        var login = await h.Auth.LoginAsync(new LoginRequest("buyer@example.com", "Passw0rd!"));
        var user = await h.Db.Users.SingleAsync();

        var logout = await h.Auth.LogoutAsync(user.Id, login.Value!.Tokens!.RefreshToken);
        logout.IsSuccess.Should().BeTrue();

        var refresh = await h.Auth.RefreshAsync(login.Value.Tokens.RefreshToken);
        refresh.IsSuccess.Should().BeFalse();
    }

    private static async Task<(User Admin, string Secret)> SeedAdminWith2faAsync(AuthTestHarness h)
    {
        var secret = h.Totp.GenerateSecret();
        var admin = User.CreateLocal(
            "admin@mvec.local",
            new Mvec.Identity.Api.Infrastructure.Security.Pbkdf2PasswordHasher().Hash("Admin#12345"),
            "Platform", "Admin", UserType.Admin);
        admin.EnableTwoFactor(secret);
        h.Db.Users.Add(admin);
        await h.Db.SaveChangesAsync();
        return (admin, secret);
    }
}
