namespace TwittorAPI.GraphQL
{
    public record RegisterUser
    (
        int? Id,
        string FullName,
        string Email,
        string UserName,
        string Password
    );
}
