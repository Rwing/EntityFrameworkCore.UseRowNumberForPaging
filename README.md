# EntityFrameworkCore.UseRowNumberForPaging

[![NuGet][main-nuget-badge]][main-nuget]

[main-nuget]: https://www.nuget.org/packages/EntityFrameworkCore.UseRowNumberForPaging/
[main-nuget-badge]: https://img.shields.io/nuget/v/EntityFrameworkCore.UseRowNumberForPaging.svg?style=flat-square&label=nuget

Bring back support for [UseRowNumberForPaging](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.infrastructure.sqlserverdbcontextoptionsbuilder.userownumberforpaging?view=efcore-3.0) in EntityFrameworkCore 5.0

# Usage

The same as original UseRowNumberForPaging method
``` csharp
optionsBuilder.UseSqlServer("connection string", i => i.UseRowNumberForPaging());
```
