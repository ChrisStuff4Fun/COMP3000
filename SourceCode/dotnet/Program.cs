using Microsoft.EntityFrameworkCore;

// Get secret from GitHub - not storing any sensitive access creds inside the repo
var tenantId         = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
var clientId         = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");



// BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER // BUILDER 


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddHttpContextAccessor();

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

// Serve React page if request is not for an API endpoint
app.UseDefaultFiles(); 
app.UseStaticFiles();  
app.MapFallbackToFile("index.html"); 


// REMOVE BEFORE FINAL VER
app.UseDeveloperExceptionPage();

app.MapKeyEndpoints();
app.MapDeviceEndpoints();
app.MapUserEndpoints();
app.MapPolicyEndpoints();
app.MapCodeEndpoints();

app.Run();



// APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  // APP  
