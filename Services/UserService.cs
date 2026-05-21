using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;

namespace Services;

public class UserService: IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly Container _container;

    public UserService(ILogger<UserService> logger, Container container)
    {
        _container = container;
        _logger = logger;
    }



    #region Seed
    public MultiResponse CreateSeed(int count)
    {
        _logger.LogInformation($"Starting CreateSeed");
        _logger.LogInformation($"Arguments\ncount: {count}");
        
        List<dbUser> Users = new List<dbUser>();

        for (int i = 0; i < count; i++)
        {
            Users.Add(dbUser.Seed());
        }

        var response = new MultiResponse()
        {
            HttpResponse = null,
            Documents = Users
        };
        return response;
    }

    public async Task<ResponseData> DeleteSeed(int count = 100, bool seeded = true)
    {
        _logger.LogInformation($"Starting DeleteSeed");
        _logger.LogInformation($"Arguments\nseeded: {seeded}");

        int deletedItems = 0;
        var deleteTasks = new List<Task>();


        // fetching documents with linq https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-dotnet-query-items#query-items-using-linq-asynchronously
        IOrderedQueryable<dbUser> queryable = _container.GetItemLinqQueryable<dbUser>();
        var matches = queryable
            .Where(p => p.Seeded == seeded);

        using FeedIterator<dbUser> linqFeed = matches.ToFeedIterator();
        while (linqFeed.HasMoreResults && deletedItems < count)
        {
            FeedResponse<dbUser> response = await linqFeed.ReadNextAsync();

            // Iterate query results
            foreach (dbUser document in response)
            {
                deleteTasks.Add(_container.DeleteItemAsync<dbUser>(document.id, new PartitionKey(document.Country)));
                deletedItems++;

                //Limits request rate to prevent 429 error
                if (deleteTasks.Count >= 50)
                {
                    await Task.WhenAll(deleteTasks);
                    deleteTasks.Clear();
                }
                
                if (deletedItems == count) break;
            }
        }

        if(deleteTasks.Count > 0 )
        {
            await Task.WhenAll(deleteTasks);
        }

        return new ResponseData()
        {
            Message = $"Deleted documents: {deletedItems}"
        };
    }
    #endregion

    #region User
    public async Task<dbUser?> ReadUserAsync(string id)
    {
        _logger.LogInformation($"Starting ReadUserAsync");
        _logger.LogInformation($"Arguments\nid: {id}");

        string country = "";

        //definning linq query
        IOrderedQueryable<dbUser> queryable = _container.GetItemLinqQueryable<dbUser>();
        var matches = queryable
            .Where(p => p.id == id);

        using FeedIterator<dbUser> linqFeed = matches.ToFeedIterator();
        while (linqFeed.HasMoreResults)
        {
            FeedResponse<dbUser> response = await linqFeed.ReadNextAsync();

            //finding partition key
            foreach (var document in response)
            {
                country = document.Country;
                _logger.LogInformation($"{document.id}"); 
                
            }
        }

        //if no partition key found user does not exist return null
        if(country == "")
        {
            _logger.LogInformation("In userservice, user not found");
            throw new Exception("User not found");
        }   
        
        //partition key found returning user with point read
        PartitionKey partitionKey = new (country);
        return await _container.ReadItemAsync<dbUser>(id, partitionKey);


    }
    public async Task<(List<dbUser>, string?)> ReadUsersAsync(string? continuationToken = null, int pageSize = 100)
    {
        _logger.LogInformation($"Starting ReadUsersAsync");
        // _logger.LogInformation($"Arguments:\ncontinuationToken: {continuationToken}\npageSize:{pageSize}");
        _logger.LogInformation($"Cosmos container: {_container.Id}");
        if (continuationToken == "null") continuationToken = null;
        _logger.LogInformation($"Arguments:\ncontinuationToken: {continuationToken}\npageSize:{pageSize}");

        //sql query 
        QueryDefinition query = new QueryDefinition("SELECT * FROM c");
        List<dbUser> users = new List<dbUser>();
    

        using FeedIterator<dbUser> resultSetIterator = _container.GetItemQueryIterator<dbUser>(
            query,
            continuationToken: continuationToken,
            requestOptions: new QueryRequestOptions() {MaxItemCount = pageSize }
            );
        
        if (resultSetIterator.HasMoreResults)
        {
            FeedResponse<dbUser> response = await resultSetIterator.ReadNextAsync();

            continuationToken = response.ContinuationToken;
            _logger.LogCritical($"continuationToken: {continuationToken}");

            users.AddRange(response);

            /*if conitunation token is null and database has more items left, fetch remaining items
            * needed because continuation token wont be returned on query where pagesize is bigger than
            * remaining items in container
            */
            
            // if(continuationToken == null && response.Count < pageSize)
            // {
            //     using FeedIterator<dbUser> lastResultSetIterator = _container.GetItemQueryIterator<dbUser>(
            //         query,
            //         continuationToken: null,
            //         requestOptions: new QueryRequestOptions(){MaxItemCount = pageSize });

            //     FeedResponse<dbUser> lastResponse = await lastResultSetIterator.ReadNextAsync();
            //     users.AddRange(lastResponse);
            // }
            
        }


        return (users, continuationToken);


    }

    public MultiResponse UpdateUserAsync(dbUser item)
    {
        _logger.LogInformation("Starting UpdateUserAsync");
        _logger.LogInformation($"Arguments:\nitem: {item}");
        return new MultiResponse()
        {
            HttpResponse = null,
            Document = item
        };
    }
    public MultiResponse CreateUserAsync(dbUser item)
    {
        _logger.LogInformation("Starting CreateUserAsync");
        _logger.LogInformation($"Value for item: {item}");


        dbUser User = new dbUser()
        {
            Seeded = false,
            id = Guid.NewGuid().ToString(),
            Comments = new List<dbComment>(),
            Country = item.Country,
            FirstName = item.FirstName,
            LastName = item.LastName,
            Email = item.Email,
            Address = item.Address,
            CreatedAt = DateTime.UtcNow,
        };
        _logger.LogInformation($"Userid:{User.id}");
        var response = new MultiResponse()
        {
            HttpResponse = null,
            Document = User
        };
         
        return response;
    }

    public async Task<ResponseData> DeleteUserAsync(string id)
    {
        _logger.LogInformation("Starting DeleteUserAsync");
        _logger.LogInformation($"Arguments\nid: {id}");

        //variable declaration
        dbUser? user;

        // fetching documents with linq https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-dotnet-query-items#query-items-using-linq-asynchronously
        IOrderedQueryable<dbUser> queryable = _container.GetItemLinqQueryable<dbUser>();
        var matches = queryable
            .Where(p => p.id == id);

        using FeedIterator<dbUser> linqFeed = matches.ToFeedIterator();

        if (!linqFeed.HasMoreResults) throw new Exception("User not found");

        FeedResponse<dbUser> response = await linqFeed.ReadNextAsync();
        _logger.LogInformation($"{response.Count}");

        user = response.FirstOrDefault() ?? throw new Exception("User not found");

        await _container.DeleteItemAsync<dbUser>(user.id, new PartitionKey(user.Country));
        

        return new ResponseData(){
            Message = $"Deleted user with id: {id}",
            Data = user
        };
    }

    #endregion
}