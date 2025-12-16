# Issues para Crear - Mejoras del Proyecto QRWifiCode

Este documento contiene todos los issues identificados en el análisis del proyecto, organizados por prioridad y categoría. Copia cada sección para crear un issue individual en GitHub.

---

## 🔴 P0: Seguridad y Estabilidad Crítica

### Issue 1: Corregir warnings de nullable reference
**Prioridad:** P0 - Alta  
**Categoría:** Seguridad / Calidad de Código  
**Labels:** `bug`, `security`, `good first issue`

**Descripción:**
El proyecto tiene 5 warnings del compilador relacionados con posibles referencias nulas que pueden causar NullReferenceException en tiempo de ejecución.

**Warnings identificados:**
- `CS8603` en `BackgroundMailQueue.cs:18` - Posible retorno de null
- `CS8604` en `EmailService.cs:36` - Argumento potencialmente null para `int.Parse()`
- `CS8604` en `Program.cs:181` - Argumento potencialmente null para `Convert.FromBase64String()`
- `CS8604` en `Program.cs:202` - Argumento potencialmente null para `Convert.FromBase64String()`

**Archivos afectados:**
- `QRCode/Services/BackgroundMailQueue.cs`
- `QRCode/Services/EmailService.cs`
- `QRCode/Program.cs`

**Tareas:**
- [ ] Añadir null checks apropiados en `BackgroundMailQueue.Dequeue()`
- [ ] Validar configuración SMTP antes de parsear en `EmailService`
- [ ] Validar `EncryptionKey` en `Program.cs` al inicio
- [ ] Considerar usar `TryParse` en lugar de `Parse` donde sea apropiado
- [ ] Compilar sin warnings

**Impacto:** Alto - Previene crashes en producción

---

### Issue 2: Eliminar URLs hardcoded del frontend
**Prioridad:** P0 - Alta  
**Categoría:** Configuración / Seguridad  
**Labels:** `enhancement`, `configuration`, `security`

**Descripción:**
La URL del backend (`https://localhost:7044`) está hardcoded en múltiples archivos del frontend, haciendo imposible el despliegue en diferentes entornos sin modificar código.

**Archivos afectados:**
- `FrontEnd/Controllers/HomeController.cs` (líneas 26, 39, 50, 57, 71)
- `FrontEnd/Views/Home/WiFi.cshtml` (línea 71)
- `FrontEnd/Views/Home/WifiForm.cshtml` (línea 57)

**Tareas:**
- [ ] Añadir `BackendApiUrl` a `appsettings.json` y `appsettings.Development.json`
- [ ] Inyectar `IConfiguration` donde sea necesario
- [ ] Reemplazar todas las instancias de URL hardcoded
- [ ] Documentar configuración en README

**Ejemplo de configuración:**
```json
{
  "BackendApi": {
    "BaseUrl": "https://localhost:7044"
  }
}
```

**Impacto:** Alto - Permite despliegue en múltiples entornos

---

### Issue 3: Implementar IHttpClientFactory en HomeController
**Prioridad:** P0 - Alta  
**Categoría:** Performance / Best Practices  
**Labels:** `bug`, `performance`, `technical-debt`

**Descripción:**
`HomeController` crea instancias de `HttpClient` directamente con `new HttpClient()`, lo cual es un antipatrón conocido que puede causar agotamiento de sockets y problemas de performance.

**Problema:**
```csharp
HttpClient httpClient = new();  // ❌ Antipatrón
```

**Archivos afectados:**
- `FrontEnd/Controllers/HomeController.cs`

**Tareas:**
- [ ] Registrar `IHttpClientFactory` en `Program.cs` del FrontEnd
- [ ] Crear un named client para el backend API
- [ ] Inyectar `IHttpClientFactory` en `HomeController`
- [ ] Reemplazar todas las instancias de `new HttpClient()`

**Solución propuesta:**
```csharp
// Program.cs
builder.Services.AddHttpClient("BackendApi", client => {
    client.BaseAddress = new Uri(builder.Configuration["BackendApi:BaseUrl"]);
});

// HomeController
private readonly IHttpClientFactory _httpClientFactory;
public HomeController(IHttpClientFactory httpClientFactory) {
    _httpClientFactory = httpClientFactory;
}
```

**Impacto:** Alto - Previene problemas de performance y agotamiento de recursos

---

### Issue 4: Añadir validación de entrada en modelos
**Prioridad:** P0 - Alta  
**Categoría:** Seguridad / Validación  
**Labels:** `security`, `enhancement`, `validation`

