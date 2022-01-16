using HotChocolate;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using TwittorAPI.Dtos;
using TwittorAPI.Kafka;
using TwittorAPI.Models;

namespace TwittorAPI.GraphQL
{
    public class Query
    {
        public async Task<IQueryable<Twittor>> GetTwits(
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var key = "GetTwits-" + DateTime.Now.ToString();
            var val = JObject.FromObject(new { Message = "GraphQL Query GetTwits" }).ToString(Formatting.None);

            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);
            return context.Twittors;
        }

        public async Task<IQueryable<Comment>> GetComments(
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var key = "GetComments-" + DateTime.Now.ToString();
            var val = JObject.FromObject(new { Message = "GraphQL Query GetComments" }).ToString(Formatting.None);

            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);
            return context.Comments;
        }

        public async Task<IQueryable<UserRead>> GetProfileUser(
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var key = "GetProfileUser" + DateTime.Now.ToString();
            var val = JObject.FromObject(new { Message = "GraphQL Query GetProfileUser" }).ToString(Formatting.None);

            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);
            return context.Users.Select(u => new UserRead()
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Username = u.Username
            });
        }
            

        /*public IQueryable<User> GetUserByUsername([Service] TwittorContext context, string username)
        {
            var users = context.Users.Where(u => u.Username == username);


            return users;
        }*/
    }
}
