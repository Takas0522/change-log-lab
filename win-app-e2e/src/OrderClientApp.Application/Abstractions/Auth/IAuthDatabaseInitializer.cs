namespace OrderClientApp.Application.Abstractions.Auth;

public interface IAuthDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
