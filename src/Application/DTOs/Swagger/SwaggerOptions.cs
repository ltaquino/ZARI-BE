namespace ZARI.Application.DTOs.Swagger;

public class SwaggerOptions
{
    public SwaggerContact Contact { get; set; } = new SwaggerContact();
    public SwaggerDocument Document { get; set; } = new SwaggerDocument();
    public SwaggerLicense License { get; set; } = new SwaggerLicense();
}

