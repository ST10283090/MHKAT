using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MHKAT
{
    public class Spongeblob
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "products";

        public Spongeblob(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        [Function("Spongeblob")]
        public async Task<HttpResponseData> UploadBlob(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("Spongeblob");
            logger.LogInformation("Processing upload request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestData = JsonSerializer.Deserialize<UploadRequest>(requestBody);

            if (string.IsNullOrEmpty(requestData.FileName) || string.IsNullOrEmpty(requestData.Base64Content))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                badResponse.WriteString("File name or content is missing.");
                return badResponse;
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(requestData.FileName);

            byte[] fileBytes = Convert.FromBase64String(requestData.Base64Content);
            using (var stream = new MemoryStream(fileBytes))
            {
                await blobClient.UploadAsync(stream);
            }

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.WriteString(blobClient.Uri.ToString());
            return response;
        }

        public class UploadRequest
        {
            public string FileName { get; set; }
            public string Base64Content { get; set; }
        }
    }
}





