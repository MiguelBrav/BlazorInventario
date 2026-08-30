# BlazorInventario - Documentación del Proyecto

## Reglas del Proyecto

### No usar emojis
- **REGLA IMPORTANTE**: No usar emojis en el código o en la interfaz de usuario
- Utilizar siempre iconos de Bootstrap Icons (`bi bi-*`) para cualquier elemento visual
- Los iconos deben ser consistentes con el estilo del sidebar (ej: `bi bi-box-seam`, `bi bi-people`, etc.)
- Ejemplo correcto: `<i class="bi bi-currency-dollar"></i>`
- Ejemplo incorrecto: `💰`

### Idioma del código vs interfaz de usuario
- **REGLA IMPORTANTE**: Todo el código backend puede estar en inglés, incluyendo comentarios
- Sin embargo, todo lo que el usuario final ve visualmente debe estar en español
- Esto incluye: etiquetas, mensajes, títulos, textos en la UI, validaciones, etc.
- Ejemplo correcto:
  - Código: `var totalProducts = products.Count;` // Count total products
  - UI: `<h6>Productos Activos</h6>`
- Ejemplo incorrecto:
  - Código: `var totalProductos = productos.Count();` // Contar productos
  - UI: `<h6>Active Products</h6>`

## Resumen del Proyecto
BlazorInventario es un sistema de gestión de inventario desarrollado con Blazor Server (.NET 10.0) que utiliza MySQL como base de datos. El sistema permite gestionar productos, categorías, movimientos de inventario y usuarios con autenticación basada en cookies.

## Arquitectura General

### Stack Tecnológico
- **Framework**: Blazor Server (.NET 10.0)
- **Base de datos**: MySQL con MySqlConnector
- **ORM**: Dapper para acceso a datos
- **Autenticación**: Cookie Authentication con Claims
- **Frontend**: Bootstrap 5 para estilos
- **Gráficos**: Chart.js para visualizaciones
- **Hashing de contraseñas**: BCrypt.Net-Next

### Estructura del Proyecto
```
BlazorInventario/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor        # Layout principal
│   │   ├── NavMenu.razor            # Menú de navegación lateral
│   │   └── ReconnectModal.razor     # Modal de reconexión
│   ├── Pages/
│   │   ├── Home.razor               # Dashboard principal
│   │   ├── Products.razor           # Gestión de productos
│   │   ├── ProductEdit.razor        # Edición de productos
│   │   ├── Categories.razor         # Gestión de categorías
│   │   ├── CategoryEdit.razor       # Edición de categorías
│   │   ├── Movements.razor          # Historial de movimientos
│   │   ├── MovementCreate.razor     # Creación de movimientos
│   │   ├── Users.razor              # Gestión de usuarios (Admin)
│   │   ├── UserEdit.razor           # Edición de usuarios
│   │   ├── Login.razor              # Página de login
│   │   ├── Error.razor              # Página de error
│   │   └── NotFound.razor           # Página 404
│   ├── App.razor                    # Componente raíz
│   ├── Routes.razor                 # Definición de rutas
│   └── _Imports.razor               # Imports globales
├── Data/
│   └── MySqlConnectionFactory.cs    # Factory de conexiones MySQL
├── Repositories/
│   ├── Interfaces/
│   │   ├── IProductsRepository.cs
│   │   ├── ICategoriesRepository.cs
│   │   ├── IMovementsRepository.cs
│   │   └── IUserRepository.cs
│   ├── Records/
│   │   ├── ProductRecord.cs         # Modelo de producto
│   │   ├── CategoryRecord.cs        # Modelo de categoría
│   │   ├── MovementRecord.cs        # Modelo de movimiento
│   │   └── UserRecord.cs            # Modelo de usuario
│   └── Implementations/
│       ├── ProductsRepository.cs
│       ├── CategoriesRepository.cs
│       ├── MovementsRepository.cs
│       └── UserRepository.cs
├── Services/
│   ├── IInventoryService.cs         # Interfaz de lógica de inventario
│   ├── InventoryService.cs          # Lógica de negocio de inventario
│   ├── IAuthService.cs              # Interfaz de autenticación
│   ├── AuthService.cs               # Servicio de autenticación
│   └── Export Services              # Servicios de exportación CSV
├── wwwroot/
│   ├── js/
│   │   ├── chartHelpers.js          # Funciones para Chart.js
│   │   └── fileHelpers.js           # Funciones para exportación
│   └── lib/                         # Librerías cliente (Bootstrap, Chart.js)
└── Program.cs                        # Configuración de la aplicación

```

## Modelos de Datos

### ProductRecord
- `id`: Identificador del producto
- `name`: Nombre del producto (requerido, max 200 caracteres)
- `category_id`: ID de la categoría (opcional)
- `stock_current`: Stock actual (no negativo)
- `stock_minimum`: Stock mínimo para alertas (no negativo)
- `average_cost`: Costo promedio (no negativo)
- `is_deleted`: Flag de eliminación lógica
- `status`: Estado del producto
- `created_at`: Fecha de creación
- `updated_at`: Fecha de última actualización

### CategoryRecord
- `id`: Identificador de la categoría
- `name`: Nombre de la categoría (requerido, max 200 caracteres)
- `description`: Descripción (opcional)
- `created_at`: Fecha de creación
- `updated_at`: Fecha de última actualización

