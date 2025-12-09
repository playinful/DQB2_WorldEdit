using Godot;
using System;
using System.IO;
using System.Security.Cryptography;

public partial class AesTest : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        string original = "Here is some data to encrypt!";

		using (Aes myAes = Aes.Create())
        {
            byte[] encrypted = EncryptStringToBytes_Aes(original, myAes.Key, myAes.IV);

			string roundtrip = DecryptStringFromBytes_Aes(encrypted, myAes.Key, myAes.IV);

			GD.Print($"Original: {original}");
			GD.Print($"Round trip: {roundtrip}");
        }
    }
	static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
    {
        if (plainText == null || plainText.Length <= 0)
			throw new ArgumentNullException("plainText");
		if (Key == null || Key.Length <= 0)
			throw new ArgumentNullException("Key");
		if (IV == null || IV.Length <= 0)
			throw new ArgumentNullException("IV");
		
		byte[] encrypted;

		using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
			aesAlg.IV = IV;

			ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

			using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                }

				encrypted = msEncrypt.ToArray();
            }
        }

		return encrypted;
    }
	public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
    {
        if (cipherText == null || cipherText.Length <= 0)
			throw new ArgumentNullException("cipherText");
		if (Key == null || Key.Length <= 0)
			throw new ArgumentNullException("Key");
		if (IV == null || IV.Length <= 0)
			throw new ArgumentNullException("IV");
		
		string plaintext = null;

		using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
			aesAlg.IV = IV;

			ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

			using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        plaintext = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

		return plaintext;
    }
}
