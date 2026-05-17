using System.Security.Claims;
using LotteryDetection.Authorization.Users;

namespace LotteryDetection.Authorization.Impersonation;

public class UserAndIdentity
{
    public UserAndIdentity(User user, ClaimsIdentity identity)
    {
        User = user;
        Identity = identity;
    }

    public User User { get; set; }

    public ClaimsIdentity Identity { get; set; }
}