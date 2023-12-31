using Chirp.Core;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure;

public class FollowRepository : IFollowRepository
{
    private readonly ChirpDBContext dbContext;

    /// <summary>
    /// This Repository contains all direct communication with the database.
    /// We use this Repository to abstract the data access layer from the rest of the application.
    /// </summary>
    /// <param name="dbContext">The ChirpDBContext that will be injected into this repo</param>
    public FollowRepository(ChirpDBContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Adds a Follow row to the Follow table
    /// </summary>
    /// <param name="followerName">The name of the person who will follow</param>
    /// <param name="followingName">The name of the person who will be followed</param>
    /// <exception cref="ArgumentNullException">The names cannot be null</exception>
    /// <exception cref="ArgumentException">The names have to match Authors in the database</exception>
    public async Task AddFollower(string followerName, string followingName)
    {
        if (followerName is null)
            throw new ArgumentNullException(nameof(followerName));
        if (followingName is null)
            throw new ArgumentNullException(nameof(followingName));

        Author? followerAuthor = await dbContext.Authors.SingleOrDefaultAsync(a => a.Name == followerName);
        Author? followingAuthor = await dbContext.Authors.SingleOrDefaultAsync(a => a.Name == followingName);

        if (followerAuthor is null)
            throw new ArgumentException($"Author with name '{followerName}' not found.");

        if (followingAuthor is null)
            throw new ArgumentException($"Author with name '{followingName}' not found.");

        if (await IsFollowing(followerName, followingName))
            throw new ArgumentException($"This follow already exists: '{followerName}' -> '{followingName}'");

        Follow newFollow = new Follow()
        {
            Follower = followerAuthor,
            FollowerId = followerAuthor.AuthorId,
            Following = followingAuthor,
            FollowingId = followingAuthor.AuthorId
        };

        dbContext.Follows.Add(newFollow);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a Follow row from the Follow table
    /// </summary>
    /// <param name="followerName">The name of the person who will unfollow</param>
    /// <param name="followingName">The name of the person who will be unfollowed</param>
    /// <exception cref="ArgumentNullException">The names cannot be null</exception>
    /// <exception cref="ArgumentException">The names have to match Authors in the database. And you can't unfollow yourself</exception>
    public async Task RemoveFollower(string followerName, string followingName)
    {
        if (followerName is null)
            throw new ArgumentNullException(nameof(followerName));
        if (followingName is null)
            throw new ArgumentNullException(nameof(followingName));
        if (followerName == followingName)
            throw new ArgumentException(followerName + " You can't unfollow yourself");

        Follow? follow = await dbContext.Follows
            .Include(f => f.Follower)
            .Include(f => f.Following)
            .SingleOrDefaultAsync(f => f.Follower.Name == followerName && f.Following.Name == followingName);

        if (follow is null)
            throw new ArgumentException($"follow between '{followerName}' -> '{followingName}' not found.");

        dbContext.Follows.Remove(follow);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Checks if an Author follows another Author
    /// </summary>
    /// <param name="followerName">The name of the person who might be following</param>
    /// <param name="followingName">The name of the person who might be followed</param>
    /// <returns>True if the follower is following</returns>
    /// <exception cref="ArgumentNullException">The names cannot be null</exception>
    public async Task<bool> IsFollowing(string followerName, string followingName)
    {
        if (followerName is null)
            throw new ArgumentNullException(nameof(followerName));
        if (followingName is null)
            throw new ArgumentNullException(nameof(followingName));

        Follow? follow = await GetFollow(followerName, followingName);

        return follow is not null;
    }

    /// <summary>
    ///  returns a IEnumerable list that contains all FollowDTO from all authors that a given author is following
    /// </summary>
    /// <param name="authorName">name of the author who is following </param>
    /// <exception cref="ArgumentNullException"> checks that the author name is not null</exception>
    /// <exception cref="ArgumentException">checks that the author with the author name exists</exception>
    public async Task<IEnumerable<FollowDTO>> GetFollowing(string authorName)
    {
        if (authorName is null)
            throw new ArgumentNullException(nameof(authorName));

        Author? author = await dbContext.Authors.SingleOrDefaultAsync(a => a.Name == authorName);
        if (author is null)
            throw new ArgumentException($"Author with name '{authorName}' not found.");

        List<FollowDTO> following = await dbContext.Follows
            .Include(f => f.Following)
            .Where(f => f.FollowerId == author.AuthorId)
            .OrderByDescending(f => f.Following.Name)
            .Select(f => new FollowDTO(author, f.Following))
            .ToListAsync();

        return following;
    }

    /// <summary>
    /// Find a Follow in the database
    /// </summary>
    /// <param name="followerName">The name of the person who is following</param>
    /// <param name="followingName">The name of the person who is followed</param>
    /// <returns>A Follow object if one exists in the database. Null if not</returns>
    private async Task<Follow?> GetFollow(string followerName, string followingName)
    {
        Follow? follow = await dbContext.Follows
            .Include(f => f.Follower)
            .Include(f => f.Following)
            .SingleOrDefaultAsync(f => f.Follower.Name == followerName && f.Following.Name == followingName);
        return follow;
    }
}