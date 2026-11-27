using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Modules.Documents.Application;
using OnboardFlow.Api.Modules.Documents.Application.DTOs;
using OnboardFlow.Api.Modules.Documents.Application.Services;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Authorize]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("api/cases/{caseId:guid}/checklist-items/{itemId:guid}/documents")]
    [Authorize(Roles = AppRoles.Client)]
    [ServiceFilter(typeof(HardRequestLimitFilter))]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    [ProducesResponseType(typeof(UploadDocumentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload(
        Guid caseId,
        Guid itemId,
        [FromForm(Name = "file")] IFormFile? file,
        CancellationToken ct)
    {
        await using var stream = file?.OpenReadStream();
        var command = new UploadDocumentCommand(
            caseId,
            itemId,
            file?.FileName,
            file?.ContentType,
            file?.Length ?? 0,
            stream);

        var result = await _documentService.UploadAsync(command, ct);
        if (!result.IsSuccess)
        {
            return ToProblemResult(result.StatusCode, result.ErrorCode, result.ErrorTitle, result.ErrorDetail);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet("api/cases/{caseId:guid}/documents")]
    [Authorize(Roles = $"{AppRoles.Client},{AppRoles.OnboardingManager},{AppRoles.Administrator}")]
    [ProducesResponseType(typeof(IReadOnlyList<CaseDocumentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListForCase(Guid caseId, CancellationToken ct)
    {
        var result = await _documentService.ListForCaseAsync(caseId, ct);
        if (!result.IsSuccess)
        {
            return ToProblemResult(result.StatusCode, result.ErrorCode, result.ErrorTitle, result.ErrorDetail);
        }

        return Ok(result.Value);
    }

    private ObjectResult ToProblemResult(int statusCode, string? code, string? title, string? detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        if (!string.IsNullOrEmpty(code))
        {
            problem.Extensions["code"] = code;
        }

        return StatusCode(statusCode, problem);
    }
}

/// <summary>
/// Enforces Documents:HardRequestLimitBytes on the upload endpoint only, returning 413 ProblemDetails with code 'request_too_large'.
/// </summary>
public sealed class HardRequestLimitFilter : IAsyncResourceFilter
{
    private readonly long _maxBytes;

    public HardRequestLimitFilter(IOptions<DocumentsOptions> options)
    {
        _maxBytes = options.Value.HardRequestLimitBytes;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var bodySizeFeature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is { IsReadOnly: false })
        {
            bodySizeFeature.MaxRequestBodySize = _maxBytes;
        }

        var contentLength = context.HttpContext.Request.ContentLength;
        if (contentLength.HasValue && contentLength.Value > _maxBytes)
        {
            context.Result = TooLargeResult(_maxBytes);
            return;
        }

        try
        {
            await next();
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            context.Result = TooLargeResult(_maxBytes);
        }
        catch (InvalidDataException ex) when (ex.Message.Contains("length limit", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = TooLargeResult(_maxBytes);
        }
    }

    private static ObjectResult TooLargeResult(long maxBytes)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status413PayloadTooLarge,
            Title = "Request Too Large",
            Detail = $"Request exceeds the maximum allowed size of {maxBytes} bytes."
        };
        problem.Extensions["code"] = "request_too_large";
        return new ObjectResult(problem) { StatusCode = StatusCodes.Status413PayloadTooLarge };
    }
}