**Descripción:**
Los parámetros WiFi no tienen validación, permitiendo datos inválidos o potencialmente maliciosos.

**Archivos afectados:**
- `QRCode/Models/WifiParams.cs`
- `FrontEnd/Models/WifiViewModel.cs`

**Tareas:**
- [ ] Añadir atributos de validación a `WifiParams`:
  - `[Required]` para campos obligatorios
  - `[StringLength]` para limitar longitud
  - `[EmailAddress]` para MailTo
  - `[RegularExpression]` para Auth (solo valores válidos)
- [ ] Aplicar mismas validaciones a `WifiViewModel`
- [ ] Añadir validación en endpoints: `if (!ModelState.IsValid) return Results.BadRequest()`
- [ ] Añadir validación frontend en formularios

**Ejemplo:**
```csharp
public class WifiParams
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "SSID es requerido")]
    [StringLength(32, ErrorMessage = "SSID no puede exceder 32 caracteres")]
    public string Ssid { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password es requerida")]
    [StringLength(63, ErrorMessage = "Password no puede exceder 63 caracteres")]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [RegularExpression("^(WPA|WPA2|WEP|nopass)$")]
    public string Auth { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string MailTo { get; set; } = string.Empty;
}
```

**Impacto:** Alto - Previene inyección de datos inválidos

---

### Issue 5: Mejorar configuración de CORS
**Prioridad:** P0 - Alta  
**Categoría:** Seguridad  
**Labels:** `security`, `configuration`

**Descripción:**
La configuración actual de CORS permite cualquier origen que sea localhost, lo cual es demasiado permisivo.

**Código actual:**
```csharp
build.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
```

**Archivos afectados:**
- `QRCode/Program.cs`

**Tareas:**
- [ ] Mover orígenes permitidos a configuración
- [ ] Especificar puertos exactos en lugar de cualquier localhost
- [ ] Considerar diferentes configuraciones para Development vs Production
- [ ] Documentar configuración de CORS

**Solución propuesta:**
```csharp
// appsettings.json
{
  "AllowedOrigins": ["https://localhost:5001"]
}

// Program.cs
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
opt.AddPolicy(Policy, build => {
    build.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
});
```

**Impacto:** Alto - Reduce superficie de ataque

---

## 🟡 P1: Mantenibilidad y Arquitectura

### Issue 6: Extraer servicio IWifiRepository
**Prioridad:** P1 - Media  
**Categoría:** Arquitectura / Refactoring  
**Labels:** `enhancement`, `refactoring`, `architecture`

**Descripción:**
Toda la lógica de acceso a datos (lectura/escritura de archivos, encriptación) está mezclada con la lógica de endpoints en `Program.cs`.

**Archivos afectados:**
- `QRCode/Program.cs` (funciones: `GetAllWifi`, `GetWifiById`, `WriteWifiData`)

**Tareas:**
- [ ] Crear interfaz `IWifiRepository` en `QRCode/Services/`
- [ ] Crear implementación `FileWifiRepository`
- [ ] Mover lógica de encriptación/desencriptación
- [ ] Mover gestión de IDs
- [ ] Registrar servicio en DI
- [ ] Refactorizar endpoints para usar repositorio

**Interfaz propuesta:**
```csharp
public interface IWifiRepository
{
    Task<List<WifiParams>> GetAllAsync();
    Task<WifiParams?> GetByIdAsync(int id);
    Task<WifiParams> CreateAsync(WifiParams wifiParams);
    Task<WifiParams?> UpdateAsync(int id, WifiParams wifiParams);
    Task<bool> DeleteAsync(int id);
}
```

**Impacto:** Medio - Mejora testabilidad y separación de responsabilidades

---

### Issue 7: Extraer servicio IQrCodeGenerator
**Prioridad:** P1 - Media  
**Categoría:** Arquitectura / Code Quality  
**Labels:** `enhancement`, `refactoring`, `DRY`

**Descripción:**
El código de generación de QR está duplicado en dos lugares de `Program.cs` (función `GetBase64QrCode` y endpoint `POST /wifi/{id}`).

**Archivos afectados:**
- `QRCode/Program.cs` (líneas 103-125, 207-226)

**Tareas:**
- [ ] Crear interfaz `IQrCodeService` en `QRCode/Services/`
- [ ] Crear implementación `QrCodeService`
- [ ] Consolidar lógica duplicada
- [ ] Añadir configuración para tamaño de QR (actualmente hardcoded a 20)
- [ ] Registrar servicio en DI
- [ ] Refactorizar endpoints

