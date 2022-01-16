using System;
using System.Collections.Generic;

#nullable disable

namespace KafkaApp.Models
{
    public partial class Comment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TwitId { get; set; }
        public string Comment1 { get; set; }
        public DateTime Created { get; set; }

        public virtual Twittor Twit { get; set; }
    }
}
