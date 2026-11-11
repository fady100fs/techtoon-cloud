namespace Techtoon.UI.Views;

public class ComboItem
{
    public int Id { get; set; }
    public string Name { get; set; }
    public override string ToString() => Name ?? "";
}
