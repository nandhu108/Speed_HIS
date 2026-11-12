namespace Prixo.Domain.Exceptions;

/// <summary>
/// Business-rule violation. The message is safe to show to end users and maps to HTTP 400.
/// </summary>
public class DomainException(string message) : Exception(message);
