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

### Secretos en desarrollo local

`Jwt:Key` está vacío en `appsettings.json` a propósito -- es compartido con el
Gateway, Identity Service, Treatment-service y FollowUp-Service, así que cada
dev lo configura una vez en su máquina con:

```bash
dotnet user-secrets set "Jwt:Key" "<pedile la clave al equipo>" --project src/MediTrack.ReminderService.Api
```

En producción, esa misma variable se setea como `Jwt__Key` en el entorno del
proveedor de deploy (Render, etc.) -- nunca en un archivo del repo.

Para correr con `docker compose`, `Jwt__Key` se toma de un `.env` local
(no versionado) o de una variable de entorno del shell:

```bash
export JWT_KEY="<pedile la clave al equipo>"
docker compose up
```
