using Services;
using Models;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
// using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
// using Microsoft.OpenApi.Models;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
// using System.IO;
// using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.Azure.Cosmos;
// using Newtonsoft.Json;
using System.Net.Http;
using System.Text.Json;

namespace Examensarbete.Function
{
    public class UserFunction
    {
        private readonly ILogger<UserFunction> _logger;
        private readonly IUserService _service;

        public UserFunction(ILogger<UserFunction> logger, IUserService service)
        {
            _logger = logger;
            _service = service;
        }

        [Function("ReadUser")]
        public async Task<HttpResponseData> ReadUser([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/read/{id}")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string id)
        {
            try{

                var logger = executionContext.GetLogger("ReadUser");
                logger.LogInformation("C# HTTP trigger function ReadUser processed a request.");

                var user = await _service.ReadUserAsync(id);

                var response = req.CreateResponse(HttpStatusCode.OK);
                var respData = new ResponseData()
                {
                    Data = user,
                    Message = "Success" 
                };
                await response.WriteAsJsonAsync(respData);
                return response;

            }catch(Exception ex)
            {
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteAsJsonAsync(new {Error = ex.Message});
                return response;
            }
        }

        [Function("ReadUsers")]
        public async Task<HttpResponseData> ReadUsers([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/read")] 
        HttpRequestData req,
        FunctionContext executionContext)
        {

            try {
                var logger = executionContext.GetLogger("ReadUsers");
                logger.LogInformation("C# HTTP trigger function ReadUsers processed a request.");

                //input from query parameter
                // string continuationToken = req.Query["continuationToken"];
                foreach (var header in req.Headers)
                {
                    Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
                }

                // string? continuationToken = null;
                // if (req.Headers.TryGetValues("x-ms-continuation", out var headerValues))
                // {
                //     continuationToken = headerValues.FirstOrDefault();
                // }
                string? _pageSize = req.Query["pageSize"];
                string? continuationToken = req.Query["token"];
   
                int pageSize = int.TryParse(_pageSize, out int result) ? result : 100;
                
                var (users, continuation) = await _service.ReadUsersAsync(continuationToken, pageSize);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("x-ms-continuation", continuation);

                var respData = new ResponseData()
                {
                    ContinuationToken = continuation,
                    Message =  $"Fetched {users.Count} users",
                    Data = users,
                };
                await response.WriteAsJsonAsync(respData);
                return response;

            }
            catch (CosmosException ex) {
                var errorDetails = $"Status: {ex.StatusCode}\n" +
                                $"Message: {ex.Message}\n" +
                                $"ActivityId: {ex.ActivityId}\n" +
                                $"ResponseBody: {ex.ResponseBody}";

                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new {Error = errorDetails});
                return response;
            }
        }

        [Function("CreateUser")]
        public async Task<MultiResponse> CreateUser([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users/create")] 
        HttpRequestData req,
        FunctionContext executionContext)
        {

            try {
                var logger = executionContext.GetLogger("CreateUser");
                logger.LogInformation("C# HTTP trigger function CreateUser processed a request.");
                
                //extracting user model from request body
                var user = await req.ReadFromJsonAsync<dbUser>();

                //validating request body, if required property Country is missing exception throws
                if(user == null)    
                {
                    var exitResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await exitResponse.WriteAsJsonAsync(new {Error = "Missing body"});
                    return new MultiResponse()
                    {
                        HttpResponse = exitResponse
                    };
                }

                MultiResponse userResponse =  _service.CreateUserAsync(user);

                var response = req.CreateResponse(HttpStatusCode.OK);
                
                var respData = new ResponseData()
                {
                    Message =  $"Success",
                    Data = userResponse.Document,
                };

                await response.WriteAsJsonAsync(respData);

                return new MultiResponse()
                {
                    Document = userResponse.Document,
                    HttpResponse = response
                };

            }
            catch(AggregateException ex) 
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                await response.WriteAsJsonAsync(new {Error = ex.Message});
                return new MultiResponse()
                {
                    HttpResponse = response
                };
            }
            catch (CosmosException ex) {
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


        [Function("UpdateUser")]
        public async Task<MultiResponse> UpdateUser([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/update")] 
        HttpRequestData req,
        FunctionContext executionContext)
        {

            try {
                var logger = executionContext.GetLogger("UpdateUser");
                logger.LogInformation("C# HTTP trigger function UpdateUser processed a request.");

                //extracting user model from request body
                var user = await req.ReadFromJsonAsync<dbUser>();

                //validating request body, if required property Country is missing exception throws
                if(user == null)    
                {
                    var exitResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await exitResponse.WriteAsJsonAsync(new {Error = "Missing body"});
                    return new MultiResponse()
                    {
                        HttpResponse = exitResponse
                    };
                }

                MultiResponse userResponse =  _service.UpdateUserAsync(user);

                var response = req.CreateResponse(HttpStatusCode.OK);
                
                var respData = new ResponseData()
                {
                    Message =  $"Success",
                    Data = userResponse.Document,
                };

                await response.WriteAsJsonAsync(respData);

                return new MultiResponse()
                {
                    Document = userResponse.Document,
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

        [Function("DeleteUser")]
        public async Task<HttpResponseData> DeleteUser([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/delete/{id}")] 
        HttpRequestData req,
        FunctionContext executionContext,
        string id)
        {

            try {
                var logger = executionContext.GetLogger("DeleteUser");
                logger.LogInformation("C# HTTP trigger function DeleteUser processed a request.");

                //if user not found throws exception
                ResponseData data = await _service.DeleteUserAsync(id);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(data);

                return response;

            }
            catch (CosmosException ex) {
                var errorDetails = $"Status: {ex.StatusCode}\n" +
                                $"Message: {ex.Message}\n" +
                                $"ActivityId: {ex.ActivityId}\n" +
                                $"ResponseBody: {ex.ResponseBody}";

                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteAsJsonAsync(new {Error = errorDetails});
                return response;
            }
            catch (Exception ex) //when (ex.Message.ToLower().Contains("not found"))
            {
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteAsJsonAsync(new {Error = ex.Message});
                return response;
            }
        }

        
    }
}
