namespace DominateDocsData.Helpers;

public interface IEncryptAes
{
    string Decrypt(string cipherText);

    string Encrypt(string textString);
}