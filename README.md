# EntityFrameworkCore.UseRowNumberForPaging

Bring back support for [UseRowNumberForPaging](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.infrastructure.sqlserverdbcontextoptionsbuilder.userownumberforpaging?view=efcore-3.0) in EntityFrameworkCore 5.0

# Usage

The same as original UseRowNumberForPaging method
``` csharp
optionsBuilder.UseSqlServer("connection string", i => i.UseRowNumberForPaging());
```
