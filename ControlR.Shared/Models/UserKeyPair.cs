namespace ControlR.Shared.Models;

public class UserKeyPair : ICloneable
{
    public UserKeyPair(byte[] publicKey, byte[] privateKey, byte[] encryptedPrivateKey)
    {
        PublicKey = publicKey;
        PrivateKey = privateKey;
        EncryptedPrivateKey = encryptedPrivateKey;
    }

    public byte[] EncryptedPrivateKey { get; private set; } = Array.Empty<byte>();
    public byte[] PrivateKey { get; private set; }
    public byte[] PublicKey { get; private set; }

    public object Clone()
    {
        return new UserKeyPair(PublicKey, PrivateKey, EncryptedPrivateKey);
    }
}
