# MediTrack — Reminder Service (`meditrack-reminder-service`)

Microservicio de **recordatorios** de la plataforma **MediTrack** (startup *Pafi Solutions*).
Es el componente de mayor criticidad clínica de la arquitectura: genera, programa,
entrega (vía Firebase Cloud Messaging) y cancela los recordatorios de medicación,
citas y exámenes de los pacientes.

> Implementa el _Reminder Service_ descrito en el Capítulo IV del informe de
> arquitectura (Component Diagram Fig. 17, Class Diagram Fig. 27, ADD Iteración 1).

---

## 1. Stack tecnológico

| Capa | Tecnología | Justificación (informe) |
| ---- | ---------- | ----------------------- |
| Runtime / API | **.NET 8** (ASP.NET Core Web API) | CON-07, 5.2.1 |
| Persistencia | **Entity Framework Core 8** + **Pomelo MySQL** sobre **MySQL 8** | CON-01 (Database per Service) |
| Mensajería | **RabbitMQ 3** (exchange topic) | 5.4, AC-02/AC-08 |
| Notificaciones push | **Firebase Cloud Messaging** | CON-05 |
| Seguridad | **JWT** (validación local de firma) | CON-04 |
| Documentación | **OpenAPI / Swagger** | 5.2.1 |
| Pruebas | **xUnit** + **Reqnroll (BDD `.feature`)** + **FluentAssertions** | 5.2.2 |

---

## 2. Arquitectura (Clean Architecture)

Cuatro proyectos con dependencias apuntando **hacia el dominio** (regla de dependencia):

```
MediTrack.ReminderService.Api            → controladores REST, Program, JWT, Swagger, health
        │  depende de
MediTrack.ReminderService.Infrastructure → EF Core/MySQL, RabbitMQ, FCM, Scheduler, Outbox/Inbox
        │  depende de
MediTrack.ReminderService.Application    → casos de uso (Schedule / Notification / Preference)
        │  depende de
MediTrack.ReminderService.Domain         → entidades, Factory Method, puertos (sin dependencias)
```

### Mapeo con el Component Diagram (Fig. 17)

| Componente del diagrama | Implementación |
| ----------------------- | -------------- |
| Reminder Controller | `Api/Controllers/RemindersController.cs` |
| Preference Controller | `Api/Controllers/PreferencesController.cs` |
| Event Consumer | `Infrastructure/Messaging/RabbitMqEventConsumerHostedService.cs` |
| Schedule Application Service | `Application/Services/ScheduleApplicationService.cs` |
| Notification Application Service | `Application/Services/NotificationApplicationService.cs` |
| Preference Application Service | `Application/Services/PreferenceApplicationService.cs` |
| Scheduler | `Infrastructure/Scheduling/ReminderSchedulerHostedService.cs` |
| Reminder Domain Model | `Domain/Entities/*`, `Domain/Factories/*` |
| Reminder / Preference Repository | `Infrastructure/Persistence/Repositories/*` |
| Notification Adapter (FCM) | `Infrastructure/Notifications/FcmNotificationSender.cs` |
| Reminder DB (MySQL) | `Infrastructure/Persistence/ReminderDbContext.cs` |
| Message Bus (RabbitMQ) | `Infrastructure/Messaging/RabbitMqConnection.cs` |

---

## 3. Patrones de diseño aplicados (sección 4.1.6)

| Patrón | Dónde |
| ------ | ----- |
| **Factory Method** | `Domain/Factories/` — `ReminderFactory` (creador abstracto) + `Medication/Appointment/Exam` factories. Cada tipo decide su anticipación y mensaje. |
| **Repository** | `IReminderRepository`, `INotificationPreferenceRepository` y sus implementaciones EF Core. |
| **Singleton** | `DatabaseConnectionPool` + `AddDbContextPool`: un único pool de conexiones MySQL. |
| **Observer** (event-driven) | `Event Consumer` + Outbox: comunicación asíncrona desacoplada entre microservicios. |
| **Outbox** | `OutboxIntegrationEventPublisher` + `OutboxDispatcherHostedService` (entrega confiable, AC-08). |
| **Inbox** (idempotencia) | `ProcessedEvent` evita procesar dos veces un evento reentregado. |

---

## 4. Endpoints REST

