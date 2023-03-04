using ControlR.Shared;
using ControlR.Shared.DbEntities;
using ControlR.Shared.Dtos;
using ControlR.Shared.Interfaces;
using ControlR.Shared.Services;
using MessagePack;
using System.Collections.Concurrent;
using System.Net.Http.Headers;

namespace ControlR.Viewer.Services;

internal interface IHttpConfigurer
{
    void ConfigureAuthenticatedClient(HttpClient client);
    HttpClient GetAuthorizedClient();
    string GetDigitalSignature();
    string GetDigitalSignature(PublicKeyDto keyDto);
    void UpdateClientAuthorizations(PublicKeyDto keyDto);
}

internal class HttpConfigurer : IHttpConfigurer
{
    public readonly IHttpClientFactory _clientFactory;
    private static readonly ConcurrentBag<HttpClient> _clients = new();
    private readonly ISettings _settings;
    private readonly IAppState _appState;

    public HttpConfigurer(
        IHttpClientFactory clientFactory,
        ISettings settings,
        IAppState appState)
    {
        _clientFactory = clientFactory;
        _settings = settings;
        _appState = appState;
    }

    public void ConfigureAuthenticatedClient(HttpClient client)
    {
        var keyDto = new PublicKeyDto()
        {
            PublicKey = _settings.PublicKey,
            Username = _settings.Username
        };

        var signature = GetDigitalSignature(keyDto);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthSchemes.DigitalSignature, signature);
        client.BaseAddress = new Uri(AppConstants.ServerUri);

        _clients.Add(client);
    }

    public HttpClient GetAuthorizedClient()
    {
        var client = _clientFactory.CreateClient();
        ConfigureAuthenticatedClient(client);
        return client;
    }

    public string GetDigitalSignature(PublicKeyDto keyDto)
    {
        var signedDto = _appState.Encryptor.CreateSignedDto(keyDto, DtoType.PublicKey, keyDto.PublicKey);
        var dtoBytes = MessagePackSerializer.Serialize(signedDto);
        var base64Payload = Convert.ToBase64String(dtoBytes);
        return base64Payload;
    }

    public string GetDigitalSignature()
    {
        return GetDigitalSignature(_appState.GetPublicKeyDto());
    }
    public void UpdateClientAuthorizations(PublicKeyDto keyDto)
    {

        var signature = GetDigitalSignature(keyDto);

        foreach (var client in _clients)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthSchemes.DigitalSignature, signature);
        }
    }
}