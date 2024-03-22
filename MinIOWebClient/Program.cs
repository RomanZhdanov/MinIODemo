using System.Globalization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Minio;
using MinIOWebClient.Models;
using MinIOWebClient.Services;

var supportedCultures = new[]
{
    new CultureInfo("ru-RU"),                

};

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

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = 5000; // Limit on individual form values
    x.MultipartBodyLengthLimit = 737280000; // Limit on form body size
    x.MultipartHeadersLengthLimit = 737280000; // Limit on form header size
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 837280000; // Limit on request body size
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions{
    DefaultRequestCulture = new RequestCulture("ru-RU"),
    SupportedCultures=supportedCultures,
    SupportedUICultures=supportedCultures
});

await app.InitialiseMinioAsync();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();