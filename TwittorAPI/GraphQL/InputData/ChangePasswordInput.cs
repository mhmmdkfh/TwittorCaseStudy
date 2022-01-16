namespace TwittorAPI.GraphQL.InputData
{
    public record ChangePasswordInput
    (
        string Username,
        string OldPassword,
        string NewPassword
    );
}
