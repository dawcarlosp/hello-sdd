namespace CertifyDocx.Api.Data;

public class Template
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string SchemaJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
