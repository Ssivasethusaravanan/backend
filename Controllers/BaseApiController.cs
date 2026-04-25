using identity_service.Constants;
using identity_service.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace identity_service.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected string GetRequestId() => HttpContext.TraceIdentifier;

    protected IActionResult ApiOk<T>(T data, PaginationMeta? meta = null)
    {
        return Ok(new ApiSuccessResponse<T>
        {
            Data = data,
            Meta = meta,
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = GetRequestId()
        });
    }

    protected IActionResult ApiCreated<T>(string actionName, object routeValues, T data)
    {
        return CreatedAtAction(actionName, routeValues, new ApiSuccessResponse<T>
        {
            Data = data,
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = GetRequestId()
        });
    }

    protected IActionResult ApiNotFound(string detail)
    {
        return NotFound(new ApiErrorResponse
        {
            Error = new ApiError
            {
                Type = ErrorTypes.NotFound,
                Title = "Not Found",
                Status = 404,
                Detail = detail,
                Instance = HttpContext.Request.Path
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = GetRequestId()
        });
    }

    protected IActionResult ApiValidationError(string detail, List<FieldError> errors)
    {
        return UnprocessableEntity(new ApiErrorResponse
        {
            Error = new ApiError
            {
                Type = ErrorTypes.Validation,
                Title = "Validation Error",
                Status = 422,
                Detail = detail,
                Instance = HttpContext.Request.Path,
                Errors = errors
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = GetRequestId()
        });
    }

    protected IActionResult ApiUnauthorized(string detail)
    {
        return Unauthorized(new ApiErrorResponse
        {
            Error = new ApiError
            {
                Type     = ErrorTypes.Unauthorized,
                Title    = "Unauthorized",
                Status   = 401,
                Detail   = detail,
                Instance = HttpContext.Request.Path,
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = GetRequestId()
        });
    }

    protected IActionResult ApiForbidden(string detail)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse
        {
            Error = new ApiError
            {
                Type     = ErrorTypes.Forbidden,
                Title    = "Forbidden",
                Status   = 403,
                Detail   = detail,
                Instance = HttpContext.Request.Path,
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = GetRequestId()
        });
    }

    protected IActionResult ApiConflict(string detail)
    {
        return Conflict(new ApiErrorResponse
        {
            Error = new ApiError
            {
                Type     = ErrorTypes.Conflict,
                Title    = "Conflict",
                Status   = 409,
                Detail   = detail,
                Instance = HttpContext.Request.Path,
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = GetRequestId()
        });
    }

    protected IActionResult ApiBadRequest(string detail)
    {
        return BadRequest(new ApiErrorResponse
        {
            Error = new ApiError
            {
                Type     = ErrorTypes.BadRequest,
                Title    = "Bad Request",
                Status   = 400,
                Detail   = detail,
                Instance = HttpContext.Request.Path,
            },
            Timestamp = DateTime.UtcNow.ToString("o"),
            RequestId = GetRequestId()
        });
    }
}

