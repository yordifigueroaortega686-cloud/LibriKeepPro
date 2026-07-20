import { test, expect } from '@playwright/test';

test.describe('Módulo de Autenticación - LibriKeep Pro', () => {
    test.beforeEach(async ({ page }) => {
        // Bloquear peticiones a la API externa para forzar el modo simulación (Mock Data)
        await page.route('**/api/**', route => route.abort());
    });

    test('Debe iniciar sesión exitosamente con credenciales válidas', async ({ page }) => {
        // 1. Navegar a la página de inicio (Catálogo OPAC)
        await page.goto('/');

        // 2. Hacer clic en el botón "Iniciar Sesión" de la barra de navegación para mostrar el formulario
        const navLoginButton = page.locator('nav button:has-text("Iniciar Sesión")');
        await expect(navLoginButton).toBeVisible();
        await navLoginButton.click();

        // 3. Llenar el formulario usando los selectores de autocompletado seguros
        await page.locator('input[autocomplete="username"]').fill('admin@librikeep.com');
        await page.locator('input[autocomplete="current-password"]').fill('PasswordSeguro123!');

        // 4. Hacer clic en el botón para ingresar (submit del formulario)
        const submitButton = page.locator('form button[type="submit"]');
        await expect(submitButton).toBeVisible();
        await submitButton.click();

        // 5. Esperar a que el inicio de sesión sea exitoso (aparece el nombre del administrador en la cabecera)
        // Se usa un timeout extendido para tolerar el "cold start" (arranque en frío) de la API alojada en Render al validar las credenciales
        await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible({ timeout: 45000 });
 
        // 6. Esperar a que desaparezca el mensaje de carga (procesando cubos analíticos)
        const loadingIndicator = page.locator('text=Procesando cubos analíticos...');
        await expect(loadingIndicator).toBeHidden({ timeout: 15000 });
 
        // 7. Verificar que el sistema muestra la interfaz de Reportes del Dashboard principal
        const dashboardHeader = page.locator('h2:has-text("Panel de Reportes Estadísticos")');
        await expect(dashboardHeader).toBeVisible();
 
        // 8. Verificar que aparece el nombre del usuario administrador en la cabecera
        await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible();
 
        // 9. La URL sigue siendo la raíz '/' ya que es una SPA gestionada por estado React
        await expect(page).toHaveURL('/');
    });
});
