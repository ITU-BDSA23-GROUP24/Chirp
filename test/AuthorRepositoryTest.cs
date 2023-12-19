using Xunit;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;


public class AuthorRepositoryTest {
    ChirpDBContext context;
    SqliteConnection _connection;
    /// <summary>
    /// The setup for Authorrepo-test, here the connection to the database and the dbContext is created 
    /// </summary>
    public AuthorRepositoryTest()
    {
        _connection =  new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var _contextOptions = new DbContextOptionsBuilder<ChirpDBContext>().UseSqlite(_connection).Options;
        context = new ChirpDBContext(_contextOptions);
        context.Database.EnsureCreated();
        SeedDatabase(context);
    }
     /// <summary>
    /// method for seeding the database with data for testing
    /// </summary>
    /// <param name="context"> the context dbcontext for testing</param>
    void SeedDatabase(ChirpDBContext context) {
        var a1 = new Author() { AuthorId = 1, Name = "existingAuthor", Email = "existingEmail@mail.com", Cheeps = new List<Cheep>() };
        var a2 = new Author() { AuthorId = 2, Name = "Luanna Muro", Email = "Luanna-Muro@ku.dk", Cheeps = new List<Cheep>() };
        
        var authors = new List<Author>() { a1, a2};

        var c1 = new Cheep() { CheepId = 1, Author = a1, Text = "They were married in Chicago, with old Smith, and was expected aboard every day; meantime, the two went past me.", TimeStamp = DateTime.Parse("2023-08-01 13:14:37") };
        var c2 = new Cheep() { CheepId = 2, Author = a1, Text = "And then, as he listened to all that''s left o'' twenty-one people.", TimeStamp = DateTime.Parse("2023-08-01 13:15:21") };
        var c3 = new Cheep() { CheepId = 3, Author = a2, Text = "In various enchanted attitudes, like the Sperm Whale.", TimeStamp = DateTime.Parse("2023-08-01 13:14:58") };
        var c4 = new Cheep() { CheepId = 4, Author = a1, Text = "Unless we succeed in establishing ourselves in some monomaniac way whatever significance might lurk in them.", TimeStamp = DateTime.Parse("2023-08-01 13:14:34") };
        var c5 = new Cheep() { CheepId = 5, Author = a2, Text = "At last we came back!", TimeStamp = DateTime.Parse("2023-08-01 13:14:35") };
        
        var cheeps = new List<Cheep>(){c1,c2,c3,c4,c5};

        a1.Cheeps = new List<Cheep>() { c1, c2,c4 };
        a2.Cheeps = new List<Cheep>() { c3,c5 };
        context.Authors.AddRange(authors);
        context.Cheeps.AddRange(cheeps);
        context.SaveChanges();
    }

    /// <summary>
    /// tests that different ways of creating an author executes correctly 
    /// </summary>
    /// <param name="authorName"></param>
    /// <param name="authorEmail"></param>
    [Theory]
    //basic format
    [InlineData("someName","someEmail@mail.com")]
    //name with space in 
    [InlineData("some Name","someEmail@mail.com")]
    //name with special charectors
    [InlineData("some_Name?!\\","someEmail@mail.com")]
    //email with special charactors
    [InlineData("someName","some.?\\Email@mail.com")]
    public async void CreateNonexistingAuthor(string authorName, string authorEmail){
        //arrange
        AuthorRepository authorRepository = new(context);
        //act
        int size = context.Authors.Count();
        await authorRepository.CreateAuthor(authorName,authorEmail);
        
        //assert
        Assert.Equal(size+1, context.Authors.Count());
        Assert.NotNull(context.Authors
            .SingleOrDefault(f => f.Name == authorName && f.Email == authorEmail));
    }
    
    /// <summary>
    /// creates author with data already in the database
    /// </summary>
    /// <param name="authorName"></param>
    /// <param name="authorEmail"></param>
    [Theory]
    [InlineData("existingAuthor","existingEmail@mail.com")]
    public async void CreateAuthorsWithExistingData(string authorName, string authorEmail){
        //arrange
        AuthorRepository authorRepository = new(context);
        
        //act
        int size = context.Authors.Count();
        async Task result() => await authorRepository.CreateAuthor(authorName, authorEmail);

        //assert
        await Assert.ThrowsAsync<ArgumentException>(result);
        Assert.Equal(size, context.Authors.Count());  
        Assert.NotNull(context.Authors
            .SingleOrDefault(f => f.Name == authorName));
    }

