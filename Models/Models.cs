
using System;
using System.Collections.Generic;
// using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
// using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Models;

public class dbComment 
{
    public virtual string Id { get; set; }
    public virtual string Message { get; set; }

    public virtual DateTime CreatedAt { get; set; }

    public virtual bool Seeded { get; set; } = false;

    public dbComment()
    {
        
    }
    public static dbComment Seed()
    {
        return new dbComment()
        {
            Seeded = true,
            
            Id = Guid.NewGuid().ToString(),
            Message = "Unique message",
            CreatedAt = default,
        };
    }
}

public class dbUser 
{
    public string id { get; set;}

    public virtual string FirstName { get; set;}

    public virtual string LastName { get; set;}

    public virtual string Email { get; set;}
    public virtual required string Country { get; set;}

    public virtual DateTime CreatedAt { get; set; }

    public virtual string Address { get; set;}
    public virtual List<dbComment> Comments { get; set; }
    
    public virtual bool Seeded { get; set;} = false;

    public dbUser()
    {
        
    }


    public static dbUser Seed()
    {
        List<dbComment> comments = new List<dbComment>();
        for(int i = 0; i < 10; i++)
        {
            comments.Add(dbComment.Seed());
        } 

        return new dbUser
        {
            Seeded = true,
            
            id = Guid.NewGuid().ToString(),
            FirstName = "Pelle",
            LastName = "Svanslös",
            Email = "PelleSvanslös@gmail.com",
            Country = "Sweden",
            Address = "Lemonadgatan 89B",
            Comments = comments,
            CreatedAt = default,
        };

    }

}

//output binding https://learn.microsoft.com/en-us/azure/azure-functions/functions-add-output-binding-cosmos-db-vs-code?pivots=programming-language-csharp#add-an-output-binding
public class MultiResponse
{
    //binding property to database container
    [CosmosDBOutput("my-examensarbete", "my-container2",
        Connection = "CosmosDbConnectionString", 
        CreateIfNotExists = true, PartitionKey = "/Country")]
    public IEnumerable<dbUser>? Documents { get; set; }


    [CosmosDBOutput("my-examensarbete", "my-container2",
        Connection = "CosmosDbConnectionString", 
        CreateIfNotExists = true, PartitionKey = "/Country")]
    public dbUser? Document { get; set; }

    public HttpResponseData? HttpResponse { get; set; }
}

public class ResponseData 
{
    public string? Message { get; set; }

    // [JsonConverter(typeof(ContinuationTokenConverter))]
    public string? ContinuationToken { get; set; }
    public object? Data { get; set; }
}

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