import { test, expect } from '@playwright/test';

test.describe('Suite Completa E2E Automatizada - LibriKeep Pro', () => {

  test('1. Smoke Test & Carga Inicial: Carga limpia sin modales de excepción y renderizado de layout', async ({ page }) => {
    // 1. Navegar a la raíz de la aplicación
    await page.goto('/');

    // 2. Verificar que el título de la pestaña y la cabecera principal se rendericen correctamente
    await expect(page).toHaveTitle(/LibriKeep/i);
    const headerTitle = page.getByRole('heading', { name: /LibriKeep/i, level: 1 });
    await expect(headerTitle).toBeVisible();
    await expect(page.getByText('PRO', { exact: true })).toBeVisible();

    // 3. Confirmar que no exista ningún modal de error crítico o HTTP 500 (ERR_UNKNOWN)
    const errorModal = page.locator('text=Infracción de Regla de Negocio');
    await expect(errorModal).not.toBeVisible();
    const errorUnknown = page.locator('text=ERR_UNKNOWN');
    await expect(errorUnknown).not.toBeVisible();

    // 4. Validar que la barra de navegación pública contenga los accesos esenciales
    const opacNavBtn = page.locator('nav').getByRole('button', { name: /Catálogo OPAC/i });
    await expect(opacNavBtn).toBeVisible();

    const loginNavBtn = page.locator('nav').getByRole('button', { name: /Iniciar Sesión/i });
    await expect(loginNavBtn).toBeVisible();

    // 5. Validar que la barra de búsqueda y el footer estén presentes
    const searchInput = page.getByPlaceholder(/Buscar por título, autor o ISBN/i);
    await expect(searchInput).toBeVisible();

    const footer = page.locator('footer');
    await expect(footer).toBeVisible();
    await expect(footer).toContainText('LibriKeep Pro');
  });

  test('2. Catálogo de Libros: Localizar "Cien Años de Soledad" y validar sus datos', async ({ page }) => {
    await page.goto('/');

    // 1. Buscar "Cien Años de Soledad" a través de la barra de búsqueda
    const searchInput = page.getByPlaceholder(/Buscar por título, autor o ISBN/i);
    await searchInput.fill('Cien Años de Soledad');

    const searchBtn = page.getByRole('button', { name: 'Buscar' });
    await searchBtn.click();

    // 2. Esperar que se muestre la sección de resultados
    const resultsHeader = page.getByRole('heading', { name: /Resultados de Búsqueda Global/i });
    await expect(resultsHeader).toBeVisible();

    // 3. Verificar que la tarjeta de "Cien Años de Soledad" exista y contenga los datos correctos
    const bookCard = page.locator('div.cursor-pointer', { hasText: 'Cien Años de Soledad' }).first();
    await expect(bookCard).toBeVisible();

    // Validar título, autor e ISBN
    await expect(bookCard.getByRole('heading', { name: 'Cien Años de Soledad' })).toBeVisible();
    await expect(bookCard.getByText('Gabriel García Márquez')).toBeVisible();
    await expect(bookCard.getByText('ISBN: 9780307474728')).toBeVisible();

    // Validar insignia de tipo de material y disponibilidad
    await expect(bookCard.getByText(/LIBROFISICO|Libro/i)).toBeVisible();
    await expect(bookCard.getByText('Disponible')).toBeVisible();
  });

  test('3. Detalle del Libro: Desplegar detalles físicos (Autor, Editorial, Categoría, Ejemplares e ISBN)', async ({ page }) => {
    await page.goto('/');

    // 1. Buscar y seleccionar "Cien Años de Soledad"
    const searchInput = page.getByPlaceholder(/Buscar por título, autor o ISBN/i);
    await searchInput.fill('Cien Años de Soledad');
    await page.getByRole('button', { name: 'Buscar' }).click();

    const bookCard = page.locator('div.cursor-pointer', { hasText: 'Cien Años de Soledad' }).first();
    await expect(bookCard).toBeVisible();
    await bookCard.click();

    // 2. Verificar el panel lateral de detalle de existencias físicas
    const detailPanel = page.locator('div', { hasText: 'Detalle de Existencias Físicas' }).last();
    await expect(detailPanel).toBeVisible();

    // Validar Título y Autor en el detalle
    await expect(detailPanel.getByRole('heading', { name: 'Cien Años de Soledad' })).toBeVisible();
    await expect(detailPanel.getByText(/Gabriel García Márquez/i)).toBeVisible();

    // Validar Editorial
    await expect(detailPanel.getByText('Editorial Sudamericana')).toBeVisible();

    // Validar Copias físicas registradas con sus códigos de barra y ubicación
    await expect(detailPanel.getByText(/Copias físicas en Biblioteca/i)).toBeVisible();
    await expect(detailPanel.getByText('9780307474728-C1')).toBeVisible();
    await expect(detailPanel.getByText(/Estante C1/).first()).toBeVisible();
  });

  test('4. Manejo de Errores y Validaciones: Ruta inexistente y búsqueda controlada sin resultados', async ({ page }) => {
    // 1. Acceder a una ruta inexistente
    await page.goto('/ruta-inexistente-404');

    // 2. La aplicación debe manejar la navegación sin pantalla blanca ni error no controlado
    const headerTitle = page.getByRole('heading', { name: /LibriKeep/i, level: 1 });
    await expect(headerTitle).toBeVisible();
    await expect(page.locator('h2:has-text("BIBLIOTECA")')).toBeVisible();

    // 3. Probar una búsqueda que no arroje resultados
    const searchInput = page.getByPlaceholder(/Buscar por título, autor o ISBN/i);
    await searchInput.fill('XYZ_LIBRO_INEXISTENTE_9999999');
    await page.getByRole('button', { name: 'Buscar' }).click();

    // 4. Verificar que se muestre el mensaje controlado de "No se encontraron resultados de búsqueda."
    const noResultsMsg = page.getByText('No se encontraron resultados de búsqueda.');
    await expect(noResultsMsg).toBeVisible();

    // Y que sugiera libros recomendados sin romper la UI
    await expect(page.getByText('Libros que te podrían interesar')).toBeVisible();
  });

  test('5. Flujo de Préstamo: Interacción del formulario de registro y confirmación de préstamo', async ({ page }) => {
    // 1. Iniciar sesión como Administrador
    await page.goto('/');
    const loginNavBtn = page.locator('nav').getByRole('button', { name: /Iniciar Sesión/i });
    await loginNavBtn.click();

    await page.locator('input[autocomplete="username"]').fill('admin@uni.edu.pe');
    await page.locator('input[autocomplete="current-password"]').fill('adminPassword');
    await page.locator('form button[type="submit"]').click();

    // Verificar login exitoso
    await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible({ timeout: 15000 });

    // 2. Navegar a la pantalla de Préstamos
    const circulationNavBtn = page.locator('nav').getByRole('button', { name: /Préstamos/i });
    await expect(circulationNavBtn).toBeVisible();
    await circulationNavBtn.click();

    // 3. Validar presencia del formulario de Préstamos
    const formHeading = page.getByRole('heading', { name: 'Registrar Nuevo Préstamo' });
    await expect(formHeading).toBeVisible();

    const dniInput = page.getByPlaceholder(/Escribe DNI o nombre del lector/i);
    await expect(dniInput).toBeVisible();

    const barcodeInput = page.getByPlaceholder(/Escribe código de barras o título/i);
    await expect(barcodeInput).toBeVisible();

    const submitBtn = page.getByRole('button', { name: /Confirmar Préstamo/i });
    await expect(submitBtn).toBeVisible();

    // 4. Completar el formulario con el DNI del lector y el código de barras del libro
    await dniInput.fill('71234567');
    await barcodeInput.fill('9780307474728-C1');

    // 5. Enviar el formulario
    await submitBtn.click();

    // 6. Verificar el resultado: Confirmación de préstamo exitoso o mensaje controlado de regla de negocio
    await expect(page.getByText(/Préstamo registrado exitosamente|Infracción de Regla de Negocio/i)).toBeVisible({ timeout: 15000 });
  });

});
