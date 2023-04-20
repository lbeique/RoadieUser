using Amazon.Lambda.Core;
using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Microsoft.EntityFrameworkCore;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RoadieUser;
public class Function
{

  DatabaseContext dbContext;
  public Function()
  {
    DotNetEnv.Env.Load();
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");
    var contextOptions = new DbContextOptionsBuilder<DatabaseContext>()
    .UseNpgsql(connectionString)
    .Options;

    dbContext = new DatabaseContext(contextOptions);
  }

  async public Task<APIGatewayHttpApiV2ProxyResponse> FunctionHandler(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
  {
    var method = request.RequestContext.Http.Method;
    var pathParameters = request.PathParameters;

    switch (method)
    {
      case "GET":
        if (pathParameters != null && pathParameters.ContainsKey("id"))
        {
          return await GetUserById(pathParameters["id"]);
        }
        break;
      case "POST":
        return await CreateUser(request);
      case "PUT":
        return await UpdateUser(request);
      case "DELETE":
        return await DeleteUser(request);
      default:
        return new APIGatewayHttpApiV2ProxyResponse
        {
          StatusCode = (int)HttpStatusCode.MethodNotAllowed,
          Body = "Method not allowed"
        };
    }

    return new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.BadRequest,
      Body = "Invalid request"
    };
  }

  private async Task<APIGatewayHttpApiV2ProxyResponse> GetUserById(string id)
  {
    var user = await dbContext.Users.FindAsync(id);

    if (user == null)
    {
      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.NotFound,
        Body = "User not found",
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.OK,
      Body = System.Text.Json.JsonSerializer.Serialize(user),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }

  private async Task<APIGatewayHttpApiV2ProxyResponse> CreateUser(APIGatewayHttpApiV2ProxyRequest request)
  {
    var user = System.Text.Json.JsonSerializer.Deserialize<User>(request.Body);

    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync();

    var response = new APIGatewayHttpApiV2ProxyResponse
    {
      StatusCode = (int)HttpStatusCode.Created,
      Body = System.Text.Json.JsonSerializer.Serialize(user),
      Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
    };
    return response;
  }

  private async Task<APIGatewayHttpApiV2ProxyResponse> UpdateUser(APIGatewayHttpApiV2ProxyRequest request)
  {
    var pathParameters = request.PathParameters;

    if (pathParameters != null && pathParameters.ContainsKey("id"))
    {
      var id = pathParameters["id"];
      var userToUpdate = await dbContext.Users.FindAsync(id);

      if (userToUpdate == null)
      {
        return new APIGatewayHttpApiV2ProxyResponse
        {
          StatusCode = (int)HttpStatusCode.NotFound,
          Body = "User not found",
          Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
      }

      var user = System.Text.Json.JsonSerializer.Deserialize<User>(request.Body);
      user.Sub = id; // Set the user id from path parameters

      dbContext.Users.Update(user);
      await dbContext.SaveChangesAsync();

      var response = new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.OK,
        Body = System.Text.Json.JsonSerializer.Serialize(user),
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
      return response;
    }
    else
    {
      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Body = "Invalid request",
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }
  }


  private async Task<APIGatewayHttpApiV2ProxyResponse> DeleteUser(APIGatewayHttpApiV2ProxyRequest request)
  {
    var pathParameters = request.PathParameters;
    if (pathParameters != null && pathParameters.ContainsKey("id"))
    {
      var id = pathParameters["id"];
      var user = await dbContext.Users.FindAsync(id);

      if (user == null)
      {
        return new APIGatewayHttpApiV2ProxyResponse
        {
          StatusCode = (int)HttpStatusCode.NotFound,
          Body = "User not found",
          Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
      }

      dbContext.Users.Remove(user);
      await dbContext.SaveChangesAsync();

      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.NoContent,
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }
    else
    {
      return new APIGatewayHttpApiV2ProxyResponse
      {
        StatusCode = (int)HttpStatusCode.BadRequest,
        Body = "Invalid request",
        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
      };
    }
  }
}