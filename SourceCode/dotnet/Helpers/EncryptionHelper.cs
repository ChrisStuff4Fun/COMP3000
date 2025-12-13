public static class EncryptionHelper
{


    public static byte[] HexToBytes(string hex)
    {
        int length = hex.Length;

        if (hex.Length % 2 != 0)
        {
             throw new ArgumentException("Invalid hex string length");
        }
           
        byte[] bytes = new byte[length / 2];

        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        
        return bytes;
    }



    public static string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "");
    }


    public static List<String> decryptInbound()
    {
        return null;
    }


}

