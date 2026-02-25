Write-Host "Cleaning MedReminder.Api..."
dotnet clean .\MedReminder.Api\MedReminder.Api.csproj

Write-Host "Starting API..."
Start-Process "http://localhost:5001/swagger"

dotnet run --project .\MedReminder.Api\MedReminder.Api.csproj