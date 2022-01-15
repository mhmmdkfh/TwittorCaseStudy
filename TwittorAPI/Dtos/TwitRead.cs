using System;

namespace TwittorAPI.Dtos
{
    public partial class TwitRead
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Twit { get; set; }
        public DateTime Created { get; set; }
    }
}
