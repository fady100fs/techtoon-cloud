using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Techtoon.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly string _connStr;

        public InvoicesController(IConfiguration configuration)
        {
            _connStr = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoices([FromQuery] int limit = 50)
        {
            var list = new List<object>();
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new NpgsqlCommand(
                "SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount, i.status, " +
                "COALESCE(p.name, '') AS party_name " +
                "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " +
                "ORDER BY i.date DESC LIMIT @limit", conn);
            cmd.Parameters.AddWithValue("limit", limit);

            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                list.Add(new
                {
                    id = r.GetInt32(0),
                    invoiceNumber = r.GetString(1),
                    date = r.GetDateTime(2),
                    type = r.GetString(3),
                    total = r.GetDouble(4),
                    paidAmount = r.GetDouble(5),
                    status = r.GetString(6),
                    partyName = r.GetString(7)
                });
            }
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            // Header
            object header = null;
            using (var cmd = new NpgsqlCommand(
                "SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount, i.status, " +
                "COALESCE(p.name, '') AS party_name " +
                "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id WHERE i.id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", id);
                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    header = new
                    {
                        id = r.GetInt32(0),
                        invoiceNumber = r.GetString(1),
                        date = r.GetDateTime(2),
                        type = r.GetString(3),
                        total = r.GetDouble(4),
                        paidAmount = r.GetDouble(5),
                        status = r.GetString(6),
                        partyName = r.GetString(7)
                    };
                }
            }

            if (header == null) return NotFound();

            // Lines
            var lines = new List<object>();
            using (var cmd = new NpgsqlCommand(
                "SELECT il.item_id, i.name, il.quantity, il.price, il.total " +
                "FROM invoice_lines il JOIN items i ON il.item_id = i.id WHERE il.invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", id);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    lines.Add(new
                    {
                        itemId = r.GetInt32(0),
                        itemName = r.GetString(1),
                        quantity = r.GetDouble(2),
                        price = r.GetDouble(3),
                        total = r.GetDouble(4)
                    });
                }
            }

            return Ok(new { header, lines });
        }
    }
}