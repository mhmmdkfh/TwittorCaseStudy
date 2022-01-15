using System;

namespace TwittorAPI.Dtos
{
    public partial class UserRead
    {
        public int? Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public DateTime Created { get; set; }
    }
}
