using CertifyDocx.Core;

namespace CertifyDocx.Api.Templates;

public record VariableDto(string Name, string Kind, int RowGroupId);

public record RowGroupDto(int RowGroupId, int TableIndex, int RowIndex, IReadOnlyList<string> Variables);

public record TemplateSchemaDto(IReadOnlyList<VariableDto> Variables, IReadOnlyList<RowGroupDto> RowGroups);

public record RowGroupValuesDto(int RowGroupId, List<Dictionary<string, string>>? Rows);

public record FillRequestDto(Dictionary<string, string>? SimpleValues, List<RowGroupValuesDto>? RowValues);

public static class SchemaMapper
{
    public static TemplateSchemaDto ToSchema(TemplateInfo template)
    {
        var variables = template.Variables
            .Select(v => new VariableDto(v.Name, v.Kind == VariableKind.Simple ? "simple" : "row", v.RowGroupId))
            .ToList();
        var rowGroups = template.RowGroups
            .Select(g => new RowGroupDto(g.Id, g.TableIndex, g.RowIndex, g.Variables))
            .ToList();
        return new TemplateSchemaDto(variables, rowGroups);
    }

    public static FillData ToFillData(FillRequestDto request)
    {
        var simpleValues = request.SimpleValues ?? new Dictionary<string, string>();
        var rowValues = (request.RowValues ?? new List<RowGroupValuesDto>())
            .Select(group => new RowGroupValues(
                group.RowGroupId,
                (group.Rows ?? new List<Dictionary<string, string>>())
                    .Select(row => (IReadOnlyDictionary<string, string>)row)
                    .ToList()))
            .ToList();
        return new FillData(simpleValues, rowValues);
    }
}
