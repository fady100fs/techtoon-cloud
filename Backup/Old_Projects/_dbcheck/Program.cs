using System;
using Npgsql;
class Program
{
    static void Main()
    {
        var connStr = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();
        Console.WriteLine("=== cash_boxes columns ===");
        using (var cmd = new NpgsqlCommand("SELECT column_name, data_type FROM information_schema.columns WHERE table_name='cash_boxes' ORDER BY ordinal_position", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                Console.WriteLine($"  {r.GetString(0),-25} | {r.GetString(1)}");
    }
}
