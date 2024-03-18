using System.Reactive.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Minio;
using Minio.DataModel;
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
    
    [BindProperty]
    public IList<Item> StorageItems { get; set; }

    public async Task OnGetAsync()
    {
        await LoadStorageItems();
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

        await LoadStorageItems();
    }

    private async Task LoadStorageItems()
    {
        string bucketName = "demo";
        string prefix = null;
        bool recursive = true;
        
        var listArgs = new ListObjectsArgs()
            .WithBucket(bucketName)
            .WithPrefix(prefix)
            .WithRecursive(recursive);
        
        StorageItems = await _minio.ListObjectsAsync(listArgs).ToList();
    }
}