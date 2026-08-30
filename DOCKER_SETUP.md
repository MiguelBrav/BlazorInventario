# Configuración de Docker con GitHub Actions

Guía rápida para configurar el build automático de imágenes Docker usando GitHub Actions. Esto es especialmente útil si trabajas en Windows on ARM y no puedes compilar imágenes Docker localmente.

## Qué hace esto

Cuando haces push al repositorio, GitHub Actions automáticamente:
1. Compila la imagen Docker
2. La sube a Docker Hub
3. La etiqueta como `latest` y con el commit SHA

## Configuración inicial

### 1. Crear cuenta en Docker Hub

Si no tienes una:
- Ve a [Docker Hub](https://hub.docker.com/) y regístrate
- En tu perfil, ve a "Account Settings" > "Security" > "New Access Token"
- Dale un nombre (ej: "github-actions") y selecciona "Read & Write"
- **IMPORTANTE**: Guarda el token, no lo volverás a ver

### 2. Configurar secrets en GitHub

En tu repositorio de GitHub:
1. Ve a `Settings` > `Secrets and variables` > `Actions`
2. Crea dos secrets:

**Secret 1:**
- Name: `DOCKER_USERNAME`
- Secret: tu usuario de Docker Hub (sin @)

**Secret 2:**
- Name: `DOCKER_PASSWORD`
- Secret: el access token que creaste en Docker Hub

### 3. Verificar el workflow

El archivo `.github/workflows/docker.yml` ya debería estar configurado. Si no, copia:

```yaml
name: Docker Build and Push

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Login to Docker Hub
        uses: docker/login-action@v3
        with:
          username: ${{ secrets.DOCKER_USERNAME }}
          password: ${{ secrets.DOCKER_PASSWORD }}

      - name: Build and push Docker image
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: |
            ${{ secrets.DOCKER_USERNAME }}/blazorinventario:latest
            ${{ secrets.DOCKER_USERNAME }}/blazorinventario:${{ github.sha }}
```

## Cómo usarlo

### Build automático

Cada vez que hagas push a la rama `main`:

```bash
git add .
git commit -m "algún cambio"
git push origin main
```

GitHub Actions se activará automáticamente y construirá la imagen.

### Build manual

Si quieres forzar un build sin hacer cambios:

1. Ve a la pestaña `Actions` en tu repositorio
2. Selecciona "Docker Build and Push"
3. Click en "Run workflow"
4. Elige la rama y dale a "Run workflow"

## Usar la imagen en tu servidor

Una vez que el build termine, en tu servidor:

```bash
# Descargar la imagen
docker pull tu-usuario/blazorinventario:latest

# Ejecutarla
docker run -d \
  --name blazorinventario \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=tu-servidor;Database=tu-db;User=tu-user;Password=tu-pass" \
  tu-usuario/blazorinventario:latest
```

O con docker-compose:

```yaml
version: '3.8'
services:
  app:
    image: tu-usuario/blazorinventario:latest
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=tu-servidor;Database=tu-db;User=tu-user;Password=tu-pass
    restart: unless-stopped
```

## Posibles mejoras

### Deploy automático al servidor

Si quieres que el servidor se actualice automáticamente cuando haya una nueva imagen, puedes agregar al workflow:

```yaml
- name: Deploy to server
  uses: appleboy/ssh-action@master
  with:
    host: ${{ secrets.SERVER_HOST }}
    username: ${{ secrets.SERVER_USER }}
    key: ${{ secrets.SSH_KEY }}
    script: |
      docker pull tu-usuario/blazorinventario:latest
      docker stop blazorinventario
      docker rm blazorinventario
      docker run -d --name blazorinventario -p 8080:8080 -e ConnectionStrings__DefaultConnection="${{ secrets.DB_CONNECTION_STRING }}" tu-usuario/blazorinventario:latest
```

Necesitarías agregar estos secrets adicionales:
- `SERVER_HOST`: IP o dominio de tu servidor
- `SERVER_USER`: usuario SSH
- `SSH_KEY`: tu llave privada SSH
- `DB_CONNECTION_STRING`: tu connection string

### Notificaciones

Para recibir notificaciones cuando el build termine:

```yaml
- name: Notify on success
  if: success()
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

### Builds programados

Si quieres builds regulares aunque no haya cambios:

```yaml
on:
  schedule:
    - cron: '0 2 * * *'  # Todos los días a las 2 AM
  push:
    branches: [ main ]
```

### Multi-arquitectura

Si necesitas soportar ARM y AMD64:

```yaml
- name: Build and push
  uses: docker/build-push-action@v5
  with:
    context: .
    platforms: linux/amd64,linux/arm64
    push: true
    tags: |
      ${{ secrets.DOCKER_USERNAME }}/blazorinventario:latest
```

### Tests antes del build

Para no construir imágenes si el código no compila:

```yaml
- name: Run tests
  run: dotnet test

- name: Build and push
  if: success()  # Solo ejecuta si los tests pasan
  uses: docker/build-push-action@v5
  # ... resto del config
```

## Troubleshooting

### El workflow no se ejecuta

- Verifica que el archivo esté en `.github/workflows/docker.yml` en la raíz del repo
- Revisa que esté en la rama `main`
- Mira la pestaña `Actions` para ver si hay errores

### Error de autenticación en Docker Hub

- Verifica que los secrets `DOCKER_USERNAME` y `DOCKER_PASSWORD` estén correctos
- Revisa que el access token de Docker Hub tenga permisos de "Read & Write"
- El token puede haber expirado, genera uno nuevo

### Build falla

- Revisa los logs en la pestaña `Actions`
- Verifica que el Dockerfile esté correcto
- Asegúrate que `.dockerignore` no esté excluyendo archivos necesarios

### No puedo hacer pull de la imagen

- Verifica que el nombre de la imagen sea correcto: `tu-usuario/blazorinventario:latest`
- Si es privada, necesitas hacer `docker login` primero
- Revisa en Docker Hub que la imagen exista

## Tips

- Usa organization secrets si tienes varios repos que usan el mismo Docker Hub
- Para debuggear, agrega pasos que muestren variables o ejecuten comandos
- Los tags con commit SHA (`:commit-sha`) son útiles para rollback
- Considera usar un registry privado si es para producción

## Recursos útiles

- [GitHub Actions docs](https://docs.github.com/en/actions)
- [Docker Hub](https://hub.docker.com/)
- [Docker buildx](https://docs.docker.com/buildx/working-with-buildx/)
