import { test, expect } from '@playwright/test';

test.describe('Pruebas End-to-End de Libros y Catálogo - LibriKeep Pro', () => {

  test('Carga inicial de la aplicación sin modales de error ERR_UNKNOWN ni HTTP 500', async ({ page }) => {
    // 1. Navegar a la página principal
    await page.goto('/');

    // 2. Verificar que el layout principal se renderice correctamente
    const headerTitle = page.getByRole('heading', { name: /LibriKeep/i, level: 1 });
    await expect(headerTitle).toBeVisible();
    await expect(page.getByText('PRO', { exact: true })).toBeVisible();

    // 3. Comprobar que NO existan modales de error no controlado
    const modalError = page.locator('text=Infracción de Regla de Negocio');
    await expect(modalError).not.toBeVisible();

    const errUnknown = page.locator('text=ERR_UNKNOWN');
    await expect(errUnknown).not.toBeVisible();

    const http500 = page.locator('text=HTTP 500');
    await expect(http500).not.toBeVisible();

    // 4. Confirmar que los elementos de navegación y búsqueda estén listos
    await expect(page.locator('nav').getByRole('button', { name: /Catálogo OPAC/i })).toBeVisible();
    await expect(page.getByPlaceholder(/Buscar por título, autor o ISBN/i)).toBeVisible();
  });

  test('El catálogo debe listar los libros existentes incluyendo "Cien Años de Soledad"', async ({ page }) => {
    // 1. Navegar a la raíz del catálogo
    await page.goto('/');

    // 2. Buscar "Cien Años de Soledad" en la barra de búsqueda
    const searchInput = page.getByPlaceholder(/Buscar por título, autor o ISBN/i);
    await searchInput.fill('Cien Años de Soledad');

    const searchBtn = page.getByRole('button', { name: 'Buscar' });
    await searchBtn.click();

    // 3. Verificar que aparezca en la lista de resultados de búsqueda
    await expect(page.getByRole('heading', { name: /Resultados de Búsqueda Global/i })).toBeVisible();

    const bookCard = page.locator('div.cursor-pointer', { hasText: 'Cien Años de Soledad' }).first();
    await expect(bookCard).toBeVisible();

    // 4. Validar datos del libro en la tarjeta
    await expect(bookCard.getByRole('heading', { name: 'Cien Años de Soledad' })).toBeVisible();
    await expect(bookCard.getByText('Gabriel García Márquez')).toBeVisible();
    await expect(bookCard.getByText('ISBN: 9780307474728')).toBeVisible();
    await expect(bookCard.getByText(/Disponible|Prestado/)).toBeVisible();

    // 5. Al hacer clic, validar que el panel de detalle renderice los datos completos
    await bookCard.click();
    const detailPanel = page.locator('div', { hasText: 'Detalle de Existencias Físicas' }).last();
    await expect(detailPanel).toBeVisible();
    await expect(detailPanel.getByRole('heading', { name: 'Cien Años de Soledad' })).toBeVisible();
    await expect(detailPanel.getByText('Editorial Sudamericana')).toBeVisible();
    await expect(detailPanel.getByText(/9780307474728-C1/)).toBeVisible();
  });

});
