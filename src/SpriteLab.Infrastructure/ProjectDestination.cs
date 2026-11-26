namespace SpriteLab.Infrastructure;

public static class ProjectDestination
{
    public static string ValidateName(string value)
    {
        var name = value.Trim();
        if (name.Length is < 1 or > 100 || name is "." or ".." ||
            name.EndsWith('.') || name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c)) ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Introduce un nombre de proyecto válido (sin separadores ni caracteres reservados).");
        var stem = name.Split('.')[0].TrimEnd().ToUpperInvariant();
        if (stem is "CON" or "PRN" or "AUX" or "NUL" ||
            (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT")) && stem[3] is >= '1' and <= '9'))
            throw new ArgumentException("Ese nombre está reservado por Windows.");
        return name;
    }

    public static string Resolve(string parent, string name)
    {
        if (string.IsNullOrWhiteSpace(parent) || !Path.IsPathFullyQualified(parent) || !Directory.Exists(parent))
            throw new ArgumentException("Selecciona una carpeta contenedora existente.");
        return Path.Combine(Path.GetFullPath(parent), ValidateName(name));
    }
}
