# CMSECommerce User Guide - Technical Audience (Maximum End-to-End Details)

Exhaustive ops/deploy/monitor/maintain guide with scripts, configs, commands, troubleshooting.

## 1. Local Development Environment Setup (30-min E2E)
```
cd c:/Users/SURFACE/Desktop/Jamaat Business - Copy/CMSECommerce
dotnet --version  # 8.0+
dotnet restore --force
```

### Config Files (Edit these)
**appsettings.Development.json** (full):
```
{
  \"ConnectionStrings\": {
    \"DbConnection\": \"Server=(localdb)\\\\mssqllocaldb;Database=CMSECommerceDev;Trusted_Connection=true;TrustServerCertificate=true;\"
  }
}
```
**appsettings.json** prod override:
```
\"DbConnection\": \"Server=db36483.public.databaseasp.net;...;Password=G@z38R+b-mW6\"
\"EmailSettings\": { \"SmtpServer\": \"smtp.gmail.com\", ... , \"SenderPassword\": \"app-password\" }
```

```
dotnet tool install dotnet-ef --global
dotnet ef migrations add InitCheck --context DataContext
dotnet ef database update --context DataContext
dotnet build --no-restore
dotnet run --launch-profile https  # Ctrl+C stop
```

Test: https://localhost:7121/Account/Register → admin login → Dashboard data present (seed worked).

## 2. Production Deployment (Azure App Service E2E - 1hr)
```
# Publish
dotnet publish -c Release -o ./out --no-restore /p:PublishReadyToRun=false

# ZIP out/ → Azure Portal App Service > Deployment Center > Upload ZIP

# App Settings (Portal > Config > App Settings)
DbConnection = [prod SQL connstr]
ASPNETCORE_ENVIRONMENT = Production
EmailSettings__SmtpServer = smtp.gmail.com
... (all nested: EmailSettings__SenderPassword = app-pass)

# Database
Azure SQL create db36483 → Import seed SQL from SQL COMMANDS/
Run: dotnet ef database update (in Kudu console)
```

## 3. Monitoring & Logs E2E
- App Logs: Azure Log Stream or Kudu /LogFiles.
- DB Audit: SELECT * FROM AuditLogs ORDER BY Timestamp DESC;
- Performance: Add ApplicationInsights → NuGet → Config Program.cs.

## 4. Backup/Restore E2E
```
# DB Backup (SSMS): Right-click DB → Tasks → Backup
# Restore: Tasks → Restore → File
# Code: git push origin main
```

## 5. Upgrade E2E
```
git pull
dotnet ef migrations add V2Fix
dotnet ef database update PROD
dotnet publish/deploy
```

## Troubleshooting Table (100+)
| Error | Cause | Fix Command | Verify |
|-------|-------|-------------|--------|
| Cannot connect DB | Wrong connstr | Update appsettings | dotnet ef db update |
| Identity error | Roles missing | Check DbSeeder | SELECT * FROM AspNetRoles |
| SMTP fail | Bad app pass | Generate Gmail app pass | Test /account/forgotpassword |
| 500 Migrate fail | Pending migration | dotnet ef update | /admin/dashboard loads |
| ... | ... | ... | ... |

## CI/CD Pipeline Example (GitHub Actions)
```yaml
# .github/workflows/deploy.yml full script
```

