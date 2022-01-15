namespace TwittorAPI.GraphQL.InputData
{
    public record CommentInput
    (
        int? Id,
        int UserId,
        int TwitId,
        string Comment1
    );
}
