using CommunityToolkit.Mvvm.Messaging;
using ControlR.Shared;
using ControlR.Shared.Models;
using ControlR.Viewer.Extensions;
using ControlR.Viewer.Models.Messages;

namespace ControlR.Viewer.Services;

internal interface ISettings
{
    string KeypairExportPath { get; set; }
    string PrivateKey { get; set; }
    byte[] PrivateKeyBytes { get; set; }
    string PublicKey { get; set; }
    bool RememberPassphrase { get; set; }
    string Username { get; set; }
    Task<string> GetEncryptedPrivateKey();
    Task<byte[]> GetEncryptedPrivateKeyBytes();
    Task<string> GetPassphrase();
    Task SetEncryptedPrivateKey(string value);

    Task SetEncryptedPrivateKeyBytes(byte[] value);

    Task SetPassphrase(string passphrase);
    Task UpdateKeypair(string username, UserKeyPair keypair);
    Task UpdateKeypair(KeypairExport export);
}

internal class Settings : ISettings
{
    private readonly IMessenger _messenger;
    private readonly IPreferences _preferences;
    private readonly ISecureStorage _secureStorage;
    private string _privateKey = string.Empty;

    public Settings(
        ISecureStorage secureStorage,
        IPreferences preferences,
        IMessenger messenger)
    {
        _secureStorage = secureStorage;
        _preferences = preferences;
        _messenger = messenger;
    }

    public string KeypairExportPath
    {
        get => _preferences.Get(nameof(KeypairExportPath), string.Empty);
        set => _preferences.Set(nameof(KeypairExportPath), value);
    }

    public string PrivateKey
    {
        get => _privateKey;
        set => _privateKey = value;
    }

    public byte[] PrivateKeyBytes
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_privateKey))
            {
                return Array.Empty<byte>();
            }
            return Convert.FromBase64String(_privateKey);
        }
        set
        {
            _privateKey = Convert.ToBase64String(value);
        }
    }

    public string PublicKey
    {
        get => _preferences.Get(nameof(PublicKey), string.Empty);
        set => _preferences.Set(nameof(PublicKey), value);
    }

    public bool RememberPassphrase
    {
        get => _preferences.Get(nameof(RememberPassphrase), false);
        set => _preferences.Set(nameof(RememberPassphrase), value);
    }

    public string ServerUri => _preferences.Get(nameof(ServerUri), AppConstants.ServerUri);

    public string Username
    {
        get => _preferences.Get(nameof(Username), string.Empty);
        set => _preferences.Set(nameof(Username), value);
    }

    public async Task<string> GetEncryptedPrivateKey()
    {
        return await _secureStorage.GetAsync("EncryptedPrivateKey");
    }

    public async Task<byte[]> GetEncryptedPrivateKeyBytes()
    {
        var stored = await _secureStorage.GetAsync("EncryptedPrivateKey");
        if (string.IsNullOrWhiteSpace(stored))
        {
            return Array.Empty<byte>();
        }
        return Convert.FromBase64String(stored);
    }

    public async Task<string> GetPassphrase()
    {
        return await _secureStorage.GetAsync("Passphrase");
    }
    public async Task SetEncryptedPrivateKey(string value)
    {
        await _secureStorage.SetAsync("EncryptedPrivateKey", value);
    }
    public async Task SetEncryptedPrivateKeyBytes(byte[] value)
    {
        await _secureStorage.SetAsync("EncryptedPrivateKey", Convert.ToBase64String(value));
    }

    public async Task SetPassphrase(string passphrase)
    {
        await _secureStorage.SetAsync("Passphrase", passphrase);
    }

    public async Task UpdateKeypair(string username, UserKeyPair keypair)
    {
        Username = username;
        PublicKey = keypair.PublicKeyBase64;
        PrivateKey = keypair.PrivateKeyBase64;
        await SetEncryptedPrivateKey(keypair.EncryptedPrivateKeyBase64);
        _messenger.SendParameterlessMessage(ParameterlessMessageKind.AuthStateChanged);
    }

    public async Task UpdateKeypair(KeypairExport export)
    {
        Username = export.Username;
        PublicKey = export.PublicKeyBase64;
        await SetEncryptedPrivateKey(export.EncryptedPrivateKeyBase64);
        _messenger.SendParameterlessMessage(ParameterlessMessageKind.AuthStateChanged);
    }
}
