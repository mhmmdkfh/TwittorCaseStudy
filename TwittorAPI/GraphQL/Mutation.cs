using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TwittorAPI.Dtos;
using TwittorAPI.GraphQL.InputData;
using TwittorAPI.Kafka;
using TwittorAPI.Models;

namespace TwittorAPI.GraphQL
{
    public class Mutation
    {
        //User
        public async Task<TransactionStatus> RegisterUserAsync(
            RegisterUser input,
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var user = context.Users.Where(o => o.Username == input.UserName).FirstOrDefault();
            if (user != null)
            {
                return new TransactionStatus(false, "User already exist");
            }
            var newUser = new User
            {
                FullName = input.FullName,
                Email = input.Email,
                Username = input.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(input.Password),
                Created = DateTime.Now
            };

            var key = "user-add-" + DateTime.Now.ToString();
            var val = JObject.FromObject(newUser).ToString(Formatting.None);
            var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "user", key, val);
            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

            var messageResult = new TransactionStatus(result, "");
            if (!result)
                messageResult = new TransactionStatus(result, "Failed to submit data");


            return await Task.FromResult(messageResult);
        }

        public async Task<UserToken> LoginAsync(
            LoginInput input,
            [Service] IOptions<TokenSettings> tokenSettings,
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var user = context.Users.Where(o => o.Username == input.Username).FirstOrDefault();
            if (user == null)
            {
                return await Task.FromResult(new UserToken(null, null, "Username or password was invalid"));
            }
            bool valid = BCrypt.Net.BCrypt.Verify(input.Password, user.Password);
            if (valid)
            {
                var securitykey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSettings.Value.Key));
                var credentials = new SigningCredentials(securitykey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, user.Username));
                var userRoles = context.UserRoles.Where(o => o.UserId == user.Id).ToList();

                foreach (var userRole in userRoles)
                {
                    var role = context.Roles.Where(o => o.Id == userRole.RoleId).FirstOrDefault();
                    if (role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
                    }
                }

                var expired = DateTime.Now.AddHours(3);
                var jwtToken = new JwtSecurityToken(
                    issuer: tokenSettings.Value.Issuer,
                    audience: tokenSettings.Value.Audience,
                    expires: expired,
                    claims: claims,
                    signingCredentials: credentials
                );

                var key = "user-login-" + DateTime.Now.ToString();
                var val = JObject.FromObject(new { Message = $"{input.Username} has signed in" }).ToString(Formatting.None);
                await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

                return await Task.FromResult(
                    new UserToken(new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    expired.ToString(), null));
            }

            return await Task.FromResult(new UserToken(null, null, Message: "Username or password was invalid"));
        }

        public async Task<TransactionStatus> AddRoleAsync(
            string roleName,
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var role = context.Roles.Where(o => o.RoleName == roleName).FirstOrDefault();
            if (role != null)
            {
                return new TransactionStatus(false, "Role already exist");
            }
            var newRole = new Role
            {
                RoleName = roleName
            };

            var key = "role-add-" + DateTime.Now.ToString();
            var val = JObject.FromObject(newRole).ToString(Formatting.None);
            var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "role", key, val);
            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

            var ret = new TransactionStatus(result, "");
            if (!result)
                ret = new TransactionStatus(result, "Failed to submit data");

            return await Task.FromResult(ret);
        }

        public async Task<TransactionStatus> AddRoleToUserAsync(
            UserRoleInput input,
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var userRole = context.UserRoles.Where(o => o.UserId == input.UserId &&
            o.RoleId == input.RoleId).FirstOrDefault();
            if (userRole != null)
            {
                return new TransactionStatus(false, "Role already exist in this user");
            }

            var newUserRole = new UserRole
            {
                UserId = input.UserId,
                RoleId = input.RoleId
            };

            var key = "user-role-add-" + DateTime.Now.ToString();
            var val = JObject.FromObject(newUserRole).ToString(Formatting.None);
            var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "userrole", key, val);
            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

            var ret = new TransactionStatus(result, "");
            if (!result)
                ret = new TransactionStatus(result, "Failed to submit data");

            return await Task.FromResult(ret);
        }

        [Authorize(Roles = new[] { "ADMIN" })]
        public async Task<TransactionStatus> ChangeUserRoleAsync(
            UserRoleUpdate input,
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var userRole = context.UserRoles.Where(o => o.UserId == input.UserId && o.RoleId == input.OldRoleId).FirstOrDefault();
            if (userRole != null)
            {
                userRole.RoleId = input.NewRoleId;
                var key = "change-user-role-" + DateTime.Now.ToString();
                var val = JObject.FromObject(userRole).ToString(Formatting.None);
                var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "changeuserrole", key, val);
                await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

                var ret = new TransactionStatus(result, "");
                if (!result)
                    ret = new TransactionStatus(result, "Failed to submit data");
                return await Task.FromResult(ret);
            };
            return new TransactionStatus(false, "User doesn't exist");
        }

        [Authorize(Roles = new[] { "ADMIN" })]
        public async Task<TransactionStatus> LockUserAsync(
            int userId,
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var userRoles = context.UserRoles.Where(o => o.UserId == userId).ToList();
            bool check = false;
            if (userRoles != null)
            {
                foreach (var userRole in userRoles)
                {
                    var key = "Lock-User-" + DateTime.Now.ToString();
                    var val = JObject.FromObject(userRole).ToString(Formatting.None);
                    var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "lockuser", key, val);
                    await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);
                    var ret = new TransactionStatus(result, "");
                    check = true;
                };

                if (!check)
                    return new TransactionStatus(false, "Failed to submit data");
                return await Task.FromResult(new TransactionStatus(true, ""));
            }
            else
            {
                return new TransactionStatus(false, "User doesnt have any role yet");
            }
        }

        [Authorize(Roles = new[] { "MEMBER", "ADMIN" })]
        public async Task<TransactionStatus> ChangePasswordAsync(
                    ChangePasswordInput input,
                    [Service] TwittorContext context,
                    [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var user = new User();
            user = context.Users.Where(o => o.Username == input.Username).FirstOrDefault();
            if (user == null)
            {
                return await Task.FromResult(new TransactionStatus(false, "Username Not Exist"));
            }
            var valid = BCrypt.Net.BCrypt.Verify(input.OldPassword, user.Password);
            if (valid)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
            }
            var key = "change-pass-" + DateTime.Now.ToString();
            var val = JObject.FromObject(user).ToString(Formatting.None);
            var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "changepassword", key, val);
            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

            var ret = new TransactionStatus(result, "");
            if (!result)
                ret = new TransactionStatus(result, "Failed to submit data");

            return await Task.FromResult(ret);
        }

        [Authorize(Roles = new[] { "MEMBER", "ADMIN" })]
        public async Task<TransactionStatus> EditProfileAsync(
                    UpdateProfileInput input,
                    [Service] TwittorContext context,
                    [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var profile = context.Users.Where(o => o.Id == input.UserId).FirstOrDefault();
            if (profile != null)
            {
                profile.FullName = input.FullName;
                profile.Email = input.Email;
                profile.Username = input.Username;

                var key = "update-profile-" + DateTime.Now.ToString();
                var val = JObject.FromObject(profile).ToString(Formatting.None);
                var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "updateprofile", key, val);
                await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

                var ret = new TransactionStatus(result, "");
                if (!result)
                    ret = new TransactionStatus(result, "Failed to submit data");
                return await Task.FromResult(ret);
            }
            else
            {
                return new TransactionStatus(false, "Profile doesn't exist");
            }
        }

        //Twittor
        [Authorize(Roles = new[] { "MEMBER" })]
        public async Task<TransactionStatus> AddTwitAsync(
            TwitInput input,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var newTwit = new Twittor
            {
                UserId = input.UserId,
                Twit = input.twit,
                Created = DateTime.Now
            };

            var key = "twittor-add-" + DateTime.Now.ToString();
            var val = JObject.FromObject(newTwit).ToString(Formatting.None);
            var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "twittor", key, val);
            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

            var ret = new TransactionStatus(result, "");
            if (!result)
                ret = new TransactionStatus(result, "Failed to submit data");


            return await Task.FromResult(ret);
        }

        [Authorize(Roles = new[] { "MEMBER" })]
        public async Task<TransactionStatus> DeleteTwittorAsync(
            int userId,
            [Service] TwittorContext context,
            [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var twets = context.Twittors.Where(o => o.UserId == userId).ToList();
            bool check = false;
            if (twets != null)
            {
                foreach (var twet in twets)
                {
                    var key = "delete-tweet-" + DateTime.Now.ToString();
                    var val = JObject.FromObject(twet).ToString(Formatting.None);
                    var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "deletetwit", key, val);
                    await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);
                    var ret = new TransactionStatus(result, "");
                    check = true;

                }
                if (!check)
                    return new TransactionStatus(false, "Failed to submit data");
                return await Task.FromResult(new TransactionStatus(true, ""));
            }
            else
            {
                return new TransactionStatus(false, "User has not tweeted yet");
            }
        }

        //Comment
        [Authorize(Roles = new[] { "MEMBER" })]
        public async Task<TransactionStatus> AddCommentAsync(
           CommentInput input,
           [Service] IOptions<KafkaSettings> kafkaSettings)
        {
            var newComment = new Comment
            {
                UserId = input.UserId,
                TwitId = input.TwitId,
                Comment1 = input.Comment1,
                Created = DateTime.Now
            };

            var key = "comment-add-" + DateTime.Now.ToString();
            var val = JObject.FromObject(newComment).ToString(Formatting.None);
            var result = await KafkaHelper.SendMessage(kafkaSettings.Value, "comment", key, val);
            await KafkaHelper.SendMessage(kafkaSettings.Value, "logging", key, val);

            var ret = new TransactionStatus(result, "");
            if (!result)
                ret = new TransactionStatus(result, "Failed to submit data");


            return await Task.FromResult(ret);
        }
    }
}
