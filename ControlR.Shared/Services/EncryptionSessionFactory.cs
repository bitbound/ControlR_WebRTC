using Microsoft.Extensions.Logging;

namespace ControlR.Shared.Services;

public interface IEncryptionSessionFactory
{
    IEncryptionSession CreateSession();
}

internal class EncryptionSessionFactory : IEncryptionSessionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public EncryptionSessionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }
    public IEncryptionSession CreateSession()
    {
        return new EncryptionSession(_loggerFactory.CreateLogger<EncryptionSession>());
    }
}
