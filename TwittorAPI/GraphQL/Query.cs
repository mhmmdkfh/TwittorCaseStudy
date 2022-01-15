using HotChocolate;
using System.Linq;
using TwittorAPI.Dtos;
using TwittorAPI.Models;

namespace TwittorAPI.GraphQL
{
    public class Query
    {
        public IQueryable<Twittor> GetTwits([Service] TwittorContext context) =>
            context.Twittors;

        public IQueryable<Comment> GetComments([Service] TwittorContext context) =>
            context.Comments;

        public IQueryable<UserRead> GetProfileUser([Service] TwittorContext context) =>
            context.Users.Select(u => new UserRead()
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Username = u.Username
            });

        /*public IQueryable<User> GetUserByUsername([Service] TwittorContext context, string username)
        {
            var users = context.Users.Where(u => u.Username == username);


            return users;
        }*/
    }
}
