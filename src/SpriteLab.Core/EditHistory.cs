namespace SpriteLab.Core;

// Bounded history of immutable snapshots. A new edit invalidates the redo branch.
public sealed class EditHistory<T>(int capacity = 64)
{
    private readonly List<(T State, string Label)> _undo = new();
    private readonly List<(T State, string Label)> _redo = new();
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public string? UndoLabel => CanUndo ? _undo[^1].Label : null;
    public string? RedoLabel => CanRedo ? _redo[^1].Label : null;
    public void Record(T before, string label)
    {
        _undo.Add((before, label));
        if (_undo.Count > Math.Max(1, capacity)) _undo.RemoveAt(0);
        _redo.Clear();
    }
    public T Undo(T current)
    {
        if (!CanUndo) throw new InvalidOperationException("Nothing to undo.");
        var item = _undo[^1]; _undo.RemoveAt(_undo.Count - 1);
        _redo.Add((current, item.Label));
        return item.State;
    }
    public T Redo(T current)
    {
        if (!CanRedo) throw new InvalidOperationException("Nothing to redo.");
        var item = _redo[^1]; _redo.RemoveAt(_redo.Count - 1);
        _undo.Add((current, item.Label));
        return item.State;
    }
    public void Clear() { _undo.Clear(); _redo.Clear(); }
}
