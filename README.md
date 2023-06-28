# PintApple API

This is the repository that contains the API developed during Project Integration, while developing the healthcare device.

## How does the data get stored?

The `PintApiDb` class extends the `DbContext` class, a central part of the Entity Framework functioning as a bridge between our .NET API and the database, that allows us to perform Create, Read, Update, and Delete (CRUD) operations against the MariaDB database that we use an engine.

Here is a sample implementation of the use of EntityFramework in this project:

```csharp
public class PintApiDb : DbContext
{
    public PintApiDb(DbContextOptions<PintApiDb> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    // additional DbSet declarations go here...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Orders)
            .WithOne(o => o.User);

        // additional model configurations go here...
    }
}
```

In this class, `DbSet` properties represent the database tables that are mapped to the models. Each row in these tables is an instance of the corresponding model. The `OnModelCreating` method is used to define relationships between tables (in this example, a one-to-many relationship between `User` and `Order`), keys, indices, and other constraints.

To leverage the power of LINQ, you can query these tables with it, creating more readable and maintainable code. An example query might look like this:

```csharp
using (var context = new PintApiDb())
{
    var usersWithOrders = context.Users
        .Include(u => u.Orders)
        .Where(u => u.Orders.Count > 0)
        .ToList();
}
```

In this query, we're fetching all users who have at least one order. The `Include` method is used to perform eager loading, which means that related data is loaded from the database as part of the initial query.

With Entity Framework handling all these details, we focus on the API functionalities and ensure that our API has a reliable abstraction to our data layer.
