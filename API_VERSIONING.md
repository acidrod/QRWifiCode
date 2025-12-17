# Estrategia de Versionado de API

## Resumen

La API de QRWifiCode utiliza versionado basado en URL para mantener compatibilidad con versiones anteriores y facilitar la evolución de la API.

## Versión Actual

**v1** - Versión inicial de la API

## Formato de URL

```text
https://localhost:7044/v{version}/{endpoint}
```

### Ejemplos

- `GET /v1/wifi` - Obtener todas las redes WiFi
- `GET /v1/wifi/{id}` - Obtener una red WiFi específica
- `POST /v1/wifi` - Crear una nueva red WiFi
- `PUT /v1/wifi/{id}` - Actualizar una red WiFi existente
- `DELETE /v1/wifi/{id}` - Eliminar una red WiFi

## Configuración

### Backend (QRCode)

La API utiliza los paquetes `Asp.Versioning.Http` v8.1.0 y `Asp.Versioning.Mvc.ApiExplorer` v8.1.0 con la siguiente configuración:

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

**Características:**

- **DefaultApiVersion**: Si no se especifica versión, se usa v1.0
- **AssumeDefaultVersionWhenUnspecified**: Permite llamadas sin versión (se usa la default)
- **ReportApiVersions**: Incluye headers con versiones soportadas en las respuestas
- **AddApiExplorer**: Integra el versionado con Swagger/OpenAPI para documentación automática
- **GroupNameFormat**: Define el formato del nombre del grupo como 'v' seguido de la versión (ej: 'v1')
- **SubstituteApiVersionInUrl**: Permite sustituir la versión en las URLs automáticamente

### Frontend (FrontEnd)

La configuración de la versión de API se encuentra en `appsettings.json`:

```json
{
  "BackendApi": {
    "BaseUrl": "https://localhost:7044",
    "Version": "v1"
  }
}
```

## Estrategia de Evolución

### Cambios No-Breaking (Patch/Minor)

Los siguientes cambios NO requieren una nueva versión:

- ✅ Agregar nuevos endpoints
- ✅ Agregar campos opcionales a responses
- ✅ Agregar parámetros opcionales a requests
- ✅ Corregir bugs
- ✅ Mejorar performance

### Cambios Breaking (Major)

Los siguientes cambios REQUIEREN una nueva versión:

- ❌ Eliminar endpoints
- ❌ Renombrar campos
- ❌ Cambiar tipos de datos
- ❌ Hacer campos requeridos que antes eran opcionales
- ❌ Cambiar comportamiento de endpoints existentes

## Proceso para Nueva Versión

### 1. Crear Nueva Versión en Backend

```csharp
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))  // Nueva versión
    .ReportApiVersions()
    .Build();

var v2 = app.MapGroup("/v2").WithApiVersionSet(apiVersionSet);
v2.MapGet("/wifi", () => { /* nuevo comportamiento */ });
```

### 2. Actualizar Frontend (Opcional)

Modificar `appsettings.json`:

```json
{
  "BackendApi": {
    "Version": "v2"
  }
}
```

### 3. Mantener Versión Anterior

- Las versiones anteriores deben mantenerse durante un período de deprecación
- Documentar cambios breaking en `CHANGELOG.md`
- Notificar a usuarios con al menos 3 meses de anticipación

## Deprecación de Versiones

### Ciclo de Vida

1. **Activa**: Versión actual, totalmente soportada
2. **Mantenimiento**: Versión anterior, solo corrección de bugs críticos
3. **Deprecada**: Se anuncia EOL (End of Life) con 3+ meses de anticipación
4. **Retirada**: Versión eliminada del código

### Versiones Actuales

| Versión | Estado       | Fecha Release | EOL Planificado |
|---------|-------------|---------------|-----------------|
| v1      | Activa      | 2025-12-17    | TBD             |

## Swagger/OpenAPI

La documentación de Swagger está configurada para mostrar todas las versiones:

- URL: `https://localhost:7044/swagger`
- Cada versión tiene su propia documentación
- Los tags organizan los endpoints por funcionalidad (WiFi)

## Testing

Al crear una nueva versión:

1. Mantener tests para versión anterior
2. Crear tests para nueva versión
3. Verificar que ambas versiones funcionan correctamente
4. Probar migración de frontend de v1 a v2

## Referencias

- [ASP.NET Core API Versioning](https://github.com/dotnet/aspnet-api-versioning)
- [Semantic Versioning](https://semver.org/)
- [API Versioning Best Practices](https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design#versioning-a-restful-web-api)
