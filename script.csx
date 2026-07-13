#r "nuget: Microsoft.Data.SqlClient, 5.1.1"

using System;
using Microsoft.Data.SqlClient;

string connStr = "Server=localhost\\SQLEXPRESS;Database=QuanLyNhanSu;Trusted_Connection=True;TrustServerCertificate=True;";
using (var conn = new SqlConnection(connStr))
{
    conn.Open();

    using (var cmd = new SqlCommand("SELECT [Key], [Role], [Status] FROM UserKeys", conn))
    {
        using (var reader = cmd.ExecuteReader())
        {
            Console.WriteLine("--- Danh sách UserKeys ---");
            while(reader.Read()) {
                Console.WriteLine($"Key: {reader["Key"]}, Role: {reader["Role"]}, Status: {reader["Status"]}");
            }
        }
    }
}
