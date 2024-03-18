using Minio;
using MinIOWebClient.Models;
using MinIOWebClient.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddMinio(configureClient =>
{
    var settings = builder
        .Configuration
        .GetSection("Minio")
        .Get<MinioSettings>();
    
    configureClient
        .WithEndpoint(settings.Endpoint, settings.Port)
        .WithSSL(false)
        .WithCredentials(settings.AccessKey, settings.SecretKey);
});

builder.Services.AddScoped<MinioInitialiser>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

await app.InitialiseMinioAsync();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();