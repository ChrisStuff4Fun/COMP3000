using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.Text.Json;
using System.Runtime.InteropServices;
using Azure.Storage.Blobs;


public class SealKeyService
{

    private bool _initialised = false;
    private readonly BlobContainerClient _container;
    private SealKeys _cachedKeys;
    private static readonly SemaphoreSlim _sealLock = new SemaphoreSlim(1, 1);
    public SemaphoreSlim SealLock => _sealLock;

    public SealKeyService()
    {
        var connStr = Environment.GetEnvironmentVariable("BLOB_CONNECTION_STRING");
        var client = new BlobServiceClient(connStr);
        _container = client.GetBlobContainerClient("bfvkeys");
    }

    public async Task initialiseAsync()
    {

         if (_initialised) return;

        if (!SealNative.initSeal())
            throw new Exception("SEAL init failed");

        bool exists = await keyExists("bfv-public");
        if (!exists)
        {
            Console.WriteLine("Generating new SEAL keys...");
            var ptr = SealNative.generateKeys();
            var json = Marshal.PtrToStringAnsi(ptr);

            var keys = JsonSerializer.Deserialize<SealKeys>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            await File.WriteAllTextAsync("C:\\home\\sealkeys_debug2.txt",  $"Public null: {keys?.Public == null}\nSecret null: {keys?.Secret == null}\nRelin null: {keys?.Relin == null}\nPublic length: {keys?.Public?.Length}\nSecret length: {keys?.Secret?.Length}\nRelin length: {keys?.Relin?.Length}");

            if (keys?.Public == null || keys?.Secret == null || keys?.Relin == null)
                throw new Exception($"Null keys returned. JSON: {json}");

            await uploadBlob("bfv-public", keys.Public);
            await File.WriteAllTextAsync("C:\\home\\sealkeys_debug3.txt", "uploaded public");
            await uploadBlob("bfv-secret", keys.Secret);
            await File.WriteAllTextAsync("C:\\home\\sealkeys_debug3.txt", "uploaded secret");
            await uploadBlob("bfv-relin", keys.Relin);
            await File.WriteAllTextAsync("C:\\home\\sealkeys_debug3.txt", "uploaded relin");
            _cachedKeys = keys;

            if (!SealNative.loadSecretKey(_cachedKeys.Secret))
                throw new Exception("Failed to load secret key into SEAL");

        }
        else
        {
            Console.WriteLine("Loading SEAL keys from Blob Storage...");
            _cachedKeys = new SealKeys
            {
                Public  = await downloadBlob("bfv-public"),
                Secret  = await downloadBlob("bfv-secret"),
                Relin   = await downloadBlob("bfv-relin")
            };
            if (!SealNative.loadSecretKey(_cachedKeys.Secret))
                throw new Exception("Failed to load secret key into SEAL");

        }

        _initialised = true;

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

    public async Task<T> SealLock<T>(Func<T> sealOperation)
    {
        await _sealLock.WaitAsync();
        try
        {
            return sealOperation();
        }
        finally
        {
            _sealLock.Release();
        }
    }

}

