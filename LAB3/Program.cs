using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// --- 1. Entities and DbContext ---

public class Book
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Author { get; set; }
    public int PublicationYear { get; set; }
}

public class BookDbContext : DbContext
{
    public DbSet<Book> Books => Set<Book>();

    public BookDbContext(DbContextOptions<BookDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", PublicationYear = 1925 },
            new Book { Id = 2, Title = "1984", Author = "George Orwell", PublicationYear = 1949 },
            new Book { Id = 3, Title = "Moby Dick", Author = "Herman Melville", PublicationYear = 1851 },
            new Book { Id = 4, Title = "Pride and Prejudice", Author = "Jane Austen", PublicationYear = 1813 },
            new Book { Id = 5, Title = "To Kill a Mockingbird", Author = "Harper Lee", PublicationYear = 1960 }
        );
    }
}

// --- 2. CQRS Contracts and DTOs ---

public interface ICommand { }
public interface IQuery<TResult> { }

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command);
}

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}

public record BookDto(int Id, string Title, string Author, int PublicationYear);
public record PaginatedResult<T>(int TotalCount, int PageIndex, int PageSize, IReadOnlyList<T> Items);

// --- 3. Commands & Queries ---

public record CreateBookCommand(string Title, string Author, int PublicationYear) : ICommand;
public record UpdateBookCommand(int Id, string Title, string Author, int PublicationYear) : ICommand;
public record DeleteBookCommand(int Id) : ICommand;

public record GetBookByIdQuery(int Id) : IQuery<BookDto?>;
public record GetAllBooksQuery(int PageIndex, int PageSize) : IQuery<PaginatedResult<BookDto>>;

// --- 4. Validator ---

public class BookCommandValidator
{
    public (bool IsValid, string Error) Validate(CreateBookCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd.Title) || string.IsNullOrWhiteSpace(cmd.Author))
            return (false, "Title and Author are required.");
        if (cmd.PublicationYear < 1000 || cmd.PublicationYear > DateTime.Now.Year)
            return (false, $"PublicationYear must be between 1000 and {DateTime.Now.Year}.");
        return (true, "");
    }

    public (bool IsValid, string Error) Validate(UpdateBookCommand cmd)
    {
        if (cmd.Id <= 0) return (false, "Invalid Id.");
        var inner = Validate(new CreateBookCommand(cmd.Title, cmd.Author, cmd.PublicationYear));
        return inner;
    }
}

// --- 5. Handlers ---

public class CreateBookHandler(BookDbContext ctx) : ICommandHandler<CreateBookCommand>
{
    public async Task HandleAsync(CreateBookCommand cmd)
    {
        ctx.Books.Add(new Book { Title = cmd.Title, Author = cmd.Author, PublicationYear = cmd.PublicationYear });
        await ctx.SaveChangesAsync();
    }
}

public class UpdateBookHandler(BookDbContext ctx) : ICommandHandler<UpdateBookCommand>
{
    public async Task HandleAsync(UpdateBookCommand cmd)
    {
        var book = await ctx.Books.FindAsync(cmd.Id);
        if (book is null) throw new InvalidOperationException($"Book with Id={cmd.Id} not found.");

        book.Title = cmd.Title;
        book.Author = cmd.Author;
        book.PublicationYear = cmd.PublicationYear;
        await ctx.SaveChangesAsync();
    }
}

public class DeleteBookHandler(BookDbContext ctx) : ICommandHandler<DeleteBookCommand>
{
    public async Task HandleAsync(DeleteBookCommand cmd)
    {
        var book = await ctx.Books.FindAsync(cmd.Id);
        if (book is null) return;
        ctx.Books.Remove(book);
        await ctx.SaveChangesAsync();
    }
}

public class GetBookByIdHandler(BookDbContext ctx) : IQueryHandler<GetBookByIdQuery, BookDto?>
{
    public async Task<BookDto?> HandleAsync(GetBookByIdQuery query)
    {
        var b = await ctx.Books.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id);
        return b is null ? null : new BookDto(b.Id, b.Title, b.Author, b.PublicationYear);
    }
}

public class GetAllBooksHandler(BookDbContext ctx) : IQueryHandler<GetAllBooksQuery, PaginatedResult<BookDto>>
{
    public async Task<PaginatedResult<BookDto>> HandleAsync(GetAllBooksQuery q)
    {
        var total = await ctx.Books.CountAsync();
        var skip = (q.PageIndex - 1) * q.PageSize;

        var items = await ctx.Books.AsNoTracking()
            .OrderBy(b => b.Id)
            .Skip(skip)
            .Take(q.PageSize)
            .Select(b => new BookDto(b.Id, b.Title, b.Author, b.PublicationYear))
            .ToListAsync();

        return new PaginatedResult<BookDto>(total, q.PageIndex, q.PageSize, items);
    }
}

// --- 6. App Setup ---

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQLite
builder.Services.AddDbContext<BookDbContext>(opt => opt.UseSqlite("Data Source=books.db"));

// CQRS Handlers
builder.Services.AddScoped<ICommandHandler<CreateBookCommand>, CreateBookHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateBookCommand>, UpdateBookHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteBookCommand>, DeleteBookHandler>();
builder.Services.AddScoped<IQueryHandler<GetBookByIdQuery, BookDto?>, GetBookByIdHandler>();
builder.Services.AddScoped<IQueryHandler<GetAllBooksQuery, PaginatedResult<BookDto>>, GetAllBooksHandler>();

builder.Services.AddSingleton<BookCommandValidator>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Books CQRS API (.NET 9)", Version = "v1" });
});

var app = builder.Build();

// Ensure DB exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookDbContext>();
    db.Database.Migrate();
}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- 7. Endpoints ---

var books = app.MapGroup("/books").WithTags("Books");

// POST /books
books.MapPost("/", async (CreateBookCommand cmd, BookCommandValidator v, ICommandHandler<CreateBookCommand> h) =>
{
    var check = v.Validate(cmd);
    if (!check.IsValid) return Results.BadRequest(new { check.Error });

    await h.HandleAsync(cmd);
    return Results.Created($"/books", cmd);
});

// PUT /books/{id}
books.MapPut("/{id:int}", async (int id, UpdateBookCommand body, BookCommandValidator v, ICommandHandler<UpdateBookCommand> h) =>
{
    var cmd = body with { Id = id };
    var check = v.Validate(cmd);
    if (!check.IsValid) return Results.BadRequest(new { check.Error });

    try
    {
        await h.HandleAsync(cmd);
        return Results.NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { ex.Message });
    }
});

// GET /books
books.MapGet("/", async ([FromQuery] int pageIndex, [FromQuery] int pageSize,
                         IQueryHandler<GetAllBooksQuery, PaginatedResult<BookDto>> h) =>
{
    pageIndex = pageIndex <= 0 ? 1 : pageIndex;
    pageSize = Math.Clamp(pageSize, 1, 100);

    var result = await h.HandleAsync(new GetAllBooksQuery(pageIndex, pageSize));
    return Results.Ok(result);
});

// GET /books/{id}
books.MapGet("/{id:int}", async (int id, IQueryHandler<GetBookByIdQuery, BookDto?> h) =>
{
    var b = await h.HandleAsync(new GetBookByIdQuery(id));
    return b is null ? Results.NotFound() : Results.Ok(b);
});

// DELETE /books/{id}
books.MapDelete("/{id:int}", async (int id, ICommandHandler<DeleteBookCommand> h) =>
{
    await h.HandleAsync(new DeleteBookCommand(id));
    return Results.NoContent();
});

app.Run();
