using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QuanLyNhanSu.EntityFrameworkCore;

var optionsBuilder = new DbContextOptionsBuilder<QuanLyNhanSuDbContext>();
optionsBuilder.UseSqlServer("Server=localhost;Database=QuanLyNhanSu;Trusted_Connection=True;TrustServerCertificate=True");
using var db = new QuanLyNhanSuDbContext(optionsBuilder.Options);
var keys = db.UserKeys.ToList();
foreach(var k in keys) {
    Console.WriteLine($"Key: {k.Key}, Role: {k.Role}, Status: {k.Status}");
}
