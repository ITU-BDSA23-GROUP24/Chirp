using Chirp.Core;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure;

public record AuthorViewModel(string AuthorName, string AuthorEmail);

public interface IAuthorRepository
{
    Task CreateAuthor(string authorName, string authorEmail);
    Task RemoveAuthor(string authorName);
    Task<AuthorViewModel> FindAuthorByName(string authorName);
    Task<AuthorViewModel> FindAuthorByEmail(string authorEmail);
}

public class AuthorRepository : IAuthorRepository
{
    public const int PageSize = 32;

    private readonly ChirpDBContext dbContext;

    /// <summary>
    /// This Repository contains all direct communication with the database.
    /// We use this Repository to abstract hte data access layer from the rest of the application.
    /// </summary>
    /// <param name="dbContext">The ChirpDBContext that will be injected into this repo</param>
    public AuthorRepository(ChirpDBContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Creates a new author
    /// </summary>
    /// <param name="authorName">The name of the new author</param>
    /// <param name="authorEmail">The email of the new author</param>
    /// <exception cref="ArgumentException">If an author with the authorName already exists</exception>
    public async Task CreateAuthor(string authorName, string authorEmail)
    {
        Author? author = await dbContext.Authors.SingleOrDefaultAsync(a => a.Name == authorName);
        if (author is not null)
            throw new ArgumentException($"Author with name '{authorName}' already exists");

        Author newAuthor = new Author() { Name = authorName, Email = authorEmail };

        dbContext.Authors.Add(newAuthor);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Removes an Author and all of their Cheeps.
    /// </summary>
    /// <param name="authorName">The name of the Author</param>
    /// <exception cref="ArgumentException">If an author with authorName doesn't exist</exception>
    public async Task RemoveAuthor(string authorName)
    {
        Author? author = await dbContext.Authors
            .Where(a => a.Name == authorName)
            .Include(a => a.Cheeps)
            .SingleOrDefaultAsync();

        if (author is null)
            throw new ArgumentException($"Author with name '{authorName}' not found.");

        dbContext.Cheeps.RemoveRange(author.Cheeps);
        dbContext.Authors.Remove(author);
        await dbContext.SaveChangesAsync();
    }
    
    /// <summary>
    /// finds an author by name
    /// </summary>
    /// <param name="authorName">The name of the author</param>
    /// <returns>A AuthorViewModel containing the Author's name and email</returns>
    /// <exception cref="ArgumentException">If an author with the authorName doesn't exist</exception>
    public async Task<AuthorViewModel> FindAuthorByName(string authorName)
    {
        Author? author = await dbContext.Authors.SingleOrDefaultAsync(a => a.Name == authorName);
        if (author is null)
            throw new ArgumentException($"Author with name '{authorName}' doesn't exists");

        AuthorViewModel authorViewModel = new AuthorViewModel(author.Name, author.Email);

        return authorViewModel;
    }

    /// <summary>
    /// finds a author by email
    /// </summary>
    /// <param name="authorEmail">The email of the author</param>
    /// <returns>A AuthorViewModel containing the Author's name and email</returns>
    /// <exception cref="ArgumentException">If an author with the authorEmail doesn't exist</exception>
    public async Task<AuthorViewModel> FindAuthorByEmail(string authorEmail)
    {
        Author? author = await dbContext.Authors.SingleOrDefaultAsync(a => a.Email == authorEmail);
        if (author is null)
            throw new ArgumentException($"Author with email '{authorEmail}' doesn't exists");

        AuthorViewModel authorViewModel = new AuthorViewModel(author.Name, author.Email);

        return authorViewModel;
    }
}