public class ForbiddenException : AppException
{
    public ForbiddenException(string message): base(message, StatusCodes.Status403Forbidden){}
}