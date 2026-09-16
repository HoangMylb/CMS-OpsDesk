namespace OpsDesk.Core.Services;

/// <summary>
/// A lightweight result wrapper that lets a service communicate success or failure
/// back to the controller WITHOUT throwing exceptions for expected business errors.
///
/// Why not just throw exceptions?
/// Exceptions should signal UNEXPECTED failures (database down, null ref, etc.).
/// A business rule violation — "you can't assign to an inactive employee" — is an
/// expected outcome. Returning a ServiceResult keeps the controller code clean and
/// avoids the overhead of exception handling for normal business flows.
///
/// Usage:
///   var result = await _ticketService.AssignAsync(...);
///   if (!result.Succeeded) { ModelState.AddModelError("", result.FirstError); }
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; } = [];

    public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

    private ServiceResult() { }

    public static ServiceResult Success() => new() { Succeeded = true };

    public static ServiceResult Failure(string error) =>
        new() { Succeeded = false, Errors = [error] };

    public static ServiceResult Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToList() };
}

/// <summary>Generic variant for operations that return data on success.</summary>
public class ServiceResult<T>
{
    public bool Succeeded { get; private set; }
    public T? Data { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; } = [];

    public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

    private ServiceResult() { }

    public static ServiceResult<T> Success(T data) =>
        new() { Succeeded = true, Data = data };

    public static ServiceResult<T> Failure(string error) =>
        new() { Succeeded = false, Errors = [error] };

    public static ServiceResult<T> Failure(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToList() };
}
