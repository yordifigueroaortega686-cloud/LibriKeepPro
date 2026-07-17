import { test, expect } from '@playwright/test';

test.describe('Catálogo OPAC de LibriKeep', () => {
  test('debe buscar un libro y mostrar el resultado en pantalla', async ({ page }) => {
    // 1. Navegar a la página de inicio (Catálogo OPAC)
    await page.goto('/');

    // 2. Localizar el input de búsqueda por su placeholder
    const searchInput = page.locator('input[placeholder*="Buscar por título, autor"]');
    await expect(searchInput).toBeVisible();

    // 3. Escribir el título del libro "Clean Code"
    await searchInput.fill('Clean Code');

    // 4. Localizar el botón de buscar y hacer clic
    const searchButton = page.locator('button:has-text("Buscar")');
    await expect(searchButton).toBeVisible();
    await searchButton.click();

    // 5. Validar que la sección de resultados globales se haga visible
    const resultsHeader = page.locator('h3:has-text("Resultados de Búsqueda Global")');
    await expect(resultsHeader).toBeVisible();

    // 6. Verificar que la tarjeta correspondiente al libro buscado se renderice correctamente
    const bookCard = page.locator('div.cursor-pointer', { hasText: 'Clean Code' }).first();
    await expect(bookCard).toBeVisible();

    // 7. Validar que la tarjeta posea la paleta de colores crema (bg-card / #F5EADB) y texto marrón café muy oscuro (#3A2A1A)
    // Para ello, verificamos que tenga las clases bg-card y text-text.
    await expect(bookCard).toHaveClass(/bg-card/);
    const bookTitle = bookCard.locator('h4');
    await expect(bookTitle).toHaveClass(/text-text/);
  });
});
