namespace Prixo.Application.Common.Exceptions;

public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with key '{key}' was not found.");
