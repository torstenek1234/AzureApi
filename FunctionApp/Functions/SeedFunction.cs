using Services;
using Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Azure.Cosmos;

using System.Net.Http;



namespace Examensarbete.Function;

public class SeedFunction
{
    private readonly ILogger<SeedFunction> _logger;
    private readonly IUserService _service;


    public SeedFunction(ILogger<SeedFunction> logger, IUserService service)
    {
        _logger = logger;
        _service = service;
    }

  
    #region Create
    [Function("CreateSeed")]
    public async Task<MultiResponse> CreateSeed([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "seed/create")] 
        HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {

            var logger = executionContext.GetLogger("CreateSeed");
            logger.LogInformation("C# HTTP trigger function CreateSeed processed a request.");

            // //deleting seeded data before reseeding
            // var delResponse = await _service.DeleteSeed();


            //input from query parameter
            string? input = req.Query["count"];
            int count;
            bool parseSuccess = int.TryParse(input, out count);
            if (parseSuccess != true)
            {
                count = 0;
            }


            // setting response message and status
            // var message = parseSuccess ? $"Seeding {count} users. {delResponse.Message}" : "Error count not in query parameter";
            var message = parseSuccess ? $"Seeding {count} users." : "Error count not in query parameter";

            var response = req.CreateResponse(parseSuccess ? HttpStatusCode.OK : HttpStatusCode.BadRequest);


            //creating user documents
            var documents = _service.CreateSeed(count);

            var respData = new ResponseData()
            {
                //Data = documents.Document,
                Message = message
            };


            await response.WriteAsJsonAsync(respData);


            // Returns a response to both HTTP trigger and Azure Cosmos DB output binding. https://github.com/Azure/azure-functions-dotnet-worker/issues/2205#issuecomment-1975095296
            return new MultiResponse()
            {
                Documents = documents.Documents != null && documents.Documents.Any() ? documents.Documents : null,
                HttpResponse = response
            };

        }
        catch (CosmosException ex)
        {
            var errorDetails = $"Status: {ex.StatusCode}\n" +
                            $"Message: {ex.Message}\n" +
                            $"ActivityId: {ex.ActivityId}\n" +
                            $"ResponseBody: {ex.ResponseBody}";
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new {Error = errorDetails});
            return new MultiResponse()
            {
                HttpResponse = response
            };

        }

    }
    #endregion

    #region Delete
    [Function("DeleteSeed")]
    public async Task<HttpResponseData> DeleteSeed([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "seed/delete")] 
        HttpRequestData req,
        FunctionContext executionContext)
    {
        try
        {

            var logger = executionContext.GetLogger("DeleteSeed");
            logger.LogInformation("C# HTTP trigger function DeleteSeed processed a request.");



            //input from query parameter
            string? input = req.Query["seeded"];
            //input from query parameter
            string? _count = req.Query["count"];
            int count;
            bool parseSuccess = int.TryParse(_count, out count);
            if (parseSuccess != true)
            {
                count = 0;
            }
            bool seeded = input != null && bool.TryParse(input, out bool result) ? result : true; //default seeded true if input null
            //deleting user documents
            var delResponse = await _service.DeleteSeed(count, seeded);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(delResponse);


            return response;

        }
        catch (CosmosException ex)
        {
            var errorDetails = $"Status: {ex.StatusCode}\n" +
                            $"Message: {ex.Message}\n" +
                            $"ActivityId: {ex.ActivityId}\n" +
                            $"ResponseBody: {ex.ResponseBody}";
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteAsJsonAsync(new {Error = errorDetails});
            return response;
        }

    }
    #endregion

}



