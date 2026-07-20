import { test, expect } from '@playwright/test';

test.describe('Circulación y Gestión de Préstamos - LibriKeep Pro', () => {

    test.beforeEach(async ({ page }) => {
        // Bloquear peticiones a la API externa para forzar el modo simulación (Mock Data)
        await page.route('**/api/**', route => route.abort());

        // 1. Iniciar sesión como Administrador antes de cada prueba de circulación
        await page.goto('/');
        await page.locator('nav button:has-text("Iniciar Sesión")').click();
        await page.locator('input[autocomplete="username"]').fill('admin@librikeep.com');
        await page.locator('input[autocomplete="current-password"]').fill('PasswordSeguro123!');
        await page.locator('form button[type="submit"]').click();

        // Esperar a que el login se complete exitosamente
        await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible({ timeout: 45000 });

        // Navegar a la pantalla de Préstamos
        const navLoansButton = page.locator('nav button:has-text("Préstamos")');
        await expect(navLoansButton).toBeVisible();
        await navLoansButton.click();

        // Verificar que el formulario de préstamo esté listo
        await expect(page.locator('h2:has-text("Registrar Nuevo Préstamo")')).toBeVisible();
    });

    test('Debe registrar exitosamente un préstamo para un usuario activo', async ({ page }) => {
        // 1. Digitar el DNI de un usuario activo y seleccionar la sugerencia (María Gómez, id=5)
        const dniInput = page.locator('input[placeholder*="Escribe DNI o nombre"]');
        await dniInput.fill('77777777');
        const userSuggestion = page.locator('button:has-text("María Gómez")');
        await expect(userSuggestion).toBeVisible({ timeout: 20000 });
        await userSuggestion.click();

        // 2. Digitar el código de barras y seleccionar la sugerencia de ejemplar disponible (Clean Architecture)
        const barcodeInput = page.locator('input[placeholder*="Escribe código de barras"]');
        await barcodeInput.fill('9780134494166-C1');
        const copySuggestion = page.locator('button:has-text("9780134494166-C1")');
        await expect(copySuggestion).toBeVisible({ timeout: 20000 });
        await copySuggestion.click();

        // 3. Confirmar la transacción
        const confirmButton = page.locator('button:has-text("Confirmar Préstamo")');
        await expect(confirmButton).toBeVisible();
        await confirmButton.click();

        // 4. Validar el mensaje de éxito de la transacción
        await expect(page.locator('text=Préstamo registrado exitosamente')).toBeVisible({ timeout: 15000 });

        // 5. Validar que los campos del formulario se hayan limpiado tras el préstamo
        await expect(dniInput).toHaveValue('');
        await expect(barcodeInput).toHaveValue('');
    });

    test('Debe rechazar el préstamo mostrando alerta si el lector está bloqueado (RN-02)', async ({ page }) => {
        // 1. Digitar el DNI de un lector suspendido con deudas y seleccionar la sugerencia (Juan Pérez, id=10)
        const dniInput = page.locator('input[placeholder*="Escribe DNI o nombre"]');
        await dniInput.fill('71234567');
        const userSuggestion = page.locator('button:has-text("Juan Pérez")');
        await expect(userSuggestion).toBeVisible({ timeout: 20000 });
        await userSuggestion.click();

        // 2. Digitar el código de barras y seleccionar la sugerencia de ejemplar
        const barcodeInput = page.locator('input[placeholder*="Escribe código de barras"]');
        await barcodeInput.fill('9780134494166-C1');
        const copySuggestion = page.locator('button:has-text("9780134494166-C1")');
        await expect(copySuggestion).toBeVisible({ timeout: 20000 });
        await copySuggestion.click();

        // 3. Confirmar la transacción
        const confirmButton = page.locator('button:has-text("Confirmar Préstamo")');
        await expect(confirmButton).toBeVisible();
        await confirmButton.click();

        // 4. Validar que salte el Modal de Regla de Negocio Bloqueante (RN-02) con el código de error correspondiente
        const errorModalHeader = page.locator('h3:has-text("Infracción de Regla de Negocio")');
        await expect(errorModalHeader).toBeVisible();
        await expect(page.locator('code:has-text("ERR_USER_SANCTIONED")')).toBeVisible();

        // 5. Cerrar el modal y verificar que desaparece de pantalla
        const closeButton = page.locator('button:has-text("Entendido, Cerrar")');
        await expect(closeButton).toBeVisible();
        await closeButton.click();
        await expect(errorModalHeader).not.toBeVisible();
    });
});
