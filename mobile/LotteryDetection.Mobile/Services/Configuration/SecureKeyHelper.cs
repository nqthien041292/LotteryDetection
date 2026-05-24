using System;
using System.Text;

namespace LotteryDetection.Mobile.Services.Configuration;

public static class SecureKeyHelper
{
    // XOR obfuscated bytes of the key with mask 0x5A
    private static readonly byte[] ObfuscatedKeyBytes = new byte[]
    {
        0x11, 0x79, 0x63, 0x2A, 0x1A, 0x37, 0x0B, 0x7E, 0x20, 0x02, 
        0x6D, 0x7C, 0x28, 0x0C, 0x7B, 0x68, 0x2D, 0x0E, 0x70, 0x62, 
        0x23, 0x0A, 0x7F, 0x69, 0x38, 0x14, 0x04, 0x6C, 0x2C, 0x19, 
        0x7E, 0x6F
    };

    private const byte XorMask = 0x5A;

    public static string GetDecryptedKey()
    {
        var decryptedBytes = new byte[ObfuscatedKeyBytes.Length];
        for (int i = 0; i < ObfuscatedKeyBytes.Length; i++)
        {
            decryptedBytes[i] = (byte)(ObfuscatedKeyBytes[i] ^ XorMask);
        }
        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
