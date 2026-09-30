$connStr = "Data Source=W-BWP-IT-ITM\SQLEXPRESS;Initial Catalog=RetailPoint;Integrated Security=True;TrustServerCertificate=True;"
Write-Host "Connecting to RetailPoint database on W-BWP-IT-ITM\SQLEXPRESS..." -ForegroundColor Yellow

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()
    Write-Host "Connected to RetailPoint database!" -ForegroundColor Green

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME"
    $reader = $cmd.ExecuteReader()
    $tables = @()
    while ($reader.Read()) {
        $tables += $reader["TABLE_NAME"]
    }
    $reader.Close()

    Write-Host "Found tables in RetailPoint:" -ForegroundColor Cyan
    foreach ($tbl in $tables) {
        $countCmd = $conn.CreateCommand()
        $countCmd.CommandText = "SELECT COUNT(*) FROM [$tbl]"
        $rowCount = $countCmd.ExecuteScalar()
        Write-Host " - $tbl : $rowCount rows" -ForegroundColor White
    }
} catch {
    Write-Host "Error connecting" -ForegroundColor Red
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
