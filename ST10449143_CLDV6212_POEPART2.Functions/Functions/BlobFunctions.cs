using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ST10449143_CLDV6212_POEPART1.Functions.Functions
{
    public class BlobFunctions
    {
        [Function("OnProductImageUploaded")]
        public void OnProductImageUploaded(
            [BlobTrigger("%BLOB_PRODUCT_IMAGES%/{name}", Connection = "STORAGE_CONNECTION")] Stream blob,
            string name,
            FunctionContext ctx)
        {
            var log = ctx.GetLogger("OnProductImageUploaded");
            log.LogInformation($"Product image uploaded: {name}, size={blob.Length} bytes");
        }
    }
}