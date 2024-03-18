using Minio;
using Minio.DataModel.Args;

namespace MinIOWebClient.Services;

public static class Initialiser
{
    public static async Task InitialiseMinioAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<MinioInitialiser>();

        await initialiser.InitialiseAsync();
    }
}

public class MinioInitialiser
{
    private readonly ILogger _logger;
    private readonly IMinioClient _minio;

    public MinioInitialiser(ILogger<MinioInitialiser> logger, IMinioClient minio)
    {
        _logger = logger;
        _minio = minio;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            await _minio.MakeBucketAsync(
                new MakeBucketArgs()
                    .WithBucket("demo"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising minio.");
            throw;
        }
    }
}
