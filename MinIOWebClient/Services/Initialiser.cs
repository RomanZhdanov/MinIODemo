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
            var bucketName = "demo";
            
            var beArgs = new BucketExistsArgs()
                .WithBucket(bucketName);
            bool found = await _minio.BucketExistsAsync(beArgs).ConfigureAwait(false);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs()
                    .WithBucket(bucketName);
                await _minio.MakeBucketAsync(mbArgs).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising minio.");
            throw;
        }
    }
}
