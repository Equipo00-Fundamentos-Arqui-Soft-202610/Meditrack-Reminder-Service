# MediTrack — Reminder Service

Microservicio de **recordatorios** de MediTrack: genera, programa, entrega (vía FCM)
y cancela los recordatorios de medicación, citas y exámenes de los pacientes.

> La documentación completa de arquitectura (diagramas C4, ADD, patrones, decisiones)
> está en el repositorio de documentación del proyecto (`meditrack-report`).

## Stack

.NET 8 · EF Core 8 + MySQL 8 · RabbitMQ 3 · Firebase Cloud Messaging · JWT · Swagger

## Cómo ejecutar

```bash
docker compose up --build
```

Levanta MySQL, RabbitMQ y el servicio. Luego:

- Swagger UI: <http://localhost:8080/swagger>
- Health: <http://localhost:8080/health>

### Pruebas

```bash
dotnet test
```

## Endpoints

| Método | Ruta |
| ------ | ---- |
| `GET`  | `/reminders/patients/{patientId}` |
| `PUT`  | `/reminders/{id}/cancel` |
| `GET`  | `/reminders/preferences/patients/{patientId}` |
| `PUT`  | `/reminders/preferences/patients/{patientId}` |

> Requieren `Authorization: Bearer <jwt>`.

## Configuración

Las claves principales (`ConnectionStrings:ReminderDb`, `RabbitMq:*`, `Jwt:*`, `Fcm:*`)
se definen en `appsettings.json` y se sobreescriben por variables de entorno.
Los secretos no se versionan.
