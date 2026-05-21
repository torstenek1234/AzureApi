using System.Collections.Generic;
using Models;

namespace Services;

public interface IUserService
{
    public MultiResponse CreateSeed(int count);
    public Task<ResponseData> DeleteSeed(int count, bool seeded = true);

    public Task<dbUser?> ReadUserAsync(string id);
    public Task<(List<dbUser>, string?)> ReadUsersAsync(string? continuationToken, int pageSize);

    public MultiResponse CreateUserAsync(dbUser item);
    public MultiResponse UpdateUserAsync(dbUser item);
    public Task<ResponseData> DeleteUserAsync(string id);


    
}