import { test, expect } from '@playwright/test';

test.describe('Control de Accesos y Seguridad - LibriKeep Pro', () => {
    test.beforeEach(async ({ page }) => {
        // Bloquear peticiones a la API externa para forzar el modo simulación (Mock Data)
        await page.route('**/api/**', route => route.abort());
    });

    test('No debe permitir el acceso directo a vistas protegidas sin sesión activa y debe mostrar el OPAC', async ({ page }) => {
        // 1. Intentar acceder directamente a la URL de Dashboard
        await page.goto('/dashboard');

        // 2. Verificar que se muestra la interfaz del catálogo OPAC (vista pública por defecto)
        const opacHeader = page.locator('h2:has-text("Catálogo de Biblioteca OPAC")');
        await expect(opacHeader).toBeVisible();

        // 3. Confirmar que el panel de administración NO está visible
        const dashboardHeader = page.locator('h2:has-text("Panel de Reportes Estadísticos")');
        await expect(dashboardHeader).not.toBeVisible();

        // 4. Confirmar que la barra de navegación muestra el botón para iniciar sesión (sesión inactiva)
        const loginBtn = page.locator('nav button:has-text("Iniciar Sesión")');
        await expect(loginBtn).toBeVisible();
    });

    test('No debe mostrar los botones administrativos en la barra de navegación sin autenticación', async ({ page }) => {
        // 1. Ir a la raíz
        await page.goto('/');

        // 2. Verificar que los accesos rápidos a módulos administrativos no se renderizan
        await expect(page.locator('nav button:has-text("Reportes")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Préstamos")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Devoluciones")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Catalogación")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Lectores")')).not.toBeVisible();
    });
});
