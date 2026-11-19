namespace Bison.Core.Domain;

// Observations that stay online after their author used "Forget me!" are
// handed to one shared placeholder author, so they show "deleted user".
public static class DeletedUser
{
    // Not a valid e-mail address, so nobody can register with it.
    public const string UserName = "deleted-user";

    // What the observation shows instead of the author's name.
    public const string Name = "deleted user";
}