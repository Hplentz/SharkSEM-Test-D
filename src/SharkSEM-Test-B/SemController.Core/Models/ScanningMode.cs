namespace SemController.Core.Models;

public record ScanningMode(int Index, string Name)
{
    public override string ToString() => $"[{Index}] {Name}";
}
