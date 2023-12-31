using Chirp.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages;

public class FollowModel : PageModel
{
    private readonly IFollowRepository followRepository;

    public FollowModel(IFollowRepository followRepository)
    {
        this.followRepository = followRepository;
    }

    public async Task<bool> CheckFollow(string followingName)
    {
        if (User.Identity?.Name is null)
            return false;
        bool follows = await followRepository.IsFollowing(User.Identity.Name, followingName);
        return follows;
    }

    public async Task<IActionResult> OnGetAsync(string tofollow, string redirection)
    {
        if (User.Identity?.Name is not null && User.Identity?.IsAuthenticated == true && User.Identity.Name != tofollow)
        {
            if (await CheckFollow(tofollow))
            {
                await followRepository.RemoveFollower(User.Identity.Name, tofollow);
            }
            else
            {
                await followRepository.AddFollower(User.Identity.Name, tofollow);
            }
        }

        if (redirection == "public")
            return Redirect("/");

        return Redirect("/" + redirection);
    }
}