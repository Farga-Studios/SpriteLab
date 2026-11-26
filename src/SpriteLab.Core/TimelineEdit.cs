namespace SpriteLab.Core;

public enum TimelineOperation { Move, Duplicate, Remove, Insert }

public sealed record TimelineResult(int[] Cells, int?[]? Durations, int Selected);

public static class TimelineEdit
{
    public static TimelineResult Apply(int[] cells, int?[]? durations, int position,
        TimelineOperation operation, int argument = 0)
    {
        if (cells.Length == 0 || cells.Length > 4096 || position < 0 || position >= cells.Length ||
            (durations is not null && durations.Length != cells.Length))
            throw new ArgumentException("Secuencia o posición inválida.");
        var frames = cells.Select((c, i) => (Cell: c, Time: durations?[i])).ToList();
        var item = frames[position];
        int selected;
        switch (operation)
        {
            case TimelineOperation.Move:
                if (argument < 0 || argument >= frames.Count) throw new ArgumentException("Destino inválido.");
                frames.RemoveAt(position);
                frames.Insert(argument, item);
                selected = argument;
                break;
            case TimelineOperation.Duplicate:
                if (frames.Count >= 4096) throw new ArgumentException("Límite de fotogramas alcanzado.");
                selected = position + 1;
                frames.Insert(selected, item);
                break;
            case TimelineOperation.Remove:
                if (frames.Count == 1) throw new ArgumentException("La pista debe conservar un fotograma.");
                frames.RemoveAt(position);
                selected = Math.Min(position, frames.Count - 1);
                break;
            case TimelineOperation.Insert:
                if (argument < 0 || frames.Count >= 4096) throw new ArgumentException("Celda o longitud inválida.");
                selected = position + 1;
                frames.Insert(selected, (argument, null));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
        var times = frames.Select(f => f.Time).ToArray();
        return new TimelineResult(frames.Select(f => f.Cell).ToArray(), times.Any(t => t.HasValue) ? times : null, selected);
    }
}
