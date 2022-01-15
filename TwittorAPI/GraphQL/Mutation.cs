using HotChocolate;
using System;
using System.Linq;
using System.Threading.Tasks;
using TwittorAPI.Dtos;
using TwittorAPI.GraphQL.InputData;
using TwittorAPI.Models;

namespace TwittorAPI.GraphQL
{
    public class Mutation
    {
        //User
        public async Task<UserRead> RegisterUserAsync(
            RegisterUser input,
            [Service] TwittorContext context)
        {
            var user = context.Users.Where(u => u.Username == input.UserName).FirstOrDefault();
            if (user != null)
            {
                return await Task.FromResult(new UserRead());
            }
            var newUser = new User
            {
                FullName = input.FullName,
                Email = input.Email,
                Username = input.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(input.Password),
                Created = DateTime.Now
            };

            var ret = context.Users.Add(newUser);
            await context.SaveChangesAsync();

            return await Task.FromResult(new UserRead
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                FullName = newUser.FullName,
                Created = newUser.Created
            });
        }

        //Twittor
        public async Task<TwitRead> AddTwitAsync(
            TwitInput input,
            [Service] TwittorContext context)
        {
            try
            {
                var newTwit = new Twittor
                {
                    UserId = input.UserId,
                    Twit = input.twit,
                    Created = DateTime.Now
                };

                var ret = context.Twittors.Add(newTwit);
                await context.SaveChangesAsync();

                return await Task.FromResult(new TwitRead
                {
                    Id = newTwit.Id,
                    UserId = newTwit.UserId,
                    Twit = newTwit.Twit,
                    Created = newTwit.Created
                });
            }
            catch (Exception ex)
            {

                throw new Exception($"Error: {ex.Message}");
            }
        }

        //Comment
        public async Task<CommentRead> AddCommentAsync(
           CommentInput input,
           [Service] TwittorContext context)
        {
            var newComment = new Comment
            {
                UserId = input.UserId,
                TwitId = input.TwitId,
                Comment1 = input.Comment1,
                Created = DateTime.Now
            };

            var ret = context.Comments.Add(newComment);
            await context.SaveChangesAsync();

            return await Task.FromResult(new CommentRead
            {
                Id = newComment.Id,
                UserId = newComment.UserId,
                TwitId = newComment.TwitId,
                Comment = newComment.Comment1,
                Created = newComment.Created
            });
        }
    }
}
