using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;
using Services;
using System;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;


// dependency injection and builder
var host = new HostBuilder()
    // .ConfigureFunctionsWorkerDefaults((IFunctionsWorkerApplicationBuilder builder ) => 
    // {
    //     builder.Services.Configure<JsonSerializerOptions>(jsonSerializerOptions =>
    //     {
    //         jsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    //         jsonSerializerOptions.Converters.Add(new ContinuationTokenConverter());
    //     });
    // })
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        
        services.AddSingleton<IUserService, UserService>();

        //cosmosdb database instance
        string CosmosDbConnectionString = Environment.GetEnvironmentVariable("CosmosDbConnectionString") 
            ?? throw new Exception("CosmosDbConnectionString not in settings");
        var cosmosClient = new CosmosClient(CosmosDbConnectionString);
        services.AddSingleton(cosmosClient);

        //cosmosdb nosql container instance
        services.AddSingleton<Container>(c => cosmosClient.GetContainer("my-examensarbete", "my-container2"));

    })
    .Build();
await host.RunAsync();


// public class ContinuationTokenConverter : JsonConverter<string>
// {
//     public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//         =>  reader.GetString();

//     public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
//     {
//         if (value.TrimStart().StartsWith("["))
//         {
//             writer.WriteRawValue(value);
//         }
//         else
//         {
//             writer.WriteStringValue(value);
//         }    
//     }

//     // => (value.TrimStart().StartsWith("[")) ? writer.WriteRawValue(value) : writer.WriteStringValue(value);
// }

