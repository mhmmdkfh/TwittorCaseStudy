using System;

namespace TwittorAPI.Dtos
{
    public partial class CommentRead
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TwitId { get; set; }
        public string Comment { get; set; }
        public DateTime Created { get; set; }
    }
}
