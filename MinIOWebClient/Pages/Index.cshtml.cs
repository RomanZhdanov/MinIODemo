using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Timers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using MinIOWebClient.Extensions;
using MinIOWebClient.Models;

namespace MinIOWebClient.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IMinioClient _minio;

    private const int Expiry = 60 * 60 * 12;
    private const string BucketName = "demo";

    public IndexModel(ILogger<IndexModel> logger, IMinioClient minio)
    {
        _logger = logger;
        _minio = minio;
    }
    
    [BindProperty]
    public IList<Item>? StorageItems { get; set; }

    public async Task OnGetAsync()
    {
        await LoadStorageItems();
    }

    [BindProperty]
    public string ExpireString
    {
        get
        {
            TimeSpan time = TimeSpan.FromSeconds(Expiry);
            return time.ToReadableString();
        }
    }

    [BindProperty]
    [Required]
    public IFormFile? Upload { get; set; }
    [BindProperty]
    public string UploadUrl { get; set; }
    [BindProperty] 
    public string UploadFileName { get; set; }
    [BindProperty]
    public string UploadTimeString { get; set; }
    [BindProperty]
    public string UploadRequestTimeString { get; set; }

    public async Task OnPostAsync()
    {
        if (Upload is not null)
        {
            var requestSw = Stopwatch.StartNew();
            await using Stream ms = new MemoryStream();
            await Upload.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            
            
            var putArgs = new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(Upload.FileName)
                .WithStreamData(ms)
                .WithObjectSize(ms.Length)
                .WithContentType("application/octet-stream");
            
            var uploadSw = Stopwatch.StartNew();
            await _minio.PutObjectAsync(putArgs).ConfigureAwait(false);
            uploadSw.Stop();

            var getArgs = new PresignedGetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(Upload.FileName)
                .WithExpiry(Expiry);
            
            UploadUrl = await _minio.PresignedGetObjectAsync(getArgs);
            requestSw.Stop();
            var uploadTime = TimeSpan.FromMilliseconds(uploadSw.ElapsedMilliseconds);
            var requestTime = TimeSpan.FromMilliseconds(requestSw.ElapsedMilliseconds);
            UploadFileName = Upload.FileName;
            UploadTimeString = uploadTime.ToReadableString();
            UploadRequestTimeString = requestTime.ToReadableString();
        }

        await LoadStorageItems();
    }

    private async Task LoadStorageItems()
    {
        string prefix = null;
        bool recursive = true;
        
        var listArgs = new ListObjectsArgs()
            .WithBucket(BucketName)
            .WithPrefix(prefix)
            .WithRecursive(recursive);
        
        var items = await _minio.ListObjectsAsync(listArgs)
            .ToList();

        StorageItems = items.OrderByDescending(i => i.LastModifiedDateTime).ToList();
    }

    public async Task<IActionResult> OnGetDownloadFile(string fileName)
    {
        byte[] file = null;
        
        var args = new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(fileName)
            .WithCallbackStream((stream) =>
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                file = memoryStream.ToArray();
            });
        
        var obj = await _minio.GetObjectAsync(args);

        return File(file, obj.ContentType, obj.ObjectName);
    }

    public async Task<IActionResult> OnGetDeleteFile(string fileName)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(BucketName)
            .WithObject(fileName);

        await _minio.RemoveObjectAsync(args);

        return RedirectToPage("Index");
    }

    public async Task<PartialViewResult> OnGetShareUrl(string fileName)
    {
        var getArgs = new PresignedGetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(fileName)
            .WithExpiry(Expiry);
            
        var shareUrl = await _minio.PresignedGetObjectAsync(getArgs);

        return Partial("_ShareUrlModal", new ShareModel
        {
            ObjectName = fileName,
            ShareUrl = shareUrl,
            ExpireString = ExpireString
        });
    }
}