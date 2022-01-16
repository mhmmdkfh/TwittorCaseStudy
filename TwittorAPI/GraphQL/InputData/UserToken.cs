namespace TwittorAPI.GraphQL.InputData
{
    public record UserToken
    (
        string Token,
        string Expired,
        string Message
    );
}