### MovementRecord
- `id`: Identificador del movimiento
- `product_id`: ID del producto asociado
- `type`: Tipo de movimiento ('in' para entrada, 'out' para salida)
- `quantity`: Cantidad movida (mayor que 0)
- `unit_cost`: Costo unitario (no negativo)
- `canceled`: Flag de cancelación
- `supplier_id`: ID del proveedor (opcional)
- `date`: Fecha del movimiento
- `user_id`: ID del usuario que realizó el movimiento
- `user_name`: Nombre del usuario (para display)
- `notes`: Notas adicionales
- `created_at`: Fecha de creación del registro

### UserRecord
- `id`: Identificador del usuario
- `name`: Nombre del usuario (requerido, max 150 caracteres)
- `email`: Email del usuario (requerido, formato válido, max 255 caracteres)
- `password_hash`: Hash de la contraseña
- `role`: Rol del usuario ('Admin', 'User', etc.)
- `is_active`: Flag de estado activo

## Servicios Principales

### InventoryService
Servicio de lógica de negocio que maneja:
- **CreateEntryAsync**: Crea movimientos de entrada, actualiza stock y recalcula costo promedio
- **CreateExitAsync**: Crea movimientos de salida, valida stock suficiente
- **CancelMovementAsync**: Cancela movimientos (solo Admin), revierte stock y costos
- **RecalculateProductCostsAsync**: Recalcula costos promedios de un producto

Cálculos importantes:
- **Nuevo costo promedio (entrada)**: `((oldStock * oldAvg) + (quantityAdded * newUnitCost)) / newStock`
- **Costo promedio después de salida**: `((oldStock * oldAvg) - (quantityRemoved * exitUnitCost)) / newStock`

### AuthService
Servicio de autenticación que maneja:
- Validación de credenciales
- Generación de claims para autenticación
- Verificación de roles

## Repositories

### ProductsRepository
- CRUD básico de productos
- `UpdateStockAndAverageCostAsync`: Actualización atómica de stock y costo promedio
- `MarkAsInactiveAsync`: Eliminación lógica

### MovementsRepository
- CRUD de movimientos
- `GetByFiltersAsync`: Filtrado por rango de fechas, producto y tipo
- `GetRecentAsync`: Obtener movimientos recientes
- `CancelAsync`: Marcar movimiento como cancelado

### CategoriesRepository
- CRUD básico de categorías

### UserRepository
- `GetByEmailAsync`: Buscar usuario por email
- `GetAllAsync`: Obtener todos los usuarios
- `DeactivateAsync`: Desactivar usuario (eliminación lógica)

## Configuración de Autenticación

### Roles
- **Admin**: Acceso completo, puede cancelar movimientos y gestionar usuarios
- **User**: Acceso limitado a operaciones básicas

### Flow de Autenticación
1. Usuario ingresa credenciales en `/login`
2. Endpoint `/signin` valida credenciales
3. Si válidas, se crea cookie de autenticación con claims
4. `ServerAuthenticationStateProvider` provee estado de autenticación
5. `CascadingAuthenticationState` distribuye el estado a componentes

## Dashboard Actual (Home.razor)

### Métricas existentes:
- Total de productos
- Productos con stock bajo (stock_current <= stock_minimum)
- Valor total del inventario (sumatoria de stock_current * average_cost)
- Gráfico de movimientos mensuales (últimos 12 meses)
- Gráfico de entradas vs salidas (últimos 12 meses)

### Implementación:
- Usa Chart.js para visualizaciones
- Funciones JavaScript en `chartHelpers.js`
- Carga datos en `OnInitializedAsync`
- Renderiza gráficos en `OnAfterRenderAsync`

## Configuración de Localización

El proyecto está configurado para usar localización es-MX (español de México):
- `CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("es-MX")`
- `CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("es-MX")`
- Middleware `UseRequestLocalization` para asegurar cultura en todas las solicitudes

Esto asegura que símbolos de moneda (₡ o $), formatos de fecha y números se muestren correctamente tanto en Windows como en Ubuntu.

## Comandos de Construcción y Ejecución

### Build
```bash
dotnet build
```

### Run
```bash
dotnet run
```

### Publicar para producción
```bash
dotnet publish -c Release
```

## Base de Datos

### Connection String
Configurada en `appsettings.json` o variable de entorno:
```
Server=localhost;Database=mininventary;User=root;Password=pass;
```

### Tablas principales
- `products`: Productos del inventario
- `categories`: Categorías de productos
- `movements`: Historial de movimientos
- `users`: Usuarios del sistema

## Consideraciones Importantes

1. **Transacciones**: Las operaciones de inventario usan transacciones para asegurar consistencia
2. **Eliminación lógica**: Productos y usuarios usan flags en lugar de eliminación física
3. **Recálculo de costos**: El sistema recalcula costos promedios al cancelar movimientos
4. **Autorización**: Las operaciones críticas (cancelar movimientos) requieren rol Admin
5. **Responsive**: La interfaz usa Bootstrap y es responsive
6. **Chart.js**: Los gráficos se renderizan del lado del cliente con Chart.js
