using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
