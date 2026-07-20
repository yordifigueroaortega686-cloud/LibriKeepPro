import { test, expect } from '@playwright/test';

test.describe('Gestión de Inventario y Catalogación - LibriKeep Pro', () => {
    test.beforeEach(async ({ page }) => {
        // Bloquear peticiones a la API externa para forzar el modo simulación (Mock Data)
        await page.route('**/api/**', route => route.abort());
    });

    test('Debe registrar un nuevo libro con múltiples ejemplares y verificarlo en el catálogo OPAC', async ({ page }) => {
        // 1. Iniciar sesión como Administrador
        await page.goto('/');
        await page.locator('nav button:has-text("Iniciar Sesión")').click();
        await page.locator('input[autocomplete="username"]').fill('admin@librikeep.com');
        await page.locator('input[autocomplete="current-password"]').fill('PasswordSeguro123!');
        await page.locator('form button[type="submit"]').click();

        // Esperar a que el login sea exitoso (arranque en frío tolerado)
        await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible({ timeout: 45000 });

        // 2. Navegar al módulo de Catalogación
        const navCatalogButton = page.locator('nav button:has-text("Catalogación")');
        await expect(navCatalogButton).toBeVisible();
        await navCatalogButton.click();

        // Verificar que estamos en la pantalla de catalogación
        await expect(page.locator('h2:has-text("Ingesta y Catalogación Avanzada")')).toBeVisible();

        // Generar un ISBN único para evitar colisiones en ejecuciones sucesivas
        const randomIsbn = '978' + Math.floor(1000000000 + Math.random() * 9000000000).toString();
        const bookTitle = `Ingeniería de Software E2E-${randomIsbn.slice(-4)}`;

        // 3. Rellenar el formulario con los metadatos de la obra
        await page.locator('input[placeholder*="ej: Clean Code"]').fill(bookTitle);
        await page.locator('input[placeholder*="ej: 978"]').fill(randomIsbn);
        await page.locator('input[placeholder*="ej: Robert C. Martin"]').fill('Roger S. Pressman');
        await page.locator('input[placeholder*="Escriba para filtrar"]').fill('Ingeniería de Software');
        
        // Seleccionar la categoría de la sugerencia del dropdown para cerrar el backdrop overlay
        const categoryOption = page.locator('button:has-text("Ingeniería de Software")').first();
        await expect(categoryOption).toBeVisible({ timeout: 10000 });
        await categoryOption.click();

        await page.locator('input[placeholder*="ej: Prentice Hall"]').fill('McGraw-Hill');

        // Incrementar el stock inicial a 2 copias físicas usando el botón "+"
        const incrementButton = page.locator('button:has-text("+")');
        await expect(incrementButton).toBeVisible();
        await incrementButton.click();

        // 4. Guardar en catálogo
        const saveButton = page.locator('button:has-text("Guardar en Catálogo")');
        await expect(saveButton).toBeVisible();
        await saveButton.click();

        // 5. Verificar mensaje de éxito
        await expect(page.locator('text=Obra ingresada correctamente')).toBeVisible({ timeout: 15000 });

        // 6. Navegar al Catálogo OPAC para verificar la existencia física de la obra recién creada
        await page.locator('nav button:has-text("Catálogo OPAC")').click();
        
        const searchInput = page.locator('input[placeholder*="Buscar por título, autor"]');
        await expect(searchInput).toBeVisible();
        await searchInput.fill(bookTitle);
        await page.locator('button:has-text("Buscar")').click();

        // Verificar que la tarjeta del libro aparece en la lista de resultados
        const bookCard = page.locator('div.cursor-pointer', { hasText: bookTitle }).first();
        await expect(bookCard).toBeVisible();

        // Hacer clic en la tarjeta del libro para desplegar las copias en el panel lateral
        await bookCard.click();

        // Verificar que se visualizan los 2 ejemplares físicos creados con sus ubicaciones
        await expect(page.locator('text=Copias físicas en Biblioteca')).toBeVisible();
        await expect(page.locator(`text=${randomIsbn}-C1`)).toBeVisible();
        await expect(page.locator(`text=${randomIsbn}-C2`)).toBeVisible();
    });
});
