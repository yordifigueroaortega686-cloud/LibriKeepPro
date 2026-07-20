import { test, expect } from '@playwright/test';

test.describe('Protección de Middleware/Enrutador - LibriKeep Pro', () => {

    test.beforeEach(async ({ page }) => {
        // Bloquear peticiones a la API externa para forzar el modo simulación (Mock Data)
        await page.route('**/api/**', route => route.abort());
    });

    test('Pruebas de Intrusión Básica: Acceso anónimo directo a /dashboard debe redirigir a OPAC', async ({ page }) => {
        // 1. Intentar acceder a la URL protegida /dashboard
        await page.goto('/dashboard');

        // 2. Verificar que se deniega el acceso mostrando el catálogo público OPAC
        await expect(page.locator('h2:has-text("Catálogo de Biblioteca OPAC")')).toBeVisible();

        // 3. Verificar que el panel de administración no se encuentra renderizado
        await expect(page.locator('h2:has-text("Panel de Reportes Estadísticos")')).not.toBeVisible();
    });

    test('Pruebas de Intrusión Básica: Acceso anónimo directo a /libros debe redirigir a OPAC', async ({ page }) => {
        // 1. Intentar acceder a la URL protegida /libros
        await page.goto('/libros');

        // 2. Verificar que se deniega el acceso mostrando el catálogo público OPAC
        await expect(page.locator('h2:has-text("Catálogo de Biblioteca OPAC")')).toBeVisible();

        // 3. Verificar que el formulario de catalogación no está visible
        await expect(page.locator('h2:has-text("Ingesta y Catalogación Avanzada")')).not.toBeVisible();
    });

    test('Pruebas de Intrusión Básica: Acceso anónimo directo a /prestamos debe redirigir a OPAC', async ({ page }) => {
        // 1. Intentar acceder a la URL protegida /prestamos
        await page.goto('/prestamos');

        // 2. Verificar que se deniega el acceso mostrando el catálogo público OPAC
        await expect(page.locator('h2:has-text("Catálogo de Biblioteca OPAC")')).toBeVisible();

        // 3. Verificar que el panel de préstamos no está visible
        await expect(page.locator('h2:has-text("Registrar Nuevo Préstamo")')).not.toBeVisible();
    });

    test('Los accesos rápidos administrativos no deben renderizarse en el navbar sin sesión activa', async ({ page }) => {
        // 1. Cargar el catálogo OPAC
        await page.goto('/');

        // 2. Comprobar que no hay accesos administrativos visibles
        await expect(page.locator('nav button:has-text("Reportes")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Préstamos")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Devoluciones")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Catalogación")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Lectores")')).not.toBeVisible();
    });
});
