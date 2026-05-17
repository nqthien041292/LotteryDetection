using System.Security.Claims;
using LotteryDetection.Authorization.Users;

namespace LotteryDetection.Authorization.Impersonation;

public class UserAndIdentity
{
    public User User { get; set; }

    public ClaimsIdentity Identity { get; set; }

    public UserAndIdentity(User user, ClaimsIdentity identity)
    {
        User = user;
        Identity = identity;
    }
}

