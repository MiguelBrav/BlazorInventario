# BlazorInventario

Aplicación Blazor (componentes interactivos, Server) para gestión básica de inventario. Proyecto orientado a .NET 10.

### Características principales
- Gestión de categorías, productos y movimientos (entradas/salidas).
- Autenticación por cookies (endpoints `/signin` y `/signout`).
- Exportación CSV para productos, categorías, movimientos y usuarios.
- Dashboard con métricas y gráficos (Chart.js desde JS interop).

---

## Requisitos
- .NET 10 SDK
- MySQL (u otra base compatible, ajustar connection string)
- Microsoft Visual Studio (recomendado) o `dotnet` CLI

---

## Configuración rápida
- La conexión a BD se toma de la cadena `DefaultConnection`. Si no está definida, el código usa por defecto:
  `Server=localhost;Database=mininventary;User=root;Password=pass;`
  Cambiar la cadena en `appsettings.json`, variables de entorno o en la configuración de Visual Studio.

---

## Ejecutar localmente
- Abrir la solución en Visual Studio y ejecutar (IIS Express o perfil de ejecución).
- O desde la terminal (en la carpeta del proyecto):
  dotnet run

El servidor mapea los assets estáticos y monta los componentes interactivos tal como está en `Program.cs`.

---

## Cómo probar (demo)
Para probar la demo desplegada puede acceder a:
https://inventario.segurab.com/

Credenciales de prueba:
- Usuario: `miguel@gmail.com`
- Contraseña: `migue123`

---

## Productos incluidos en la demo
- Coca-Cola 600 ml — Categoría: Bebidas  
  Refresco carbonatado sabor cola en presentación de 600 ml, ideal para consumo individual.
- Papas Sabritas Original 45 g — Categoría: Botanas  
  Papas fritas sabor original en bolsa de 45 g, perfectas para un snack o acompañar comidas.
- Chocolate Carlos V 20 g — Categoría: Dulces y Chocolates  
  Barra de chocolate con leche de 20 g, un clásico para disfrutar como postre o antojo.

---

## Categorías de la demo (descripciones)
- Bebidas: Productos líquidos destinados al consumo, incluyendo refrescos, jugos, agua, bebidas energéticas y otras bebidas embotelladas o enlatadas.
- Botanas: Alimentos ligeros para consumir entre comidas, como papas fritas, frituras, palomitas, cacahuates y otros snacks.
- Dulces y Chocolates: Productos de confitería como chocolates, caramelos, gomitas, chicles y otros dulces para consumo ocasional.

---

## Notas útiles para desarrolladores
- Autenticación: cookie-based, endpoints definidos en `Program.cs` (`/signin`, `/signout`).
- Repositorios y registros: verificar `Repositories` y `Services` para reglas de negocio y exportación CSV.
- Cambios en la cadena de conexión: actualizar `DefaultConnection` en la configuración que prefiera (appsettings/Secrets/ENV).
- Si necesita datos de ejemplo localmente, insertar los productos/categorías anteriores en la tabla correspondiente.

---

Si necesita, puedo generar un script SQL básico para insertar las categorías y productos de la demo.