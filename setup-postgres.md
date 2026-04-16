# PostgreSQL Setup for RecipeManager

## Current Status
✅ **PostgreSQL 18 is installed and running**
- Service: `postgresql-x64-18` (Running)
- Installation: `C:\Program Files\PostgreSQL\18`
- Port: 5432 (default)

## Next Steps

### 1. Set Up Database and User

Open PowerShell **as Administrator** and run:

```powershell
# Set the PostgreSQL password you created during installation
$PGPASSWORD = "YOUR_PASSWORD_HERE"
$env:PGPASSWORD = $PGPASSWORD

# Connect to PostgreSQL and create database
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -c "CREATE DATABASE recipedb;"

# Create a dedicated user for the application (optional but recommended)
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -c "CREATE USER recipeuser WITH PASSWORD 'recipe_dev_password';"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -c "GRANT ALL PRIVILEGES ON DATABASE recipedb TO recipeuser;"
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U postgres -h localhost -c "ALTER DATABASE recipedb OWNER TO recipeuser;"

# Verify connection
& "C:\Program Files\PostgreSQL\18\bin\psql.exe" -U recipeuser -h localhost -d recipedb -c "\dt"
```

### 2. Configure Connection String

After creating the database, I'll update the AppHost configuration to use your local PostgreSQL instead of Docker containers.

## Connection String Format

```
Host=localhost;Port=5432;Database=recipedb;Username=recipeuser;Password=recipe_dev_password
```

## Troubleshooting

**If you forgot your postgres password:**
1. Locate `pg_hba.conf`: `C:\Program Files\PostgreSQL\18\data\pg_hba.conf`
2. Temporarily change `scram-sha-256` to `trust` for local connections
3. Restart PostgreSQL service: `Restart-Service postgresql-x64-18`
4. Connect without password and reset: `ALTER USER postgres PASSWORD 'new_password';`
5. Change `pg_hba.conf` back to `scram-sha-256`
6. Restart service again

**Check service status:**
```powershell
Get-Service postgresql-x64-18
```

**Start/Stop service:**
```powershell
Start-Service postgresql-x64-18
Stop-Service postgresql-x64-18
Restart-Service postgresql-x64-18
```
