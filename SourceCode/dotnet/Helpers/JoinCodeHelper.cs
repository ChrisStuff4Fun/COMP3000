using System.Security.Cryptography;
using System.Text;

public static class JoinCodeGenerator
{
    // Define usable characters
    private static readonly char[] chars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public static string generateJoinCode()
    {

        // Generate 12 random numbers (1 byte max, so 255)
        Span<byte> randomBytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(randomBytes);

        // Start new string builder
        var joinCode = new StringBuilder(14);

        for (int i = 0; i < 12; i++)
        {
            // Add random character
            joinCode.Append(chars[randomBytes[i] % chars.Length]);

            if (i == 3 || i == 7)
                joinCode.Append('-');
        }

        return joinCode.ToString();
    }
}
