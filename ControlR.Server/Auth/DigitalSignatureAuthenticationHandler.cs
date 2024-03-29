﻿using ControlR.Server.Data;
using ControlR.Shared;
using ControlR.Shared.Dtos;
using ControlR.Shared.Extensions;
using ControlR.Shared.Services;
using MessagePack;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ControlR.Server.Auth;

public class DigitalSignatureAuthenticationHandler(
    UrlEncoder encoder,
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<DigitalSignatureAuthenticationHandler> logger) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    private readonly ILogger<DigitalSignatureAuthenticationHandler> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        using var _ = _logger.BeginScope(nameof(HandleAuthenticateAsync));

        var authHeader = Context.Request.Headers.Authorization.FirstOrDefault(x =>
            x?.StartsWith(AuthSchemes.DigitalSignature) == true);

        if (string.IsNullOrWhiteSpace(authHeader))
        {
            return AuthenticateResult
                .Fail($"{AuthSchemes.DigitalSignature} authorization is missing.")
                .AsTaskResult();
        }

        try
        {
            if (!TryGetSignedPayload(authHeader, out var signedDto))
            {
                return AuthenticateResult
                    .Fail("Failed to parse authorization header.")
                    .AsTaskResult();
            }

            return VerifySignature(signedDto).AsTaskResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse header {authHeader}.", authHeader);
            return AuthenticateResult.Fail("An error occurred on the server.").AsTaskResult();
        }
    }

    private bool TryGetSignedPayload(string header, out SignedPayloadDto signedPayload)
    {
        var base64Token = header.Split(" ", 2).Skip(1).First();
        var payloadBytes = Convert.FromBase64String(base64Token);
        signedPayload = MessagePackSerializer.Deserialize<SignedPayloadDto>(payloadBytes);

        if (signedPayload.DtoType != DtoType.PublicKey)
        {
            _logger.LogWarning("Unexpected DTO type of {type}.", signedPayload.DtoType);
            return false;
        }

        return true;
    }

    private AuthenticateResult VerifySignature(SignedPayloadDto signedDto)
    {
        using var scope = _scopeFactory.CreateScope();
        using var appDb = scope.ServiceProvider.GetRequiredService<AppDb>();
        var encryptionFactory = scope.ServiceProvider.GetRequiredService<IEncryptionSessionFactory>();
        using var encryptor = encryptionFactory.CreateSession();

        var account = MessagePackSerializer.Deserialize<PublicKeyDto>(signedDto.Payload);
        var publicKey = account.PublicKey;

        if (publicKey.Length == 0)
        {
            return AuthenticateResult.Fail("No public key found in the payload.");
        }
        encryptor.ImportPublicKey(publicKey);
        var result = encryptor.Verify(signedDto.Payload, signedDto.Signature);

        if (!result)
        {
            return AuthenticateResult.Fail("Digital siganture verification failed.");
        }

        var claims = new Claim[]
        {
            new Claim(ClaimNames.PublicKey, signedDto.PublicKeyBase64),
            new Claim(ClaimNames.Username, account.Username),
        };

        var identity = new ClaimsIdentity(claims, AuthSchemes.DigitalSignature);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, AuthSchemes.DigitalSignature));
    }
}