**Interfaz propuesta:**
```csharp
public interface IQrCodeService
{
    string GenerateWifiQrCode(WifiParams wifiParams);
    Task<byte[]> GenerateWifiQrCodeBytesAsync(WifiParams wifiParams);
}
```

**Impacto:** Medio - Elimina duplicación, mejora mantenibilidad

---

### Issue 8: Implementar DTOs para API
**Prioridad:** P1 - Media  
**Categoría:** Arquitectura / Best Practices  
**Labels:** `enhancement`, `architecture`, `api`

**Descripción:**
Los modelos de dominio (`WifiParams`) se exponen directamente en la API sin capa de DTOs, acoplando representación interna con contratos de API.

**Tareas:**
- [ ] Crear carpeta `QRCode/DTOs/`
- [ ] Crear `CreateWifiRequest`, `UpdateWifiRequest`, `WifiResponse`
- [ ] Añadir métodos de mapeo (o usar AutoMapper)
- [ ] Actualizar endpoints para usar DTOs
- [ ] Actualizar documentación Swagger

**DTOs propuestos:**
```csharp
public record CreateWifiRequest(
    string Ssid, 
    string Password, 
    string Auth, 
    string MailTo
);

public record UpdateWifiRequest(
    string Ssid, 
    string Password, 
    string Auth, 
    string MailTo
);

public record WifiResponse(
    int Id, 
    string Ssid, 
    string Auth, 
    string MailTo
); // No exponer Password
```

**Impacto:** Medio - Mejor control de contratos API, seguridad

---

### Issue 9: Añadir logging estructurado
**Prioridad:** P1 - Media  
**Categoría:** Observabilidad  
**Labels:** `enhancement`, `observability`, `logging`

**Descripción:**
El frontend no tiene logging apropiado y el backend solo tiene logging básico en el background worker.

**Archivos afectados:**
- `FrontEnd/Controllers/HomeController.cs`
- `QRCode/Program.cs` (endpoints)
- `QRCode/Services/EmailService.cs`

**Tareas:**
- [ ] Añadir logging en `HomeController` para operaciones importantes
- [ ] Añadir logging en endpoints de API
- [ ] Añadir logging en `EmailService` (inicio/fin de envío)
- [ ] Considerar logging de métricas (tiempo de generación de QR)
- [ ] Añadir correlation IDs para rastrear requests

**Ejemplo:**
```csharp
_logger.LogInformation("Generating QR code for SSID: {Ssid}", wifiParams.Ssid);
_logger.LogError(ex, "Failed to send email to {Email}", email.To);
```

**Impacto:** Medio - Mejora debugging y observabilidad en producción

---

### Issue 10: Refactorizar lógica de Program.cs a servicios
**Prioridad:** P1 - Media  
**Categoría:** Arquitectura / Code Organization  
**Labels:** `refactoring`, `architecture`, `technical-debt`

**Descripción:**
`Program.cs` contiene ~230 líneas incluyendo lógica de negocio, configuración, y definición de rutas mezcladas.

**Archivos afectados:**
- `QRCode/Program.cs`

**Tareas:**
- [ ] Mover configuración de servicios a `Extensions/ServiceCollectionExtensions.cs`
- [ ] Mover definición de endpoints a `Endpoints/WifiEndpoints.cs`
- [ ] Dejar `Program.cs` solo con configuración de app
- [ ] Aplicar patrón de configuración modular

**Estructura propuesta:**
```
QRCode/
  ├── Extensions/
  │   └── ServiceCollectionExtensions.cs
  ├── Endpoints/
  │   └── WifiEndpoints.cs
  └── Program.cs (< 50 líneas)
```

**Impacto:** Medio - Mejora organización y legibilidad

---

## 🟢 P2: Calidad de Vida y Mejoras Incrementales

### Issue 11: Añadir infraestructura de testing
**Prioridad:** P2 - Baja  
**Categoría:** Testing / Quality Assurance  
**Labels:** `testing`, `infrastructure`, `enhancement`

**Descripción:**
El proyecto no tiene ningún test unitario o de integración.

**Tareas:**
- [ ] Crear proyecto `QRCode.Tests` con xUnit
- [ ] Crear proyecto `FrontEnd.Tests` con xUnit
- [ ] Añadir tests para `RandomIvEncryptionService`
- [ ] Añadir tests para servicios de negocio
- [ ] Configurar CI/CD para ejecutar tests
- [ ] Añadir coverage reporting

