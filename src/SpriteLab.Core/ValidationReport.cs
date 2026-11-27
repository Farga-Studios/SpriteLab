using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpriteLab.Core;

public sealed record ReportFinding(string Code, int? Cell, int? ReferenceCell, string Message, CellEdges Edges);

public sealed record ValidationReport(int FormatVersion, string AppVersion, DateTimeOffset AnalyzedAtUtc,
    string Project, string Resource, int ImageWidth, int ImageHeight, int CellWidth, int CellHeight,
    int Columns, int Rows, int TotalCells, int EmptyCells, int EdgeCells, int DuplicateCells,
    int AlignmentFindings, int AnimationFindings, int OmittedFindings, IReadOnlyList<ReportFinding> Findings)
{
    public int ListedFindings => Findings.Count;
    public int TotalFindings => ListedFindings + OmittedFindings;
    public bool IsTruncated => OmittedFindings > 0;

    public static ValidationReport Create(string appVersion, DateTimeOffset analyzedAt, string project, string resource,
        CellGrid grid, SheetValidationResult sheet, int alignment, int animation, int omitted,
        IEnumerable<SheetFinding> findings) =>
        new(1, appVersion, analyzedAt.ToUniversalTime(), project, resource, grid.Image.Width, grid.Image.Height,
            grid.Cell.Width, grid.Cell.Height, grid.Columns, grid.Rows, sheet.TotalCells, sheet.EmptyCells,
            sheet.EdgeCells, sheet.DuplicateCells, alignment, animation, omitted,
            Array.AsReadOnly(findings.Where(f => f.Code != "LIMIT").Select(f =>
                new ReportFinding(f.Code, f.Cell is int c ? c + 1 : null,
                    f.ReferenceCell is int reference ? reference + 1 : null, f.Message, f.Edges)).ToArray()));

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    });

    public string ToPlainText(bool english = false)
    {
        var text = new StringBuilder();
        text.AppendLine((english?"SPRITELAB — VALIDATION REPORT":"SPRITELAB — INFORME DE VALIDACIÓN"));
        text.AppendLine((english?$"Version: {AppVersion} · Report format: {FormatVersion}":$"Versión: {AppVersion} · Formato de informe: {FormatVersion}"));
        text.AppendLine((english?$"Analysis date (UTC): {AnalyzedAtUtc:O}":$"Fecha del análisis (UTC): {AnalyzedAtUtc:O}"));
        text.AppendLine((english?$"Project: {Project}":$"Proyecto: {Project}"));
        text.AppendLine((english?$"Resource: {Resource}":$"Recurso: {Resource}"));
        text.AppendLine((english?$"Image: {ImageWidth} × {ImageHeight} px":$"Imagen: {ImageWidth} × {ImageHeight} px"));
        text.AppendLine((english?$"Grid: {Columns} × {Rows} · Cell: {CellWidth} × {CellHeight} px":$"Cuadrícula: {Columns} × {Rows} · Celda: {CellWidth} × {CellHeight} px"));
        text.AppendLine();
        text.AppendLine((english?$"Cells analyzed: {TotalCells}":$"Celdas analizadas: {TotalCells}"));
        text.AppendLine((english?$"Empty: {EmptyCells} · Edge: {EdgeCells} · Duplicates: {DuplicateCells}":$"Vacías: {EmptyCells} · Con borde: {EdgeCells} · Duplicadas: {DuplicateCells}"));
        text.AppendLine((english?$"Size/alignment findings: {AlignmentFindings} · Animations: {AnimationFindings}":$"Avisos de tamaño/alineación: {AlignmentFindings} · Animaciones: {AnimationFindings}"));
        text.AppendLine((english?$"Total findings: {TotalFindings} · Listed: {ListedFindings} · Omitted: {OmittedFindings}":$"Avisos totales: {TotalFindings} · Listados: {ListedFindings} · Omitidos: {OmittedFindings}"));
        text.AppendLine((english?"Categories may overlap. Findings require manual review.":"Las categorías pueden solaparse. Los avisos requieren revisión manual."));
        text.AppendLine((english?"Cell numbering starts at 1. Active sheet report, without filters.":"Numeración de celdas desde 1. Informe de la hoja activa, sin aplicar filtros."));
        if (IsTruncated) text.AppendLine((english?"WARNING: list capped; counts are complete.":"ATENCIÓN: lista limitada; los recuentos son completos."));
        text.AppendLine();
        foreach (var finding in Findings)
        {
            var cell = finding.Cell is int c ? (english?$"Cell {c} · ":$"Celda {c} · ") : "";
            var reference = finding.ReferenceCell is int r ? (english?$" · Reference: cell {r}":$" · Referencia: celda {r}") : "";
            text.AppendLine($"[{finding.Code}] {cell}{finding.Message}{reference}");
        }
        if (TotalFindings == 0) text.AppendLine((english?"No findings were detected in the checks performed.":"No se detectaron avisos en las comprobaciones ejecutadas."));
        return text.ToString();
    }
}
