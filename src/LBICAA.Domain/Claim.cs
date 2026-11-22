
namespace LBICAA.Domain.Claims;

public enum ClaimNature
{
    Documented,
    Inferred,
    Hypothesis
}

public enum ClaimStatus
{
    Active,
    Disputed,
    Retracted
}

public enum ConfidenceLevel
{
    Unknown,
    Low,
    Medium,
    High
}

public enum ClaimValueKind
{
    EntityReference,
    Literal
}

public sealed record ClaimValue
{
    public ClaimValueKind Kind { get; }
    public string Value { get; }

    private ClaimValue(ClaimValueKind kind, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A claim value is required.");

        Kind = kind;
        Value = value;
    }

    public static ClaimValue Entity(string entityId) =>
        new(ClaimValueKind.EntityReference, entityId);

    public static ClaimValue Literal(string value) =>
        new(ClaimValueKind.Literal, value);
}

public sealed record TemporalValidity
{
    public DateOnly? ValidFrom { get; }
    public DateOnly? ValidUntil { get; }

    public TemporalValidity(
        DateOnly? validFrom,
        DateOnly? validUntil)
    {
        if (validFrom.HasValue &&
            validUntil.HasValue &&
            validUntil < validFrom)
        {
            throw new ArgumentException(
                "ValidUntil cannot precede ValidFrom.");
        }

        ValidFrom = validFrom;
        ValidUntil = validUntil;
    }
}

public sealed record Claim
{
    public Guid Id { get; }
    public string SubjectId { get; }
    public string Predicate { get; }
    public ClaimValue Object { get; }

    public ClaimNature Nature { get; }
    public ClaimStatus Status { get; }
    public ConfidenceLevel Confidence { get; }

    public TemporalValidity Validity { get; }

    public DateTimeOffset RecordedAt { get; }

    public Claim(
        Guid id,
        string subjectId,
        string predicate,
        ClaimValue @object,
        ClaimNature nature,
        ConfidenceLevel confidence,
        TemporalValidity validity,
        DateTimeOffset recordedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Claim ID is required.");

        if (string.IsNullOrWhiteSpace(subjectId))
            throw new ArgumentException("Subject is required.");

        if (string.IsNullOrWhiteSpace(predicate))
            throw new ArgumentException("Predicate is required.");

        Id = id;
        SubjectId = subjectId;
        Predicate = predicate;
        Object = @object ?? throw new ArgumentNullException(nameof(@object));

        Nature = nature;
        Confidence = confidence;
        Validity = validity ?? throw new ArgumentNullException(nameof(validity));

        Status = ClaimStatus.Active;
        RecordedAt = recordedAt;
    }
}
