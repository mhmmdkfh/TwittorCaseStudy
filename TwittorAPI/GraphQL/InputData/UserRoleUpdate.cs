namespace TwittorAPI.GraphQL.InputData
{
    public record UserRoleUpdate
    (
        int UserId,
        int OldRoleId,
        int NewRoleId
    );
    
}
