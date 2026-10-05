# TaskFlow API

[![CI](https://github.com/jose28gena/TaskFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/jose28gena/TaskFlow/actions/workflows/ci.yml)
![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)
![License: MIT](https://img.shields.io/badge/license-MIT-green)

API REST para gestionar tareas personales, construida con **.NET 8**, **Clean Architecture**, autenticación **JWT** y **EF Core**. Cada usuario solo puede ver y modificar sus propias tareas.

## Características

- Registro y login con JWT (contraseñas con BCrypt).
- CRUD de tareas con filtro por estado y paginación.
- Aislamiento por usuario: acceder a una tarea ajena devuelve `404`, no `403`, para no revelar que existe.
- Validación de entrada con DataAnnotations y errores en formato `ProblemDetails` (RFC 7807).
- Swagger/OpenAPI con soporte para autenticación Bearer.
- Endpoint de salud en `/health`.
- 13 tests de integración sobre la API real (xUnit + `WebApplicationFactory`).
- Dockerfile multi-etapa, `docker-compose` y CI con GitHub Actions.

## Arquitectura

```
src/
├── TaskFlow.Domain          # Entidades y enums. Sin dependencias.
├── TaskFlow.Application     # Casos de uso (AuthService, TaskService), DTOs y abstracciones
├── TaskFlow.Infrastructure  # EF Core + SQLite, JWT, BCrypt
└── TaskFlow.Api             # Controllers, middleware de errores, configuración
tests/
└── TaskFlow.Tests           # Tests de integración
```

Las dependencias apuntan hacia el dominio: `Api → Infrastructure → Application → Domain`. La capa de aplicación define las interfaces (`IAppDbContext`, `ITokenService`, `IPasswordHasher`) y la infraestructura las implementa.

## Endpoints

| Método | Ruta | Auth | Descripción |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | No | Crea un usuario y devuelve un token |
| POST | `/api/auth/login` | No | Devuelve un token |
| GET | `/api/tasks?status=&page=&pageSize=` | Sí | Lista paginada, con filtro opcional por estado |
| GET | `/api/tasks/{id}` | Sí | Detalle de una tarea |
| POST | `/api/tasks` | Sí | Crea una tarea |
| PUT | `/api/tasks/{id}` | Sí | Actualiza una tarea |
| DELETE | `/api/tasks/{id}` | Sí | Elimina una tarea |
| GET | `/health` | No | Estado del servicio |

Estados de una tarea: `Todo`, `InProgress`, `Done`.

## Ejecutar en local

Requisitos: [.NET 8 SDK](https://dotnet.microsoft.com/download).

La clave de firma JWT **no** está en el repositorio. Defínela como variable de entorno (mínimo 32 caracteres):

```powershell
# PowerShell
$env:Jwt__Key = "una-clave-larga-y-aleatoria-de-al-menos-32-caracteres"
dotnet run --project src/TaskFlow.Api
```

```bash
# Bash
export Jwt__Key="una-clave-larga-y-aleatoria-de-al-menos-32-caracteres"
dotnet run --project src/TaskFlow.Api
```

Abre Swagger en la URL que muestre la consola, añadiendo `/swagger`. Se crea una base SQLite `taskflow.db` en el directorio de trabajo.

Ejemplo rápido con curl:

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","password":"Password123!"}'

curl http://localhost:5000/api/tasks -H "Authorization: Bearer <token>"
```

## Con Docker

```bash
export JWT_KEY="$(openssl rand -base64 48)"
docker compose up --build
# API en http://localhost:8080/swagger
```

La base de datos se guarda en el volumen `taskflow-data`.

## Tests

```bash
dotnet test
```

Los tests levantan la API completa con una base SQLite temporal por ejecución y una clave JWT de prueba. Cubren registro, login, autenticación, validación, CRUD, paginación, filtros y aislamiento entre usuarios.

## Decisiones de diseño

- **SQLite por simplicidad**: el proyecto se ejecuta sin infraestructura externa. Cambiar a SQL Server o PostgreSQL implica cambiar el proveedor en `Infrastructure/DependencyInjection.cs`.
- **`EnsureCreated` en lugar de migraciones**: suficiente para una demo; en producción usaría migraciones de EF Core.
- **Secretos fuera del repo**: `Jwt__Key` se valida al arrancar y la aplicación falla rápido si falta o es demasiado corta.
- **Excepciones de aplicación mapeadas en un middleware**: los servicios lanzan `NotFoundException`, `ConflictException` o `InvalidCredentialsException` y la API decide el código HTTP.

## Próximos pasos

- Migraciones de EF Core y proveedor PostgreSQL.
- Refresh tokens.
- Etiquetas y fechas de vencimiento con recordatorios.

## Licencia

MIT. Consulta [LICENSE](LICENSE).
