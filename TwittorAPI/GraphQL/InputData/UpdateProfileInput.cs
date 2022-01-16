namespace TwittorAPI.GraphQL.InputData
{
    public record UpdateProfileInput
    (
        int? UserId,
        string FullName,
        string Email,
        string Username
    );
}
