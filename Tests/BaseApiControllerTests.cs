using identity_service.Constants;
using identity_service.Controllers;
using identity_service.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace identity_service.Tests;

// Concrete subclass to test abstract BaseApiController
public class TestableController : BaseApiController
{
    public IActionResult TestApiOk<T>(T data, PaginationMeta? meta = null) => ApiOk(data, meta);
    public IActionResult TestApiCreated<T>(string actionName, object routeValues, T data) => ApiCreated(actionName, routeValues, data);
    public IActionResult TestApiNotFound(string detail) => ApiNotFound(detail);
    public IActionResult TestApiValidationError(string detail, List<FieldError> errors) => ApiValidationError(detail, errors);
    public string TestGetRequestId() => GetRequestId();
}

public class BaseApiControllerTests
{
    private readonly TestableController _controller;

    public BaseApiControllerTests()
    {
        _controller = new TestableController();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void ApiOk_ReturnsOkWithWrappedResponse()
    {
        var data = "test-data";

        var result = _controller.TestApiOk(data);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<string>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("test-data", response.Data);
        Assert.NotNull(response.Timestamp);
        Assert.NotEmpty(response.RequestId);
    }

    [Fact]
    public void ApiOk_WithMeta_IncludesMeta()
    {
        var meta = new PaginationMeta { Page = 1, Limit = 20, Total = 50, HasMore = true };

        var result = _controller.TestApiOk("data", meta);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<string>>(okResult.Value);
        Assert.NotNull(response.Meta);
        Assert.Equal(50, response.Meta!.Total);
    }

    [Fact]
    public void ApiCreated_ReturnsCreatedAtAction()
    {
        var data = "created-item";

        var result = _controller.TestApiCreated("GetById", new { id = 1 }, data);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var response = Assert.IsType<ApiSuccessResponse<string>>(createdResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public void ApiNotFound_ReturnsNotFoundResponseWithCorrectErrorType()
    {
        var result = _controller.TestApiNotFound("Resource not found.");

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var response = Assert.IsType<ApiErrorResponse>(notFoundResult.Value);
        Assert.False(response.Success);
        Assert.Equal(ErrorTypes.NotFound, response.Error.Type);
        Assert.Equal("Not Found", response.Error.Title);
        Assert.Equal(404, response.Error.Status);
        Assert.Equal("Resource not found.", response.Error.Detail);
    }

    [Fact]
    public void ApiValidationError_ReturnsUnprocessableEntityWithErrors()
    {
        var errors = new List<FieldError>
        {
            new() { Field = "Name", Message = "Name is required", Code = "REQUIRED" },
            new() { Field = "Code", Message = "Code too long", Code = "MAX_LENGTH" }
        };

        var result = _controller.TestApiValidationError("Validation failed", errors);

        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var response = Assert.IsType<ApiErrorResponse>(unprocessableResult.Value);
        Assert.False(response.Success);
        Assert.Equal(ErrorTypes.Validation, response.Error.Type);
        Assert.Equal("Validation Error", response.Error.Title);
        Assert.Equal(422, response.Error.Status);
        Assert.Equal("Validation failed", response.Error.Detail);
        Assert.Equal(2, response.Error.Errors!.Count);
        Assert.Equal("Name", response.Error.Errors[0].Field);
        Assert.Equal("REQUIRED", response.Error.Errors[0].Code);
    }

    [Fact]
    public void GetRequestId_ReturnsTraceIdentifier()
    {
        _controller.HttpContext.TraceIdentifier = "test-req-123";

        var requestId = _controller.TestGetRequestId();

        Assert.Equal("test-req-123", requestId);
    }
}