    /// <summary>
    /// tests that the correct Exeption is raised if the CreateAuthor 
    /// method is called with a null values
    /// </summary>
    /// <param name="authorName"></param>
    /// <param name="authorEmail"></param>
    [Theory]
    //both as null
    [InlineData(null,null)]
    //name as null
    [InlineData(null,"someEmail@mail.com")]
    //mail as unll
    [InlineData("someName",null)]
    public async void CreateAuthorNullValue(string authorName, string authorEmail){
        //arrange
        AuthorRepository authorRepository = new(context);
        
        //act
        int size = context.Authors.Count();
        async Task result() => await authorRepository.CreateAuthor(authorName, authorEmail);

        //assert
        await Assert.ThrowsAsync<DbUpdateException>(result);
        Assert.Equal(size, context.Authors.Count()); 
        Assert.Null(context.Authors
            .SingleOrDefault(f => f.Name == authorName));
        
    }

    /// <summary>
    /// tests that the correct Exeption is raised if the CreateAuthor 
    /// method is called twice in a row, witch would otherwise create dublicate authors
    /// </summary>
    /// <param name="authorName">should not already exist in the database</param>
    /// <param name="authorEmail">should not already exist in the database</param>
    [Theory]
    //basic format
    [InlineData("someName","someEmail@mail.com")]
    public async void CreateAuthorTwiceTest(string authorName, string authorEmail){
        //arrange
        AuthorRepository authorRepository = new(context);
        
        //act
        int size = context.Authors.Count();
        await authorRepository.CreateAuthor(authorName, authorEmail);
        async Task result() => await authorRepository.CreateAuthor(authorName, authorEmail);

        //assert
        await Assert.ThrowsAsync<ArgumentException>(result);
        Assert.Equal(size+1, context.Authors.Count());  
        Assert.NotNull(context.Authors
            .SingleOrDefault(f => f.Name == authorName && f.Email == authorEmail));
    }

