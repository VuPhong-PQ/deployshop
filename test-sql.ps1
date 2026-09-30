$connStr = "Server=localhost;Database=master;Integrated Security=true;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()
    Write-Host "Connected to master!" -ForegroundColor Green
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT name FROM sys.databases"
    $reader = $cmd.ExecuteReader()
    Write-Host "Databases on local SQL Server:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host " - $($reader['name'])" -ForegroundColor White
    }
    $conn.Close()
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}
