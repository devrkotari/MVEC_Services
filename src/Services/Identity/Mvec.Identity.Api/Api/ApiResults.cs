using Microsoft.AspNetCore.Mvc;
using Mvec.BuildingBlocks.Common;

namespace Mvec.Identity.Api.Api;

/// <summary>Maps the domain <see cref="Result"/> type onto HTTP responses (ProblemDetails on failure).</summary>
public static class ApiResults
{
    public static IActionResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : result.Error.ToProblem();

    public static IActionResult ToOk(this Result result) =>
        result.IsSuccess ? new OkResult() : result.Error.ToProblem();

    public static IActionResult ToCreated<T>(this Result<T> result, string location) =>
        result.IsSuccess
            ? new CreatedResult(location, result.Value)
            : result.Error.ToProblem();

    private static ObjectResult ToProblem(this Error error)
    {
        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            "validation" => StatusCodes.Status400BadRequest,
            "account_suspended" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Message
        };
        return new ObjectResult(problem) { StatusCode = status };
    }
}
