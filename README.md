# EntityFrameworkCore.UseRowNumberForPaging

[![NuGet][main-nuget-badge]][main-nuget]

[main-nuget]: https://www.nuget.org/packages/EntityFrameworkCore.UseRowNumberForPaging/
[main-nuget-badge]: https://img.shields.io/nuget/v/EntityFrameworkCore.UseRowNumberForPaging.svg?style=flat-square&label=nuget

Bring back support for [UseRowNumberForPaging](https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.infrastructure.sqlserverdbcontextoptionsbuilder.userownumberforpaging?view=efcore-3.0) in EntityFrameworkCore 9.0/8.0

If you are using EntityFrameworkCore 5.0 please use version 0.2.  
If you are using EntityFrameworkCore 7.0 please use version 0.5.  
If you are using EntityFrameworkCore 6.0 please use version 0.6.  

# Usage

The same as original UseRowNumberForPaging method
``` csharp
optionsBuilder.UseSqlServer("connection string", i => i.UseRowNumberForPaging());
```

# Contributor

* [@Megasware128](https://github.com/Megasware128)
* [Simon Foster (@funkysi1701)](https://github.com/funkysi1701)
* [Clemens Lieb (@Vogel612)](https://github.com/Vogel612)
