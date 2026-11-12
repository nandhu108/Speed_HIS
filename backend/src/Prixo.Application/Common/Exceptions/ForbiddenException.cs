namespace Prixo.Application.Common.Exceptions;

public class ForbiddenException(string message = "You do not have permission to perform this action.")
    : Exception(message);