**Dependencias a añadir:**
- xUnit
- xUnit.runner.visualstudio
- Moq (para mocking)
- FluentAssertions

**Impacto:** Bajo pero importante a largo plazo - Previene regresiones

---

### Issue 12: Añadir validación frontend de formularios
**Prioridad:** P2 - Baja  
**Categoría:** UX / Validación  
**Labels:** `enhancement`, `frontend`, `ux`

**Descripción:**
Los formularios no tienen validación del lado del cliente, obligando al usuario a esperar la respuesta del servidor para ver errores.

**Archivos afectados:**
- `FrontEnd/Views/Home/WifiForm.cshtml`

**Tareas:**
- [ ] Añadir atributos de validación HTML5 (required, maxlength, type="email")
- [ ] Añadir validación JavaScript antes del fetch
- [ ] Mostrar mensajes de error amigables
- [ ] Añadir feedback visual (spinners durante operaciones)
- [ ] Usar Tag Helpers de validación de ASP.NET

**Mejoras UX adicionales:**
- [ ] Deshabilitar botón durante envío
- [ ] Mostrar toast de éxito/error
- [ ] Prevenir double-submit

**Impacto:** Bajo - Mejora experiencia de usuario

---

### Issue 13: Optimizar background worker con eventos
**Prioridad:** P2 - Baja  
**Categoría:** Performance  
**Labels:** `enhancement`, `performance`, `optimization`

**Descripción:**
El background worker usa polling con delay fijo de 1 segundo, consumiendo CPU innecesariamente cuando no hay items en la cola.

**Archivos afectados:**
- `QRCode/Services/BackgroundMailQueueService.cs`
- `QRCode/Services/BackgroundMailQueue.cs`

**Tareas:**
- [ ] Reemplazar `ConcurrentQueue` con `Channel<T>` para soporte nativo de async/await
- [ ] Eliminar `Task.Delay` y usar `await channel.Reader.WaitToReadAsync()`
- [ ] Actualizar interfaz y implementación

**Código propuesto:**
```csharp
private readonly Channel<Email> _channel = Channel.CreateUnbounded<Email>();

public async ValueTask EnqueueAsync(Email item) 
    => await _channel.Writer.WriteAsync(item);

// En BackgroundService:
await foreach (var email in _channel.Reader.ReadAllAsync(stoppingToken))
{
    // Procesar email
}
```

**Impacto:** Bajo - Mejora eficiencia, reduce latencia

---

### Issue 14: Añadir health checks
**Prioridad:** P2 - Baja  
**Categoría:** Observabilidad / DevOps  
**Labels:** `enhancement`, `observability`, `devops`

**Descripción:**
No hay endpoints de health check para monitoreo en producción.

**Tareas:**
- [ ] Añadir paquete `Microsoft.Extensions.Diagnostics.HealthChecks`
- [ ] Configurar health checks básicos
- [ ] Añadir health check para SMTP (¿puede conectar?)
- [ ] Añadir health check para sistema de archivos
- [ ] Exponer en `/health` y `/health/ready`

**Código propuesto:**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("smtp", () => /* verificar SMTP */)
    .AddCheck("storage", () => /* verificar archivo */)
    .AddCheck("background_queue", () => /* verificar cola */);

app.MapHealthChecks("/health");
```

**Impacto:** Bajo - Facilita monitoreo en producción

---

### Issue 15: Simplificar configuración de EncryptionKey
**Prioridad:** P2 - Baja  
**Categoría:** Configuration / Developer Experience  
**Labels:** `enhancement`, `configuration`, `developer-experience`

**Descripción:**
La configuración actual requiere un byte array en base64, lo cual es poco intuitivo. Debería aceptar una string simple.

**Archivos afectados:**
- `QRCode/Services/RandomIvEncryptionService.cs`
- `QRCode/Program.cs`
- `QRCode/appsettings.json`

**Tareas:**
- [ ] Modificar `RandomIvEncryptionService` para aceptar string en constructor
- [ ] Derivar byte array usando PBKDF2 o similar
- [ ] Actualizar configuración para usar string simple
- [ ] Actualizar README con nueva configuración

**Configuración propuesta:**
```json
{
  "EncryptionKey": "mi-clave-super-secreta-y-larga-para-mayor-seguridad"
}
```

**Impacto:** Bajo - Mejora developer experience

---

### Issue 16: Añadir versionado de API
**Prioridad:** P2 - Baja  
**Categoría:** API Design  
**Labels:** `enhancement`, `api`, `architecture`

**Descripción:**
La API no tiene versionado, dificultando cambios breaking en el futuro.

**Tareas:**
- [ ] Añadir paquete `Asp.Versioning.Http`
- [ ] Configurar versionado en `Program.cs`
- [ ] Añadir `/v1/` a rutas actuales
- [ ] Actualizar frontend para usar rutas versionadas
- [ ] Documentar estrategia de versionado

**Código propuesto:**
```csharp
builder.Services.AddApiVersioning(options => {
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
});

