$conn = New-Object System.Data.SqlClient.SqlConnection("Server=.\SQLEXPRESS;Database=StaffAutomationDb;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;")
$conn.Open()
$cmd = $conn.CreateCommand()
$cmd.CommandText = "SELECT r.RoleName, COUNT(a.Id) as AttendanceCount FROM tbl_Attendance a JOIN tbl_Users u ON a.UserId = u.Id JOIN tbl_Roles r ON u.RoleId = r.Id GROUP BY r.RoleName;"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
    Write-Host "$($reader['RoleName']): $($reader['AttendanceCount'])"
}
$conn.Close()
