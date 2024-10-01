using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MHKAT
{
    public static class TombstoneFileDrive

    {
        private static readonly string connectionString = "DefaultEndpointsProtocol=https;AccountName=ha55an;AccountKey=UDKeDju9w9U1uGkKr2G1c2zyIHc5SQ1b57SUREejkqJCYYImcsgpG+NMZ8A9Q8X5m/t+iU8VR9ev+AStFcwLEQ==;EndpointSuffix=core.windows.net";
        private static readonly string shareName = "productshare";
        private static readonly string directoryName = "uploads";

        [Function("TombstoneFileDrive")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Content-Type", out var contentType) || !contentType.ToString().StartsWith("multipart/form-data"))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid content type. Expected multipart/form-data.");
                return badResponse;
            }

            var shareClient = new ShareClient(connectionString, shareName);
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            var reader = new MultipartReader(contentType.ToString(), req.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                var contentDisposition = section.ContentDisposition;

                if (contentDisposition.Contains("filename="))
                {
                    var fileName = contentDisposition.Split(';')
                                            .FirstOrDefault(x => x.Trim().StartsWith("filename="))?.Split('=')[1]?.Trim('"');

                    if (string.IsNullOrEmpty(fileName))
                    {
                        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                        await badResponse.WriteStringAsync("File name is missing.");
                        return badResponse;
                    }

                    var fileClient = directoryClient.GetFileClient(fileName);

                    using (var memoryStream = new MemoryStream())
                    {
                        await section.Body.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        await fileClient.CreateAsync(memoryStream.Length);
                        await fileClient.UploadAsync(memoryStream);
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("File uploaded successfully.");
            return response;
        }
    }
}














