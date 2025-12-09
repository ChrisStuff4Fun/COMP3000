using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;


// Get secret from GitHub - not storing any sensitive access creds inside the repo
var tenantId         = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
var clientId         = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");



// BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER 


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Process to auth current user
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience = clientId;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.AddAuthorization();

// Connect to DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));


// BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER 



var app = builder.Build();



// APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// DO NOT UNCOMMENT PRE-REACT APP
//app.UseAuthentication(); // Add authentication middleware
//app.UseAuthorization();  // Add authorisation middleware


// Serve React page if request is not for an API endpoint
app.UseDefaultFiles(); 
app.UseStaticFiles();  
app.MapFallbackToFile("index.html"); 

app.Run();



// APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  
