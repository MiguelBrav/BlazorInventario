-- Mini Inventory System - schema for MariaDB (all identifiers in English)
-- Tables: users, categories, suppliers, products, movements

CREATE DATABASE IF NOT EXISTS mininventary CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE mininventary;

-- Table users
CREATE TABLE IF NOT EXISTS users (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role ENUM('Admin','Storekeeper') NOT NULL DEFAULT 'Storekeeper',
    is_active TINYINT(1) NOT NULL DEFAULT 1,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table categories
CREATE TABLE IF NOT EXISTS categories (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table suppliers
CREATE TABLE IF NOT EXISTS suppliers (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    contact VARCHAR(150) NULL,
    phone VARCHAR(50) NULL,
    email VARCHAR(255) NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table products
CREATE TABLE IF NOT EXISTS products (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    category_id INT NULL,
    stock_current INT NOT NULL DEFAULT 0,
    stock_minimum INT NOT NULL DEFAULT 0,
    average_cost DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_products_category FOREIGN KEY (category_id) REFERENCES categories(id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table movements
CREATE TABLE IF NOT EXISTS movements (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    product_id INT NOT NULL,
    type ENUM('in','out') NOT NULL,
    quantity INT NOT NULL,
    unit_cost DECIMAL(18,4) NOT NULL,
    supplier_id INT NULL,
    date DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    user_id INT NULL,
    notes TEXT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_movements_product FOREIGN KEY (product_id) REFERENCES products(id)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_movements_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers(id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT fk_movements_user FOREIGN KEY (user_id) REFERENCES users(id)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Frequent indexes
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
CREATE INDEX IF NOT EXISTS idx_movements_product_date ON movements(product_id, date);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);

-- Note: To insert an initial admin, generate a BCrypt hash for the password and INSERT, for example:
-- INSERT INTO users (name, email, password_hash, role, is_active) VALUES ('Admin','admin@example.com','$2y$...hash...','Admin',1);


START TRANSACTION;

-- 1) products: agregar columnas si no existen
ALTER TABLE products
  ADD COLUMN IF NOT EXISTS is_deleted TINYINT(1) NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS status CHAR(1) NOT NULL DEFAULT 'A';

-- 2) movements: agregar columna canceled si no existe
ALTER TABLE movements
  ADD COLUMN IF NOT EXISTS canceled TINYINT(1) NOT NULL DEFAULT 0;

-- 3) Mapear valores existentes (si anteriormente usabas 'activo' / 'dado_de_baja' en status)
-- Ajusta las condiciones si tus valores actuales difieren.
UPDATE products
SET status = 'A'
WHERE status IS NULL OR status = '' OR LOWER(status) = 'activo';

UPDATE products
SET status = 'I', is_deleted = 1
WHERE LOWER(status) IN ('dado_de_baja', 'dado de baja') OR is_deleted = 1;

COMMIT;