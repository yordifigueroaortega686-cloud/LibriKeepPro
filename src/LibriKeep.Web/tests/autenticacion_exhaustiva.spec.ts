import { test, expect } from '@playwright/test';

test.describe('Módulo de Autenticación Exhaustivo - LibriKeep Pro', () => {

    test.beforeEach(async ({ page }) => {
        // Bloquear peticiones a la API externa para forzar el modo simulación (Mock Data)
        await page.route('**/api/**', route => route.abort());
        await page.goto('/');
        await page.locator('nav button:has-text("Iniciar Sesión")').click();
    });

    test('Caso Positivo: Debe iniciar sesión exitosamente con credenciales válidas', async ({ page }) => {
        // Llenar el formulario con credenciales válidas
        await page.locator('input[autocomplete="username"]').fill('admin@librikeep.com');
        await page.locator('input[autocomplete="current-password"]').fill('PasswordSeguro123!');
        await page.locator('form button[type="submit"]').click();

        // Verificar el acceso al Dashboard del Administrador
        await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible({ timeout: 45000 });
        await expect(page.locator('h2:has-text("Panel de Reportes Estadísticos")')).toBeVisible();
    });

    test('Caso Negativo 1: Campos vacíos deben activar la validación nativa (required)', async ({ page }) => {
        const emailInput = page.locator('input[autocomplete="username"]');
        const passwordInput = page.locator('input[autocomplete="current-password"]');

        // Verificar atributos HTML5 de validación
        await expect(emailInput).toHaveAttribute('required');
        await expect(passwordInput).toHaveAttribute('required');

        // Intentar enviar formulario vacío y comprobar que el estado de validez nativa del formulario es falso
        const form = page.locator('form');
        const isFormValid = await form.evaluate((el: HTMLFormElement) => el.checkValidity());
        expect(isFormValid).toBe(false);
    });

    test('Caso Negativo 2: Debe rechazar formato de correo inválido', async ({ page }) => {
        const emailInput = page.locator('input[autocomplete="username"]');
        await emailInput.fill('admin_error'); // Formato incorrecto sin @ ni dominio
        await page.locator('input[autocomplete="current-password"]').fill('PasswordSeguro123!');

        // Comprobar que el navegador marca el campo de email como inválido
        const isEmailValid = await emailInput.evaluate((el: HTMLInputElement) => el.validity.valid);
        expect(isEmailValid).toBe(false);
    });

    test('Caso Negativo 3: Debe mostrar error con credenciales incorrectas', async ({ page }) => {
        // Llenar con credenciales inválidas controladas por el mock
        await page.locator('input[autocomplete="username"]').fill('error@librikeep.com');
        await page.locator('input[autocomplete="current-password"]').fill('wrong-password');
        await page.locator('form button[type="submit"]').click();

        // Verificar modal de excepción de negocio / fallo de autenticación
        const errorModalHeader = page.locator('h3:has-text("Infracción de Regla de Negocio")');
        await expect(errorModalHeader).toBeVisible();
        await expect(page.locator('code:has-text("ERR_AUTH_FAILED")')).toBeVisible();
        await expect(page.locator('text=Usuario o contraseña incorrectos')).toBeVisible();

        // Cerrar modal
        await page.locator('button:has-text("Entendido, Cerrar")').click();
        await expect(errorModalHeader).not.toBeVisible();
    });

    test('Caso de Seguridad: Flujo de Cierre de Sesión y prevención de vuelta atrás', async ({ page }) => {
        // 1. Iniciar sesión exitosamente
        await page.locator('input[autocomplete="username"]').fill('admin@librikeep.com');
        await page.locator('input[autocomplete="current-password"]').fill('PasswordSeguro123!');
        await page.locator('form button[type="submit"]').click();
        await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible({ timeout: 45000 });

        // 2. Hacer clic en Cerrar Sesión
        const logoutButton = page.locator('button[title="Cerrar Sesión"]');
        await expect(logoutButton).toBeVisible();
        await logoutButton.click();

        // 3. Validar banner de éxito y redirección a OPAC
        await expect(page.locator('text=Sesión cerrada correctamente')).toBeVisible();
        await expect(page.locator('h2:has-text("Catálogo de Biblioteca OPAC")')).toBeVisible();
        await expect(logoutButton).not.toBeVisible();

        // 4. Recargar la página para simular que no persiste la sesión en almacenamiento
        await page.reload();

        // 5. Verificar que seguimos desautenticados en el catálogo OPAC y no volvemos a la vista privada
        await expect(page.locator('h2:has-text("Catálogo de Biblioteca OPAC")')).toBeVisible();
        await expect(page.locator('h2:has-text("Panel de Reportes Estadísticos")')).not.toBeVisible();
        await expect(page.locator('nav button:has-text("Iniciar Sesión")')).toBeVisible();
    });
});
