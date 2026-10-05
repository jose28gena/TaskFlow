namespace TaskFlow.Application.Common;

public class NotFoundException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

public class InvalidCredentialsException(string message) : Exception(message);
