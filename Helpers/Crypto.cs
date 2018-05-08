using System;
using System.Security.Cryptography;

static class Crypto {

    public static string SecureRandomString(int keyLength = 33) {
        RNGCryptoServiceProvider rngCryptoServiceProvider = new RNGCryptoServiceProvider();
        byte[] randomBytes = new byte[keyLength];
        rngCryptoServiceProvider.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).Replace('/', '0');
    }

}