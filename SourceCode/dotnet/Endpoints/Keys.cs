
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.AspNetCore.Mvc;

public static class KeyEndpoints
{
    public static void MapKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var keys = app.MapGroup("/keys");

        // Map endpoints
        keys.MapGet("/public", servePublicKey);
        keys.MapGet("/register/{inboundMessage:string}", registerDeviceKeys);

        keys.MapGet("/test", test);

    }



    private static async Task<IResult> test()
    {


        return Results.Ok("hello");
    }

    // Methods for endpoints
    private static async Task<IResult> servePublicKey([FromServices] AppDbContext db, [FromServices] IHttpContextAccessor httpAccessor)
    {

        Console.WriteLine("Public key requested");
        
        try {
            Uri keyVaultUri = new Uri("https://cybertrackserver.vault.azure.net/");
            KeyClient keyClient = new KeyClient(keyVaultUri, new DefaultAzureCredential());

            // Fetch the key
            KeyVaultKey key = await keyClient.GetKeyAsync("CyberTrackServerKeys");

            var publicKey = new
            {
                Curve = key.Key.CurveName,
                X = Convert.ToBase64String(key.Key.X),
                Y = Convert.ToBase64String(key.Key.Y)
            };

            return Results.Ok(publicKey);
        }
        catch(Exception e)
        {
            return Results.Problem(detail: e.ToString(), statusCode: 500);
        }

    }



    private static async Task<IResult> registerDeviceKeys( String inboundMessage, AppDbContext db, IHttpContextAccessor httpAccessor)
    {


        return Results.Ok();
    }

    

}
