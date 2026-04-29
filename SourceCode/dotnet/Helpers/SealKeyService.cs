using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Text.Json;
using System.Runtime.InteropServices;
using Azure.Storage.Blobs;


public class SealKeyService
{
    private readonly BlobContainerClient _container;
    private SealKeys _cachedKeys;

    public SealKeyService()
    {
        var connStr = Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING");
        var client = new BlobServiceClient(connStr);
        _container = client.GetBlobContainerClient("bfvkeys");
    }

    public async Task initialiseAsync()
    {
        if (!SealNative.initSeal())
            throw new Exception("SEAL init failed");

        bool exists = await keyExists("bfvPublic");
        if (!exists)
        {
            Console.WriteLine("Generating new SEAL keys...");
            var ptr = SealNative.generateKeys();
            var json = Marshal.PtrToStringAnsi(ptr);
            var keys = JsonSerializer.Deserialize<SealKeys>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (keys?.Public == null || keys?.Secret == null || keys?.Relin == null)
                throw new Exception($"Null keys returned. JSON: {json}");

            await uploadBlob("", keys.Public);
            await uploadBlob("bfvSecret", keys.Secret);
            await uploadBlob("bfvRelin", keys.Relin);
            _cachedKeys = keys;
        }
        else
        {
            Console.WriteLine("Loading SEAL keys from Blob Storage...");
            _cachedKeys = new SealKeys
            {
                Public = await downloadBlob("bfvPublic"),
                Secret = await downloadBlob("bfvSecret"),
                Relin  = await downloadBlob("bfvRelin")
            };
        }
    }

    private async Task uploadBlob(string name, string value)
    {
        var blob = _container.GetBlobClient(name);
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(value));
        await blob.UploadAsync(stream, overwrite: true);
    }

    private async Task<string> downloadBlob(string name)
    {
        var blob = _container.GetBlobClient(name);
        var response = await blob.DownloadContentAsync();
        return response.Value.Content.ToString();
    }

    private async Task<bool> keyExists(string name)
    {
        var blob = _container.GetBlobClient(name);
        var response = await blob.ExistsAsync();
        return response.Value;
    }

    public SealKeys getKeys() => _cachedKeys;
}