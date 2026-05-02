using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;



// Get secret from GitHub - not storing any sensitive access creds inside the repo
//var tenantId          = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
//var clientId          = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
var connectionString  = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

// BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER 


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddRouting();


builder.Services.AddHttpContextAccessor();


// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://cybertrack.azurewebsites.net")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});


// Connect to DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));


// JWT key storage
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\home\dataprotection-keys"))
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));


builder.Services.AddSingleton<IDataProtector>(provider =>
    provider.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("AuthCookieProtector"));


builder.Services.AddSingleton<SealKeyService>();

// BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER 


var app = builder.Build();



// APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  


app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Log to console (visible in Kudu/Console or App Service logs)
        Console.WriteLine("Unhandled Exception:");
        Console.WriteLine(ex.ToString());

        // Return 500 to client
        context.Response.StatusCode = 500;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(ex.ToString());
    }
});


app.UseHttpsRedirection();

app.UseDefaultFiles(); 
app.UseStaticFiles();  

app.UseRouting();

app.UseCors();

app.UseDeveloperExceptionPage();

app.MapOrgEndpoints();
app.MapAuthEndpoints();
app.MapDeviceEndpoints();
app.MapFenceEndpoints();
app.MapGPSEndpoints();
app.MapGroupEndpoints();
app.MapCodeEndpoints();
app.MapKeyEndpoints();
app.MapPolicyEndpoints();
app.MapUserEndpoints();
app.MapStatusEndpoints();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Serve React page if request is not for an API endpoint
app.MapFallbackToFile("index.html"); 


// REMOVE BEFORE FINAL VER
//app.UseDeveloperExceptionPage();


app.Run();



// APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  

