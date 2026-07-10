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

### FCM en docker compose

Para que el contenedor mande push notifications reales (no simuladas) necesitás
el archivo JSON de credenciales de la cuenta de servicio de Firebase (el mismo
que usás en local con `dotnet user-secrets set "Fcm:CredentialsPath" "<ruta>"`)
y el `ProjectId` del proyecto de Firebase. Ninguno de los dos se hardcodea en
`docker-compose.yml` -- se pasan como variables de entorno del shell antes de
levantar el compose:

```bash
export FCM_PROJECT_ID="meditrack-app-si657"
export FCM_CREDENTIALS_HOST_PATH="/ruta/absoluta/a/tu/service-account.json"
# Opcional: por defecto SimulateDelivery=false (push real). Poné "true" si
# todavía no tenés credenciales y querés levantar el servicio igual.
export FCM_SIMULATE_DELIVERY=false
docker compose up
```

`FCM_CREDENTIALS_HOST_PATH` se monta como volumen de solo lectura dentro del
contenedor en `/app/firebase-credentials.json`, y `Fcm__CredentialsPath` ya
apunta a esa ruta interna. Si falta `FCM_PROJECT_ID` o
`FCM_CREDENTIALS_HOST_PATH`, `docker compose up` falla con un mensaje
explicando qué variable falta.
