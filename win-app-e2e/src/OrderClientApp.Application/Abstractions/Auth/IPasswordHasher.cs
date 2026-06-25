namespace OrderClientApp.Application.Abstractions.Auth;

public interface IPasswordHasher
{
    PasswordHashResult HashPassword(string password);

    bool VerifyPassword(string password, string hash, string salt);
}
