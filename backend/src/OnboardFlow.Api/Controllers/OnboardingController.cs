using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnboardFlow.Api.Common.Authorization;
using OnboardFlow.Api.Models;

namespace OnboardFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 🔒 Protect all endpoints in OnboardingController
public class OnboardingController : ControllerBase
{
    private static readonly object LockObject = new();
    private static List<WorkflowStep> _steps = InitializeDefaultSteps();

    // Default case belongs to Ceylon Exports
    public static readonly Guid CaseClientOrganisationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string AssignedReviewerEmail = "reviewer@onboardflow.com";

    private static List<WorkflowStep> InitializeDefaultSteps()
    {
        return new List<WorkflowStep>
        {
            new WorkflowStep
            {
                Id = "step-1",
                Title = "Create your case",
                Status = "Approved",
                Order = 1,
                Description = "Initial client KYC profile and case registration established.",
                CompletedAt = DateTime.UtcNow.AddDays(-2),
                Documents = new List<string> { "Client_Profile_Form.pdf", "KYC_Verification.pdf" }
            },
            new WorkflowStep
            {
                Id = "step-2",
                Title = "Collect documents",
                Status = "Approved",
                Order = 2,
                Description = "Required compliance paperwork, certificate of incorporation, and tax IDs uploaded.",
                CompletedAt = DateTime.UtcNow.AddDays(-1),
                Documents = new List<string> { "Certificate_of_Incorporation.pdf", "Tax_Identification_W8BEN.pdf", "Signatory_Resolution.pdf" }
            },
            new WorkflowStep
            {
                Id = "step-3",
                Title = "Review & approve",
                Status = "In review",
                Order = 3,
                Description = "Legal counsel and compliance officers review submitted documents for final clearance.",
                CompletedAt = null,
                Documents = new List<string> { "Compliance_Audit_Checklist.pdf" }
            }
        };
    }

    [HttpGet("workflow")]
    public ActionResult<OnboardingWorkflowResponse> GetWorkflow()
    {
        // 🔒 Resource-level enforcement: Client can only view cases belonging to their own organization
        if (User.IsInRole(AppRoles.Client))
        {
            var userOrgId = User.FindFirstValue("clientOrganisationId");
            if (string.IsNullOrWhiteSpace(userOrgId) || 
                !Guid.TryParse(userOrgId, out var orgGuid) || 
                orgGuid != CaseClientOrganisationId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Forbidden: You are not authorized to view cases outside your own client organisation."
                });
            }
        }

        lock (LockObject)
        {
            var approvedCount = _steps.Count(s => s.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));
            var totalCount = _steps.Count;
            var progress = totalCount > 0 ? (int)Math.Round((double)approvedCount / totalCount * 100) : 0;

            var response = new OnboardingWorkflowResponse
            {
                CaseId = "OBF-2026-904",
                ClientName = "Apex Global Advisory Partners",
                Headline = "Great partnerships\nstart here.",
                Subtitle = "One place for your clients, documents and every step in between.",
                CardTitle = "A smoother start",
                Category = "CLIENT ONBOARDING & DOCUMENT WORKFLOW",
                ProgressPercentage = progress,
                Steps = _steps.OrderBy(s => s.Order).ToList()
            };

            return Ok(response);
        }
    }

    [HttpPost("steps/{id}/toggle")]
    [Authorize(Roles = $"{AppRoles.Reviewer},{AppRoles.OnboardingManager},{AppRoles.Administrator}")]
    public ActionResult<OnboardingWorkflowResponse> ToggleStep(string id)
    {
        // 🔒 Reviewer isolation: Reviewer can only act if assigned
        if (User.IsInRole(AppRoles.Reviewer))
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (!string.Equals(userEmail, AssignedReviewerEmail, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Forbidden: You can only act on reviews and documents assigned directly to you."
                });
            }
        }

        lock (LockObject)
        {
            var step = _steps.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (step == null)
            {
                return NotFound(new { message = $"Workflow step with id '{id}' was not found." });
            }

            if (step.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
            {
                step.Status = "In review";
                step.CompletedAt = null;
            }
            else
            {
                step.Status = "Approved";
                step.CompletedAt = DateTime.UtcNow;
            }

            var approvedCount = _steps.Count(s => s.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));
            var totalCount = _steps.Count;
            var progress = totalCount > 0 ? (int)Math.Round((double)approvedCount / totalCount * 100) : 0;

            return Ok(new OnboardingWorkflowResponse
            {
                CaseId = "OBF-2026-904",
                ClientName = "Apex Global Advisory Partners",
                Headline = "Great partnerships\nstart here.",
                Subtitle = "One place for your clients, documents and every step in between.",
                CardTitle = "A smoother start",
                Category = "CLIENT ONBOARDING & DOCUMENT WORKFLOW",
                ProgressPercentage = progress,
                Steps = _steps.OrderBy(s => s.Order).ToList()
            });
        }
    }

    [HttpPost("steps/{id}/status")]
    [Authorize(Roles = $"{AppRoles.Reviewer},{AppRoles.OnboardingManager},{AppRoles.Administrator}")]
    public ActionResult<OnboardingWorkflowResponse> UpdateStepStatus(string id, [FromBody] UpdateStepStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest(new { message = "Status is required." });
        }

        // 🔒 Reviewer isolation: Reviewer can only act if assigned
        if (User.IsInRole(AppRoles.Reviewer))
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
            if (!string.Equals(userEmail, AssignedReviewerEmail, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Forbidden: You can only act on reviews and documents assigned directly to you."
                });
            }
        }

        lock (LockObject)
        {
            var step = _steps.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (step == null)
            {
                return NotFound(new { message = $"Workflow step with id '{id}' was not found." });
            }

            step.Status = request.Status;
            step.CompletedAt = request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) 
                ? DateTime.UtcNow 
                : null;

            var approvedCount = _steps.Count(s => s.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));
            var totalCount = _steps.Count;
            var progress = totalCount > 0 ? (int)Math.Round((double)approvedCount / totalCount * 100) : 0;

            return Ok(new OnboardingWorkflowResponse
            {
                CaseId = "OBF-2026-904",
                ClientName = "Apex Global Advisory Partners",
                Headline = "Great partnerships\nstart here.",
                Subtitle = "One place for your clients, documents and every step in between.",
                CardTitle = "A smoother start",
                Category = "CLIENT ONBOARDING & DOCUMENT WORKFLOW",
                ProgressPercentage = progress,
                Steps = _steps.OrderBy(s => s.Order).ToList()
            });
        }
    }

    [HttpPost("reset")]
    [Authorize(Roles = AppRoles.Administrator)] // 🔒 Only Admin can reset the workflow
    public ActionResult<OnboardingWorkflowResponse> ResetWorkflow()
    {
        lock (LockObject)
        {
            _steps = InitializeDefaultSteps();

            var approvedCount = _steps.Count(s => s.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));
            var totalCount = _steps.Count;
            var progress = totalCount > 0 ? (int)Math.Round((double)approvedCount / totalCount * 100) : 0;

            return Ok(new OnboardingWorkflowResponse
            {
                CaseId = "OBF-2026-904",
                ClientName = "Apex Global Advisory Partners",
                Headline = "Great partnerships\nstart here.",
                Subtitle = "One place for your clients, documents and every step in between.",
                CardTitle = "A smoother start",
                Category = "CLIENT ONBOARDING & DOCUMENT WORKFLOW",
                ProgressPercentage = progress,
                Steps = _steps.OrderBy(s => s.Order).ToList()
            });
        }
    }
}