> Todos requieren `Authorization: Bearer <jwt>`. Documentados en Swagger UI (`/swagger`).

| Método | Ruta | Descripción |
| ------ | ---- | ----------- |
| `GET`  | `/reminders/patients/{patientId}` | Lista los recordatorios activos del paciente. |
| `PUT`  | `/reminders/{id}/cancel` | Cancela un recordatorio. |
| `GET`  | `/reminders/preferences/patients/{patientId}` | Obtiene las preferencias de notificación (US22). |
| `PUT`  | `/reminders/preferences/patients/{patientId}` | Crea/actualiza las preferencias. |
| `GET`  | `/health` | Liveness/readiness (incluye verificación de MySQL). |

---

## 5. Eventos de integración (RabbitMQ)

**Consume** (genera/cancela recordatorios):

| Evento | Origen | Efecto |
| ------ | ------ | ------ |
| `RecetaCargada` | Treatment Service | Genera un recordatorio de medicación por dosis (US05/US13). |
| `CitaAgendada` | Appointment Service | Genera un recordatorio de cita 24 h antes (US09). |
| `StockBajo` | Treatment Service | Genera una alerta inmediata de reabastecimiento (US07). |
| `CumplimientoRegistrado` | Follow-up Service | Cancela el recordatorio pendiente (US06). |

**Publica:** `RecordatorioEnviado` (vía Outbox) para alimentar la analítica de adherencia.

---

## 6. Resiliencia de la entrega (AC-02 / AC-09)

El `NotificationApplicationService` aplica:

1. Consulta de **preferencias** del paciente (interruptor global, sonido, vibración).
2. **Reintentos con backoff exponencial** por el canal push (FCM): `delay = base · 2^intento`.
3. **Fallback local** (AC-09) cuando se agotan los reintentos: la app programa la notificación local.
4. **Bitácora** de cada intento en `notification_log` para auditar el QAS-2 (99.9 %).

---

## 7. Base de datos (Database per Service)

Esquema propio `meditrack_reminder` (MySQL 8), tablas en `snake_case` singular:

| Tabla | Origen (Class Diagram) |
| ----- | ---------------------- |
| `reminder` | `Reminder` |
| `notification_log` | `NotificationLog` |
| `notification_preference` | `NotificationPreference` |
| `outbox_message` / `processed_event` | Infraestructura (Outbox/Inbox) |

Migraciones EF Core en `Infrastructure/Persistence/Migrations/`.

---

## 8. Cómo ejecutar

### Opción A — Todo con Docker (recomendado)

```bash
docker compose up --build
```

Levanta MySQL 8, RabbitMQ 3 y el servicio. Luego abre:

- Swagger UI: <http://localhost:8080/swagger>
- Health:     <http://localhost:8080/health>
- RabbitMQ:   <http://localhost:15672> (guest / guest)

### Opción B — Local (.NET SDK 8)

```bash
# 1. Levanta dependencias
docker compose up -d mysql rabbitmq

# 2. Aplica migraciones (o deja Database:AutoMigrate=true)
dotnet ef database update \
  --project src/MediTrack.ReminderService.Infrastructure \
  --startup-project src/MediTrack.ReminderService.Api

# 3. Ejecuta la API
dotnet run --project src/MediTrack.ReminderService.Api
```

### Pruebas

```bash
dotnet test
```

---

## 9. Configuración (`appsettings.json` / variables de entorno)

| Clave | Descripción |
| ----- | ----------- |
| `ConnectionStrings:ReminderDb` | Cadena de conexión MySQL. |
| `RabbitMq:*` | Host, credenciales y topología del Message Bus. |
| `Jwt:Key/Issuer/Audience` | Validación local del token (CON-04). |
| `Fcm:SimulateDelivery` | `true` en desarrollo (no envía push real). Para FCM real: `CredentialsPath` + `ProjectId`. |
| `Scheduler:PollingSeconds` | Frecuencia del barrido de recordatorios vencidos. |
| `ReminderNotification:MaxAttempts/BaseDelaySeconds` | Política de reintentos (AC-02). |

> Los secretos (clave JWT, credenciales de FCM, contraseñas) se inyectan por variables
> de entorno / Azure Key Vault en producción; **nunca** se versionan (sección 5.4).
