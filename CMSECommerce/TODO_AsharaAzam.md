# Ashara Azam Implementation TODO

## Steps:
1. ✅ Create this TODO file
2. ✅ Install SkiaSharp NuGet packages (SkiaSharp 3.119.2 added)
3. ✅ Create Models/AsharaAzamEntry.cs model
4. ✅ Update Infrastructure/DataContext.cs (added DbSet<AsharaAzamEntries>, OnModelCreating config with ITSNumber PK)
5. ✅ Generate EF migration `dotnet ef migrations add AddAsharaAzamEntry`
6. ✅ Update database `dotnet ef database update`
7. ✅ Create Areas/AsharaAzam/Controllers/AzamController.cs
8. ✅ Create Areas/AsharaAzam/Views/Shared/_ViewImports.cshtml
9. ✅ Create Areas/AsharaAzam/Views/Shared/_ViewStart.cshtml (using shared layout)
10. ✅ Create Areas/AsharaAzam/Views/Azam/Index.cshtml (professional Bootstrap form with validation)
11. ✅ Create wwwroot/css/ashara-azam.css (gold theme, animations)
12. ✅ Implement certificate JPEG generation in AzamController (SkiaSharp gold certificate)
13. ✅ Test form, duplicate ITS, cert download at /AsharaAzam/Azam (HTTP 400 fixed)
14. ✅ Update TODO as completed

**Notes:**
- Keep AsharaAzam area as-is
- New table AsharaAzamEntry with unique ITSNumber
- Professional Bootstrap 5 UI
- Cert: Gold theme, centered text, download JPEG

