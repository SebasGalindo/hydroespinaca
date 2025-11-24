# Configuración TLS con DNS-01 (Cloudflare)

Este proyecto utiliza certificados SSL/TLS generados automáticamente mediante Certbot con el plugin DNS-01 de Cloudflare. Esta guía explica cómo configurar el sistema correctamente.

## Requisitos previos

1. **Token de API de Cloudflare**: Necesitas un token con permisos de edición de DNS
2. **Red Docker externa**: Debes crear la red Docker antes de iniciar los servicios
3. **Variables de entorno**: Configura las variables necesarias en tu archivo `.env`

## Paso 1: Crear red Docker externa

Antes de iniciar los contenedores, crea la red Docker externa:

```bash
docker network create hydroespinaca
```

Verifica que la red se creó correctamente:

```bash
docker network ls | grep hydroespinaca
```

## Paso 2: Configurar Token de Cloudflare

### 2.1 Obtener el token

1. Ve a https://dash.cloudflare.com/profile/api-tokens
2. Haz clic en "Create Token"
3. Usa la plantilla "Edit zone DNS" o crea uno personalizado con:
   - **Permissions**: `Zone -> DNS -> Edit`
   - **Zone Resources**: Include -> Specific zone -> `hydroespinaca.online`
4. Copia el token generado

### 2.2 Configurar en .env

Edita tu archivo `.env` (o `.env.production` para producción):

```bash
# Cloudflare API Token para DNS-01 Challenge
CLOUDFLARE_API_TOKEN=tu_token_de_cloudflare_aqui

# Habilitar TLS
USE_TLS=true
ENVIRONMENT=Production
```

## Paso 3: Generar certificados

### 3.1 Modo inicial (solo certificados)

Para generar certificados sin iniciar todos los servicios:

```bash
# Establecer modo certbot-only
export CERTBOT_ONLY=true

# Iniciar solo nginx y certbot
docker-compose --profile production up nginx-proxy certbot
```

El sistema:
1. Creará automáticamente el archivo `/etc/letsencrypt/cloudflare.ini` dentro del contenedor
2. Generará un certificado wildcard para `hydroespinaca.online` y `*.hydroespinaca.online`
3. Este certificado cubrirá todos los subdominios (www, api, mqtt, etc.)

### 3.2 Verificar certificados

```bash
# Ver los certificados generados
docker exec certbot ls -la /etc/letsencrypt/live/

# Ver detalles del certificado
docker exec certbot openssl x509 -in /etc/letsencrypt/live/hydroespinaca.online/fullchain.pem -text -noout
```

## Paso 4: Iniciar servicios en producción

Una vez generados los certificados:

```bash
# Desactivar modo certbot-only
export CERTBOT_ONLY=false

# Iniciar todos los servicios
docker-compose --profile production up -d
```

## Renovación automática

El servicio `certbot-renew` se ejecuta automáticamente cada 12 horas y:

1. Verifica si los certificados necesitan renovación (< 30 días)
2. Usa el método DNS-01 con Cloudflare (no requiere detener nginx)
3. Recarga nginx automáticamente si se renuevan certificados
4. Actualiza certificados para MQTT si es necesario

## Ventajas del método DNS-01

✅ **No requiere HTTP**: No necesita que el puerto 80 esté accesible públicamente
✅ **Cloudflare proxied**: Puedes mantener el proxy naranja de Cloudflare activado
✅ **Certificados wildcard**: Un solo certificado cubre todos los subdominios
✅ **Renovación automática**: No necesita intervención manual
✅ **Sin downtime**: Nginx no necesita detenerse para renovar

## Troubleshooting

### Error: "Network hydroespinaca not found"

```bash
# Crear la red manualmente
docker network create hydroespinaca
```

### Error: "Invalid Cloudflare credentials"

1. Verifica que el token esté correctamente configurado en `.env`
2. Verifica que el token tenga permisos de edición de DNS
3. Verifica que el token no haya expirado

### Ver logs de certbot

```bash
# Logs del contenedor certbot
docker logs certbot

# Logs del servicio de renovación
docker logs certbot-renew -f
```

### Forzar renovación manual

```bash
# Forzar renovación de todos los certificados
docker exec certbot-renew certbot renew --dns-cloudflare --dns-cloudflare-credentials /etc/letsencrypt/cloudflare.ini --force-renewal
```

## Estructura de certificados

```
/etc/letsencrypt/
├── live/
│   └── hydroespinaca.online/
│       ├── fullchain.pem    # Certificado completo (para nginx)
│       ├── privkey.pem      # Clave privada (para nginx)
│       ├── cert.pem         # Certificado del servidor
│       └── chain.pem        # Cadena de certificados
└── cloudflare.ini           # Credenciales de Cloudflare (600 perms)
```

## Seguridad

- El archivo `cloudflare.ini` se genera automáticamente con permisos `600` (solo lectura para root)
- El token de Cloudflare solo tiene permisos de edición de DNS
- Las credenciales nunca se almacenan en el repositorio
- El token se pasa como variable de entorno al contenedor
