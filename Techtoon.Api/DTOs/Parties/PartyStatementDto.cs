namespace Techtoon.Api.DTOs.Parties;

public class PartyStatementDto
{
    public int PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public List<StatementEntryDto> Entries { get; set; } = new();
}

public class StatementEntryDto
{
    public DateTime Date { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Notes { get; set; }
}