app.MapGet("/v1/wifi", () => { ... });
```

**Impacto:** Bajo - Facilita evolución de API

---

### Issue 17: Mejorar documentación del README
**Prioridad:** P2 - Baja  
**Categoría:** Documentation  
**Labels:** `documentation`, `enhancement`

**Descripción:**
El README es básico y le faltan detalles técnicos importantes.

**Tareas a añadir al README:**
- [ ] Requisitos del sistema (.NET 8.0 SDK)
- [ ] Instrucciones detalladas de configuración
- [ ] Cómo ejecutar ambos proyectos simultáneamente
- [ ] Arquitectura del sistema (diagrama)
- [ ] Estructura del proyecto
- [ ] Cómo contribuir
- [ ] Troubleshooting común
- [ ] Configuración de email para desarrollo (usar MailHog o similar)

**Impacto:** Bajo - Facilita onboarding de nuevos desarrolladores

---

### Issue 18: Remover variable no usada en WiFi.cshtml
**Prioridad:** P2 - Baja  
**Categoría:** Code Quality  
**Labels:** `cleanup`, `good first issue`

**Descripción:**
Variable `datePattern` declarada pero nunca usada.

**Archivo afectado:**
- `FrontEnd/Views/Home/WiFi.cshtml:5`

**Tarea:**
- [ ] Eliminar línea: `string datePattern = "dd/MM/yyyy HH:mm:ss";`

**Impacto:** Muy bajo - Limpieza de código

---

### Issue 19: Añadir configuración de producción
**Prioridad:** P2 - Baja  
**Categoría:** Configuration / DevOps  
**Labels:** `enhancement`, `configuration`, `devops`

**Descripción:**
No hay diferenciación clara entre configuración de desarrollo y producción.

**Tareas:**
- [ ] Crear `appsettings.Production.json` en ambos proyectos
- [ ] Documentar variables de entorno necesarias
- [ ] Configurar User Secrets para desarrollo
- [ ] Documentar estrategia de secretos en producción (Azure Key Vault, etc.)
- [ ] Añadir validación de configuración al startup

**Impacto:** Bajo - Facilita deployment

---

### Issue 20: Añadir comentarios XML en código público
**Prioridad:** P2 - Baja  
**Categoría:** Documentation  
**Labels:** `documentation`, `code-quality`

**Descripción:**
Los servicios e interfaces públicas carecen de documentación XML.

**Archivos afectados:**
- Todos los servicios en `QRCode/Services/`
- Todas las interfaces
- Modelos públicos

**Tareas:**
- [ ] Añadir comentarios `///` a todas las interfaces públicas
- [ ] Añadir comentarios a métodos públicos explicando parámetros
- [ ] Habilitar generación de XML docs en .csproj
- [ ] Configurar Swagger para usar XML comments

**Impacto:** Bajo - Mejora documentación de código

---

## 📊 Resumen por Prioridad

- **P0 (Alta):** 5 issues - Seguridad y estabilidad crítica
- **P1 (Media):** 5 issues - Mantenibilidad y arquitectura
- **P2 (Baja):** 10 issues - Calidad de vida y mejoras incrementales

**Total:** 20 issues identificados

---

## 🚀 Orden Sugerido de Implementación

1. **Issues 1-5** (P0): Abordar primero para estabilizar la aplicación
2. **Issues 6-8** (P1 - Arquitectura): Refactorizar servicios principales
3. **Issues 9-10** (P1 - Observabilidad): Añadir logging y organizar código
4. **Issues 11-20** (P2): Mejoras incrementales según necesidad

---

## 📝 Notas

- Cada issue es independiente y puede ser trabajado por separado
- Los issues P0 pueden tener dependencias entre sí
- Algunos issues P1/P2 pueden requerir issues P0 completados primero
- Se recomienda crear branches separadas para cada issue
- Los issues marcados con `good first issue` son buenos para contributors nuevos
