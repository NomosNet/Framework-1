using System.Text.Json;
using UserEntity = UserService.Domain.User;
using UserBusinessService = UserService.Service.User.UserService;
using UserService.Middleware;

namespace UserService.Handler.User;

public class UserHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly UserBusinessService _userService;

    public UserHandler(UserBusinessService userService)
    {
        _userService = userService;
    }

    public async Task CreateUser(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        UserEntity? user;
        try
        {
            user = await JsonSerializer.DeserializeAsync<UserEntity>(context.Request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "INVALID_JSON", "Invalid JSON format");
            return;
        }

        if (user is null)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "INVALID_JSON", "Invalid JSON format");
            return;
        }

        try
        {
            _userService.CreateUser(user);
        }
        catch (ArgumentException ex)
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "VALIDATION_ERROR", ex.Message);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status201Created;
        await context.Response.WriteAsJsonAsync(user);
    }

    public async Task GetUserById(HttpContext context, string id)
    {
        context.Response.ContentType = "application/json";

        if (string.IsNullOrEmpty(id))
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "MISSING_ID", "User ID is required");
            return;
        }

        if (!int.TryParse(id, out var idInt))
        {
            await WriteError(context, StatusCodes.Status400BadRequest, "INVALID_ID", "Invalid user ID format");
            return;
        }

        try
        {
            var user = _userService.GetUserById(idInt);
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(user);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteError(context, StatusCodes.Status404NotFound, "USER_NOT_FOUND", ex.Message);
        }
    }

    public async Task GetUsers(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        var users = _userService.GetUsers();
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(users);
    }

    private static async Task WriteError(HttpContext context, int status, string code, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = status;

        await context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = message,
            Code = code,
            RequestId = RequestContext.GetRequestId(context)
        });
    }
}
