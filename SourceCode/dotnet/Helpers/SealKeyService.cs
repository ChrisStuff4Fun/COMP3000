using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Text.Json;
using System.Runtime.InteropServices;

public class SealKeyService
{
    private readonly SecretClient _client;
    private SealKeys _cachedKeys;

    public SealKeyService()
    {
        _client = new SecretClient(
            new Uri("https://cybertrackserver.vault.azure.net/"),
            new DefaultAzureCredential()
        );
    }

    public async Task initialiseAsync()
    {
        SealNative.initSeal();

        bool exists = await keyExists("bfv-public");

        if (!exists)
        {
            Console.WriteLine("Generating new SEAL keys...");

            var ptr = SealNative.generateKeys();
            var json = Marshal.PtrToStringAnsi(ptr);

            var keys = JsonSerializer.Deserialize<SealKeys>(json);

            // save to Key Vault
            await _client.SetSecretAsync("bfv-public", keys.Public);
            await _client.SetSecretAsync("bfv-secret", keys.Secret);
            await _client.SetSecretAsync("bfv-relin", keys.Relin);

            _cachedKeys = keys;
        }
        else
        {
            Console.WriteLine("Loading SEAL keys from Key Vault...");

            _cachedKeys = new SealKeys
            {
                Public = (await _client.GetSecretAsync("bfv-public")).Value.Value,
                Secret = (await _client.GetSecretAsync("bfv-secret")).Value.Value,
                Relin = (await _client.GetSecretAsync("bfv-relin")).Value.Value
            };
        }
    }

    private async Task<bool> keyExists(string name)
    {
        try
        {
            await _client.GetSecretAsync(name);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public SealKeys getKeys() => _cachedKeys;
}