using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Application.DTOs;
using OnboardFlow.Core.Interfaces;

namespace OnboardFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IBlobStorageService _storageService;

    public DocumentsController(IBlobStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] Guid checklistItemId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No valid file uploaded." });
        }

        // 25MB max size policy
        if (file.Length > 25 * 1024 * 1024)
        {
            return BadRequest(new { message = "File exceeds the maximum permitted size of 25MB." });
        }

        using var stream = file.OpenReadStream();
        var uri = await _storageService.UploadFileAsync(stream, file.FileName, file.ContentType);

        return Ok(new
        {
            documentId = Guid.NewGuid(),
            checklistItemId,
            fileName = file.FileName,
            blobUri = uri,
            uploadedAt = DateTime.UtcNow
        });
    }

    [HttpPost("{id}/review")]
    public IActionResult Review(Guid id, [FromBody] ReviewDocumentDto dto)
    {
        return Ok(new
        {
            documentId = id,
            status = dto.Approved ? "Approved" : "Rejected",
            reviewedAt = DateTime.UtcNow,
            nextStage = dto.NextStage?.ToString()
        });
    }
}
