namespace ErpMetronic.Domain.Entities;

/// <summary>Hak akses menu untuk sebuah divisi (join MenuItem ↔ Division).</summary>
public class MenuItemDivision
{
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public int DivisionId { get; set; }
    public Division? Division { get; set; }
}

/// <summary>Hak akses menu untuk sebuah posisi (join MenuItem ↔ Position).</summary>
public class MenuItemPosition
{
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public int PositionId { get; set; }
    public Position? Position { get; set; }
}
