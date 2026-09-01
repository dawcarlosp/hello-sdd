using System.Text.Json;
using CertifyDocx.Api.Data;
using CertifyDocx.Core;
using Microsoft.EntityFrameworkCore;

namespace CertifyDocx.Api.Templates;

public static class TemplatesEndpoints
{
    private const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const int MaxFieldValueLength = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/templates", UploadTemplate);
        app.MapGet("/api/templates", ListTemplates);
        app.MapGet("/api/templates/{id:int}", GetTemplate);
        app.MapPost("/api/templates/{id:int}/document", GenerateDocument);
    }

    private static async Task<IResult> UploadTemplate(HttpRequest request, AppDbContext db)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "La subida debe ser multipart/form-data." });
        }

        var form = await request.ReadFormAsync();
        var name = form["name"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Results.BadRequest(new { error = "El nombre de la plantilla es obligatorio." });
        }

        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "Falta el archivo de la plantilla." });
        }

        if (file.Length > Limits.MaxTemplateBytes)
        {
            return Results.Json(new { error = "El archivo supera el máximo de 10 MB." }, statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        byte[] fileBytes;
        await using (var stream = file.OpenReadStream())
        using (var buffer = new MemoryStream())
        {
            await stream.CopyToAsync(buffer);
            fileBytes = buffer.ToArray();
        }

        if (await db.Templates.AnyAsync(t => t.Name == name))
        {
            return Results.Conflict(new { error = "Ya existe una plantilla con ese nombre." });
        }

        var analysis = TemplateAnalyzer.Analyze(fileBytes);
        if (!analysis.Success)
        {
            return Results.BadRequest(new { error = string.Join(" ", analysis.Errors) });
        }

        var schema = SchemaMapper.ToSchema(analysis.Template);
        var template = new Template
        {
            Name = name,
            FileBytes = fileBytes,
            SchemaJson = JsonSerializer.Serialize(schema, JsonOptions),
            CreatedAt = DateTime.UtcNow
        };
        db.Templates.Add(template);
        await db.SaveChangesAsync();

        return Results.Created($"/api/templates/{template.Id}", new
        {
            templateId = template.Id,
            name = template.Name,
            schema,
            warnings = analysis.Template.Warnings
        });
    }

    private static async Task<IResult> ListTemplates(AppDbContext db)
    {
        var templates = await db.Templates
            .OrderBy(t => t.CreatedAt)
            .Select(t => new { t.Id, t.Name, t.SchemaJson, t.CreatedAt })
            .ToListAsync();

        var items = templates.Select(t =>
        {
            var schema = JsonSerializer.Deserialize<TemplateSchemaDto>(t.SchemaJson, JsonOptions);
            return new
            {
                templateId = t.Id,
                name = t.Name,
                variableCount = schema?.Variables.Count ?? 0,
                createdAt = t.CreatedAt
            };
        });

        return Results.Ok(items);
    }

    private static async Task<IResult> GetTemplate(int id, AppDbContext db)
    {
        var template = await db.Templates.FindAsync(id);
        if (template is null)
        {
            return Results.NotFound(new { error = "La plantilla no existe." });
        }

        var schema = JsonSerializer.Deserialize<TemplateSchemaDto>(template.SchemaJson, JsonOptions);
        return Results.Ok(new
        {
            templateId = template.Id,
            name = template.Name,
            schema
        });
    }

    private static async Task<IResult> GenerateDocument(int id, FillRequestDto? request, AppDbContext db)
    {
        var template = await db.Templates.FindAsync(id);
        if (template is null)
        {
            return Results.NotFound(new { error = "La plantilla no existe." });
        }

        if (request is null)
        {
            return Results.BadRequest(new { error = "Faltan los datos del formulario." });
        }

        var tooLong = FindFirstTooLongValue(request);
        if (tooLong is not null)
        {
            return Results.BadRequest(new
            {
                error = $"El valor de la variable «{tooLong}» supera los {MaxFieldValueLength} caracteres."
            });
        }

        var result = DocumentFiller.Fill(template.FileBytes, SchemaMapper.ToFillData(request));
        if (!result.Success)
        {
            return Results.BadRequest(new { error = string.Join(" ", result.Errors) });
        }

        var downloadName = $"{SanitizeFileName(template.Name)}.docx";
        return Results.File(result.Document, DocxContentType, downloadName);
    }

    private static string? FindFirstTooLongValue(FillRequestDto request)
    {
        if (request.SimpleValues is not null)
        {
            foreach (var pair in request.SimpleValues)
            {
                if (pair.Value is not null && pair.Value.Length > MaxFieldValueLength)
                {
                    return pair.Key;
                }
            }
        }

        if (request.RowValues is not null)
        {
            foreach (var group in request.RowValues)
            {
                if (group.Rows is null)
                {
                    continue;
                }
                foreach (var row in group.Rows)
                {
                    foreach (var pair in row)
                    {
                        if (pair.Value is not null && pair.Value.Length > MaxFieldValueLength)
                        {
                            return pair.Key;
                        }
                    }
                }
            }
        }

        return null;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
        return sanitized.Length == 0 ? "documento" : sanitized;
    }
}
