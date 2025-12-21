# TODO: Switch from SQLite to SQL Server

- [ ] Update `appsettings.json` to use SQL Server connection string from `_SQLServerAppSettings.json`
- [ ] Update `Program.cs` to use `UseSqlServer` instead of `UseSqlite`
- [ ] Update `CMSECommerce.csproj` to replace `Microsoft.EntityFrameworkCore.Sqlite` with `Microsoft.EntityFrameworkCore.SqlServer`
- [ ] Update `Infrastructure/DataContextFactory.cs` to use `UseSqlServer`
- [ ] Remove existing SQLite migrations
- [ ] Create new migrations for SQL Server
- [ ] Install dependencies if needed
- [ ] Run `dotnet ef database update`
- [ ] Test the application
