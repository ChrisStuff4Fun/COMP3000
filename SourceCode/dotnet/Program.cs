using Microsoft.EntityFrameworkCore;

// Get secret from GitHub - not storing any sensitive access creds inside the repo
var tenantId          = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
var clientId          = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
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


// BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER 



var app = builder.Build();



// APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  



app.UseRouting();

app.UseCors();

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


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve React page if request is not for an API endpoint
app.UseDefaultFiles(); 
app.UseStaticFiles();  
app.MapFallbackToFile("index.html"); 


// REMOVE BEFORE FINAL VER
//app.UseDeveloperExceptionPage();


app.Run();



// APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  
