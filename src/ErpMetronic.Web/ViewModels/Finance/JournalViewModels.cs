using System.ComponentModel.DataAnnotations;

namespace ErpMetronic.Web.ViewModels;

public class JournalEntryCreateViewModel
{
    [Required, DataType(DataType.Date), Display(Name = "Tanggal")]
    public DateTime EntryDate { get; set; } = DateTime.Today;

    [Display(Name = "Keterangan"), StringLength(250)]
    public string? Description { get; set; }

    public List<JournalLineInput> Lines { get; set; } = new();
}

public class JournalLineInput
{
    public int AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}
