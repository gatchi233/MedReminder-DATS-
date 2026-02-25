Write-Host "======================================="
Write-Host "Starting MedReminder Full Demo Mode"
Write-Host "======================================="

# Clean API
Write-Host "`nCleaning API..."
dotnet clean .\MedReminder.Api\MedReminder.Api.csproj

# Start API in new window
Write-Host "Starting API..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project .\MedReminder.Api\MedReminder.Api.csproj"

# Small delay to allow API to start
Start-Sleep -Seconds 3

# Optional: open Swagger
Start-Process "http://localhost:5001/swagger"

# Start Desktop app in new window
Write-Host "Starting Desktop App..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project .\MedReminder.Desktop\MedReminder.Desktop.csproj"

Write-Host "`nAll services launched."