using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Minio;
using Minio.DataModel.Args;

namespace MinIOWebClient.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IMinioClient _minio; 

    public IndexModel(ILogger<IndexModel> logger, IMinioClient minio)
    {
        _logger = logger;
        _minio = minio;
    }

    public void OnGet()
    {
    }

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public async Task OnPostAsync()
    {
        if (Upload is not null)
        {
            await using Stream ms = new MemoryStream();
            await Upload.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            
            var args = new PutObjectArgs()
                .WithBucket("demo")
                .WithObject(Upload.FileName)
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType("application/octet-stream");
            
            await _minio.PutObjectAsync(args).ConfigureAwait(false);
        }
    }
}