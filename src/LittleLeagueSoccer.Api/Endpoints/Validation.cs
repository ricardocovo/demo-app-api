namespace LittleLeagueSoccer.Api.Endpoints;

public sealed class Validator
{
    public const int MinAge = 3;
    public const int MaxAge = 18;
    public const int MaxMinutes = 120;
    public const int MaxNameLength = 100;

    private readonly Dictionary<string, List<string>> _errors = new();

    public bool IsValid => _errors.Count == 0;

    public Validator Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
            _errors[field] = list = new List<string>();
        list.Add(message);
        return this;
    }

    public Validator Require(bool condition, string field, string message) =>
        condition ? this : Add(field, message);

    public Validator Name(string? value, string field = "name") =>
        Require(!string.IsNullOrWhiteSpace(value), field, $"{field} is required.")
            .Require(value is null || value.Trim().Length <= MaxNameLength, field,
                $"{field} must be at most {MaxNameLength} characters.");

    public IResult ToProblem() =>
        Results.ValidationProblem(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
}

public static class Problems
{
    public static IResult Conflict(string detail) =>
        Results.Problem(detail: detail, statusCode: StatusCodes.Status409Conflict, title: "Conflict");
}
