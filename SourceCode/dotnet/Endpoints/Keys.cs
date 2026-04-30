
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


public static class KeyEndpoints
{
    public static void MapKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var keys = app.MapGroup("/keys");

        // Map endpoints
        keys.MapGet("/public", servePublicKey);
        keys.MapGet("/bfv", serveBFV);
        keys.MapPost("/register", registerDeviceKeys);

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



    private static async Task<IResult> serveBFV([FromServices] AppDbContext db, [FromServices] IHttpContextAccessor httpAccessor, [FromServices] SealKeyService bfvService)
    {

        Console.WriteLine("BFV key requested");
        
        try {
            await bfvService.initialiseAsync();

            var keys = bfvService.getKeys();

            return Results.Ok(new { publicBFV = keys.Public });
        }
        catch(Exception e)
        {
            return Results.Problem(detail: e.ToString(), statusCode: 500);
        }

    }



    private static async Task<IResult> registerDeviceKeys( KeyExchange inboundMessage, [FromServices] AppDbContext db, [FromServices] IHttpContextAccessor httpAccessor)
    {
        // No auth since its from an app
        
        if (inboundMessage == null || inboundMessage.Code == null) return Results.BadRequest("Invalid request, no code provided");

        DeviceJoinCode? dbJoinCode = await db.DeviceJoinCodes.FirstOrDefaultAsync(j => j.Code == inboundMessage.Code);

        // Error cases 
        if (dbJoinCode == null) return Results.NotFound("Code was not found in database");
        if (dbJoinCode.IsUsed) return Results.BadRequest("Join code already used.");
        if (dbJoinCode.ExpiryDate < DateTime.UtcNow) return Results.BadRequest("Join code has expired.");

        // Set vars for new device entity
        Device device = new Device();
        device.DeviceName = inboundMessage.DeviceName;
        device.OrgID      = dbJoinCode.OrgID;
        device.PublicKeyX = inboundMessage.X;
        device.PublicKeyY = inboundMessage.Y;

        // Update fields
        dbJoinCode.IsUsed = true;

        db.Devices.Add(device);

        // Save 
        await db.SaveChangesAsync();

        return Results.Ok(new {deviceId = device.DeviceID});

    }

    

}
