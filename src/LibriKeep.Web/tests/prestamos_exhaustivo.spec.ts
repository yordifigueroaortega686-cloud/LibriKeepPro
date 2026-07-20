import { test, expect } from '@playwright/test';

test.describe('Módulo de Préstamos y Devoluciones Exhaustivo (Reglas de Negocio) - LibriKeep Pro', () => {

    test.beforeEach(async ({ page }) => {
        // Bloquear peticiones a la API externa para forzar el modo simulación (Mock Data)
        await page.route('**/api/**', route => route.abort());
        
        // Iniciar sesión como Administrador antes de cada prueba de circulación
        await page.goto('/');
        await page.locator('nav button:has-text("Iniciar Sesión")').click();
        await page.locator('input[autocomplete="username"]').fill('admin@librikeep.com');
        await page.locator('input[autocomplete="current-password"]').fill('PasswordSeguro123!');
        await page.locator('form button[type="submit"]').click();
        await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible({ timeout: 45000 });
    });

    test('Caso Positivo: Registro de préstamo exitoso de un libro disponible a un lector activo', async ({ page }) => {
        // Navegar a la pantalla de Préstamos
        await page.locator('nav button:has-text("Préstamos")').click();
        await expect(page.locator('h2:has-text("Registrar Nuevo Préstamo")')).toBeVisible();

        // 1. Digitar DNI del lector y seleccionar sugerencia (María Gómez, id=5)
        const dniInput = page.locator('input[placeholder*="Escribe DNI o nombre"]');
        await dniInput.fill('77777777');
        const userSuggestion = page.locator('button:has-text("María Gómez")');
        await expect(userSuggestion).toBeVisible({ timeout: 20000 });
        await userSuggestion.click();

        // 2. Digitar código de barras del ejemplar y seleccionar sugerencia (Clean Architecture)
        const barcodeInput = page.locator('input[placeholder*="Escribe código de barras"]');
        await barcodeInput.fill('9780134494166-C1');
        const copySuggestion = page.locator('button:has-text("9780134494166-C1")');
        await expect(copySuggestion).toBeVisible({ timeout: 20000 });
        await copySuggestion.click();

        // 3. Confirmar Préstamo
        await page.locator('button:has-text("Confirmar Préstamo")').click();

        // 4. Validar mensaje de éxito y limpieza de campos
        await expect(page.locator('text=Préstamo registrado exitosamente')).toBeVisible({ timeout: 15000 });
        await expect(dniInput).toHaveValue('');
        await expect(barcodeInput).toHaveValue('');
    });

    test('Caso Negativo: Rechazo de préstamo de un ejemplar sin stock / no disponible (RN-01)', async ({ page }) => {
        // Navegar a la pantalla de Préstamos
        await page.locator('nav button:has-text("Préstamos")').click();
        await expect(page.locator('h2:has-text("Registrar Nuevo Préstamo")')).toBeVisible();

        // 1. Digitar DNI del lector y seleccionar sugerencia (María Gómez)
        await page.locator('input[placeholder*="Escribe DNI o nombre"]').fill('77777777');
        await page.locator('button:has-text("María Gómez")').click();

        // 2. Digitar código de barras de un ejemplar ya prestado ("9780132350884-C1" - Clean Code)
        // Como este ejemplar no está disponible, no aparece en sugerencias, por lo que escribimos el código directamente
        const barcodeInput = page.locator('input[placeholder*="Escribe código de barras"]');
        await barcodeInput.fill('9780132350884-C1');

        // 3. Confirmar Préstamo
        await page.locator('button:has-text("Confirmar Préstamo")').click();

        // 4. Validar bloqueo por regla de negocio RN-01 (Ejemplar No Disponible)
        const errorModal = page.locator('h3:has-text("Infracción de Regla de Negocio")');
        await expect(errorModal).toBeVisible();
        await expect(page.locator('code:has-text("ERR_COPY_NOT_AVAILABLE")')).toBeVisible();
        await expect(page.locator('text=no se encuentra disponible')).toBeVisible();

        // Cerrar modal
        await page.locator('button:has-text("Entendido, Cerrar")').click();
        await expect(errorModal).not.toBeVisible();
    });

    test('Caso de Negocio: Rechazo de préstamo por exceder límite de préstamos permitidos (RN-03)', async ({ page }) => {
        // Navegar a la pantalla de Préstamos
        await page.locator('nav button:has-text("Préstamos")').click();
        await expect(page.locator('h2:has-text("Registrar Nuevo Préstamo")')).toBeVisible();

        // 1. Digitar un DNI simulado (99999999) para el cual el mock activará el límite de préstamos
        const dniInput = page.locator('input[placeholder*="Escribe DNI o nombre"]');
        await dniInput.fill('99999999');
        const userSuggestion = page.locator('button:has-text("Lector Limite Excedido")');
        await expect(userSuggestion).toBeVisible({ timeout: 20000 });
        await userSuggestion.click();

        // 2. Digitar un código de barras de un ejemplar disponible
        const barcodeInput = page.locator('input[placeholder*="Escribe código de barras"]');
        await barcodeInput.fill('9780134494166-C1');
        const copySuggestion = page.locator('button:has-text("9780134494166-C1")');
        await expect(copySuggestion).toBeVisible({ timeout: 20000 });
        await copySuggestion.click();

        // 3. Confirmar Préstamo
        await page.locator('button:has-text("Confirmar Préstamo")').click();

        // 4. Validar bloqueo por regla de negocio RN-03 (Límite Excedido)
        const errorModal = page.locator('h3:has-text("Infracción de Regla de Negocio")');
        await expect(errorModal).toBeVisible();
        await expect(page.locator('code:has-text("ERR_LOAN_LIMIT_EXCEEDED")')).toBeVisible();
        await expect(page.locator('text=ha alcanzado el límite máximo')).toBeVisible();

        // Cerrar modal
        await page.locator('button:has-text("Entendido, Cerrar")').click();
        await expect(errorModal).not.toBeVisible();
    });

    test('Caso de Devolución: Retorno exitoso de un ejemplar y cambio de estado a Disponible reactivamente', async ({ page }) => {
        // 1. Navegar al módulo de Devoluciones
        await page.locator('nav button:has-text("Devoluciones")').click();
        await expect(page.locator('h2:has-text("Procesar Devolución Efectiva")')).toBeVisible();

        // 2. Seleccionar el primer préstamo activo en el dropdown (index 1, ya que 0 es el placeholder)
        const select = page.locator('select');
        await select.selectOption({ index: 1 });

        // 3. Registrar el retorno (estado Bueno por defecto)
        await page.locator('button:has-text("Registrar Retorno de Obra")').click();

        // 4. Validar mensaje de éxito de retorno
        await expect(page.locator('text=Devolución procesada y guardada en base de datos.')).toBeVisible();

        // 5. Navegar al Catálogo OPAC para validar el cambio reactivo a "Disponible"
        await page.locator('nav button:has-text("Catálogo OPAC")').click();
        await page.locator('input[placeholder*="Buscar por título, autor"]').fill('Clean Architecture');
        await page.locator('button:has-text("Buscar")').click();

        // Hacer clic en la tarjeta del libro
        await page.locator('div.cursor-pointer', { hasText: 'Clean Architecture' }).first().click();

        // Verificar en la lista de ejemplares que el C2 que devolvimos ahora es "Disponible"
        const copyStatus = page.locator('div.rounded-xl', { hasText: '9780134494166-C2' }).locator('span').last();
        await expect(copyStatus).toHaveText('Disponible');
    });
});