    /// <summary>
    /// tests that removing an author that exists in the database works correctly
    /// </summary>
    /// <param name="authorName"></param>
    [Theory]
    //Existing author values
    [InlineData("existingAuthor")]
    public async void RemoveExistingAuthorTest(string authorName){
        //arrange
        AuthorRepository authorRepository = new AuthorRepository(context);

        //act
        int size = context.Authors.Count();
        await authorRepository.RemoveAuthor(authorName);
        
        //assert
        Assert.Equal(size-1, context.Authors.Count());
        Assert.Null(context.Authors
            .SingleOrDefault(f => f.Name == authorName));
        Assert.False(context.Cheeps.Any(f => f.Author.Name == authorName));
        Assert.False(context.Follows.Any(f => f.Follower.Name == authorName));
        Assert.False(context.Follows.Any(f => f.Following.Name == authorName));


    }
    /// <summary>
    /// tests that removing an author with null as the name value returns an error
    /// </summary>
    /// <param name="authorName"></param>
    [Theory]
    //both as null
    [InlineData(null)]
    public async void RemoveAuthorNullValue(string authorName){
        //arrange
        AuthorRepository authorRepository = new AuthorRepository(context);

        //act
        async Task result() => await authorRepository.RemoveAuthor(authorName);

        //assert
        await Assert.ThrowsAsync<ArgumentNullException>(result);
    }
    /// <summary>
    /// tests that removing an author that exists in the database twice is no different from removing them once
    /// </summary>
    /// <param name="authorName"></param>
    [Theory]
    //both as null
    [InlineData("existingAuthor")]
    public async void RemoveAuthorTwice(string authorName){
        //arrange
        AuthorRepository authorRepository = new AuthorRepository(context);

        //act
        int size = context.Authors.Count();
        await authorRepository.RemoveAuthor(authorName);
        async Task result() => await authorRepository.RemoveAuthor(authorName);

        //assert
        await Assert.ThrowsAsync<ArgumentException>(result);
        Assert.Equal(size-1, context.Authors.Count()); 
        Assert.Null(context.Authors
            .SingleOrDefault(f => f.Name == authorName));

    }
    /// <summary>
    /// tests that removing an author that has never been in the database returns an error
    /// </summary>
    /// <param name="authorName"></param>
    [Theory]
    //basic format
    [InlineData("someName")]
    public async void RemoveNonexistingAuthor(string authorName){
        //arrange
        AuthorRepository authorRepository = new AuthorRepository(context);

        //act 
        async Task result() => await authorRepository.RemoveAuthor(authorName);
        //assert
        await Assert.ThrowsAsync<ArgumentException>(result);
    }
    /// <summary>
    /// tests that an author in the database can be found through their name
    /// </summary>
    /// <param name="authorName"></param>
    [Theory]
    //Existing author values
    [InlineData("existingAuthor")]
    public async void FindAuthorByNameTest(string authorName){
        //arrange
        AuthorRepository authorRepository = new AuthorRepository(context);

        //act
        AuthorViewModel foundAuthor = await authorRepository.FindAuthorByName(authorName);
        //assert
        Assert.Equal("existingAuthor", foundAuthor.AuthorName);  
        Assert.Equal("existingEmail@mail.com", foundAuthor.AuthorEmail);
    }
    /// <summary>
    /// tests that attempting to find an author that does not exist in the database through their name returns an error
    /// </summary>
    /// <param name="authorName"></param>
    [Theory]
    //basic format
    [InlineData("someName")]
    public async void FindNonexistingAuthorByName(string authorName){
        //arrange
        AuthorRepository authorRepository = new AuthorRepository(context);

        //act 
        async Task result() => await authorRepository.FindAuthorByName(authorName);
        //assert
        await Assert.ThrowsAsync<ArgumentException>(result);
    }
    /// <summary>
    /// tests that an author in the database can be found through their email
    /// </summary>
    /// <param name="authorEmail"></param>
    [Theory]
    //Existing author values
    [InlineData("existingEmail@mail.com")]
    public async void FindAuthorByEmailTest(string authorEmail){
           //arrange
        AuthorRepository authorRepository = new AuthorRepository(context);

        //act
        AuthorViewModel foundAuthor = await authorRepository.FindAuthorByEmail(authorEmail);
        //assert
        Assert.Equal("existingEmail@mail.com", foundAuthor.AuthorEmail);
        Assert.Equal("existingAuthor", foundAuthor.AuthorName);  
    }
    /// <summary>
    /// tests that attempting to find an author that does not exist in the database through their email returns an error
    /// </summary>
    /// <param name="authorEmail"></param>
    [Theory]
    //basic format
    [InlineData("someEmail@mail.com")]
    public async void FindNonexistingAuthorEmail(string authorEmail){
        //arrange
        AuthorRepository authorRepository = new AuthorRepository(context);

        //act 
        async Task result() => await authorRepository.FindAuthorByName(authorEmail);
        //assert
        await Assert.ThrowsAsync<ArgumentException>(result);
    }

    /// <summary>
    /// checks that the DoesUserNameExists task gives a true on a existing user
    /// </summary>
    /// <param name="authorName"> the name of the user that is checked</param>
    [Theory]
    //Author names
    [InlineData("existingAuthor")]
    
    public async void FindThatUserExistByName(string authorName)
    {
        //Arrange
        AuthorRepository authorRepository = new AuthorRepository(context);
        
        //act
         bool result = await authorRepository.DoesUserNameExists(authorName);
        //Assert
        
        Assert.True(result);
    }
    
    /// <summary>
    /// checks that the DoesUserNameExists task gives a false on a nonexisting user
    /// </summary>
    /// <param name="authorName"> the name of the user that is checked</param>
    [Theory]
    //Author names
    [InlineData("NonExistingAuthor")]
    
    public async void FindThatUserDoesNotExistByName(string authorName)
    {
        //Arrange
        AuthorRepository authorRepository = new AuthorRepository(context);
        
        //act
        bool result = await authorRepository.DoesUserNameExists(authorName);
        //Assert
        Assert.False(result);
    }
    
}
