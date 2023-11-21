using Chirp.Core;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Infrastructure;

public interface ICheepRepository
{
    Task<IEnumerable<CheepViewModel>> GetPageOfCheepsByAuthor(string authorName, int pageNumber, int pageSize);

    Task<IEnumerable<CheepViewModel>> GetPageOfCheeps(int pageNumber, int pageSize);

    Task<int> GetCheepPageAmountAll();

    Task<int> GetCheepPageAmountAuthor(string authorName);
    Task CreateCheep(string authorName, string text, DateTime timestamp);
    Task RemoveCheep(int cheepId);
}

public class CheepRepository : ICheepRepository
{

    private readonly ChirpDBContext dbContext;

    int pageSize = 32;

    /// <summary>
    /// This Repository contains all direct communication with the database.
    /// We use this Repository to abstract hte data access layer from the rest of the application.
    /// </summary>
    /// <param name="dbContext">The ChirpDBContext that will be injected into this repo</param>
    public CheepRepository(ChirpDBContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Returns a list of Cheeps (size: PageSize) written by the specified Author sorted by time posted.
    /// </summary>
    /// <param name="authorName">The name of the Author</param>
    /// <param name="pageNumber">The page number (starts at 1)</param>
    /// <returns>A IEnumerable of Cheeps</returns>
    public async Task<IEnumerable<CheepViewModel>> GetPageOfCheepsByAuthor(string authorName, int pageNumber, int pageSize)
    {
        if (authorName is null)
            throw new ArgumentNullException(nameof(authorName));
        if (pageNumber < 1)
            throw new ArgumentException("Page number cannot be under 1");

        Author? author = await dbContext.Authors.SingleOrDefaultAsync(a => a.Name == authorName);
        if (author is null)
            throw new ArgumentException($"Author with name '{authorName}' not found.");
        
        int skipCount = (pageNumber - 1) * pageSize;

        List<CheepViewModel> cheepList = await dbContext.Cheeps
            .Where(c => c.AuthorId == author.AuthorId)
            .OrderByDescending(c => c.TimeStamp)
            .Skip(skipCount)
            .Take(pageSize)
            .Select(c => new CheepViewModel(author.Name, c.Text, c.TimeStamp))
            .ToListAsync();
        return cheepList;
    }

    /// <summary>
    /// Returns a list of Cheeps (size: PageSize) sorted by time posted.
    /// </summary>
    /// <param name="pageNumber">The page number (starts at 1)</param>
    /// <returns></returns>
    public async Task<IEnumerable<CheepViewModel>> GetPageOfCheeps(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number cannot be under 1");

        int skipCount = (pageNumber - 1) * pageSize;

        List<CheepViewModel> cheepList = await dbContext.Cheeps
            .Include(c => c.Author)
            .OrderByDescending(c => c.TimeStamp)
            .Skip(skipCount)
            .Take(pageSize)
            .Select(c => new CheepViewModel(c.Author.Name, c.Text, c.TimeStamp))
            .ToListAsync();

        return cheepList;
    }
    /// <summary>
    /// Returns the total amount of cheeps in the database
    /// </summary>
    /// <returns>an int count of the amount of cheeps</returns>
    public async Task<int> GetCheepPageAmountAll() {

        List<CheepViewModel> cheepList = await dbContext.Cheeps
            .Include(c => c.Author)
            .Select(c => new CheepViewModel(c.Author.Name, c.Text, c.TimeStamp))
            .ToListAsync();
        
        int totalPages = cheepList.Count()/pageSize;
        //Adds one extra page if the amount if cheeps is not perfectly divisible by the page size, where the remaining cheeps can be shown
        if (cheepList.Count()%pageSize != 0){
            totalPages++;
        }
        return totalPages;
    }
    /// <summary>
    /// Returns the amount of cheeps associated with a specific author
    /// </summary>
    /// <param name="authorName">The author whose cheeps are to be counted</param>
    /// <returns>an int count of the amount of cheeps</returns>
    public async Task<int> GetCheepPageAmountAuthor(string authorName){
        if (authorName is null)
            throw new ArgumentNullException(nameof(authorName));

        Author? author = await dbContext.Authors.SingleOrDefaultAsync(a => a.Name == authorName);
        if (author is null)
            throw new ArgumentException($"Author with name '{authorName}' not found.");

        List<CheepViewModel> cheepList = await dbContext.Cheeps
            .Where(c => c.AuthorId == author.AuthorId)
            .Select(c => new CheepViewModel(author.Name, c.Text, c.TimeStamp))
            .ToListAsync();

        int totalPages = cheepList.Count()/pageSize;
        //Adds one extra page if the amount if cheeps is not perfectly divisible by the page size, where the remaining cheeps can be shown
        if (cheepList.Count()%pageSize != 0){
            totalPages++;
        }
        return totalPages;
        
    }

    /// <summary>
    /// Adds a new Cheep to the database.
    /// </summary>
    /// <param name="authorName">The name of the Author of the Cheep</param>
    /// <param name="text">The text in the Cheep</param>
    /// <param name="timestamp">The time the Cheep was posted</param>
    public async Task CreateCheep(string authorName, string text, DateTime timestamp)
    {
        if (authorName is null)
            throw new ArgumentNullException(nameof(authorName));
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        Author? author = await dbContext.Authors.SingleOrDefaultAsync(a => a.Name == authorName);

        if (author is null)
            throw new ArgumentException($"Author with name '{authorName}' not found.");

        Cheep newCheep = new Cheep() { Author = author, Text = text, TimeStamp = timestamp };

        dbContext.Cheeps.Add(newCheep);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a Cheep from the database.
    /// </summary>
    /// <param name="cheepId">The ID of the Cheep</param>
    public async Task RemoveCheep(int cheepId)
    {
        Cheep? cheep = await dbContext.Cheeps.SingleOrDefaultAsync(c => c.CheepId == cheepId);

        if (cheep is null)
            throw new ArgumentException($"Cheep with ID '{cheepId}' not found.");

        dbContext.Cheeps.Remove(cheep);
        await dbContext.SaveChangesAsync();
    }
}