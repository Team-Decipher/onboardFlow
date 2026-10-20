namespace OnboardFlow.Core.Enums;

public enum UserRole
{
    Administrator = 1,
    OnboardingManager = 2,
    Reviewer = 3,
    Client = 4
}

public enum ClientType
{
    Enterprise = 1,
    SME = 2,
    Partner = 3
}

public enum CaseStatus
{
    Draft = 1,
    InProgress = 2,
    UnderReview = 3,
    Approved = 4,
    Rejected = 5,
    Completed = 6
}

public enum DocumentStatus
{
    Pending = 1,
    Uploaded = 2,
    Approved = 3,
    Rejected = 4
}

public enum ReviewStage
{
    Legal = 1,
    Finance = 2,
    Executive = 3,
    Final = 4
}
