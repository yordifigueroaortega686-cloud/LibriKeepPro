import { test, expect } from '@playwright/test';

test.describe('Módulo de Inventario Exhaustivo (Catalogación y OPAC) - LibriKeep Pro', () => {

    test.beforeEach(async ({ page }) => {
        // Bloquear peticiones a la API externa para forzar el modo simulación (Mock Data)
        await page.route('**/api/**', route => route.abort());
        
        // Iniciar sesión como Administrador
        await page.goto('/');
        await page.locator('nav button:has-text("Iniciar Sesión")').click();
        await page.locator('input[autocomplete="username"]').fill('admin@librikeep.com');
        await page.locator('input[autocomplete="current-password"]').fill('PasswordSeguro123!');
        await page.locator('form button[type="submit"]').click();
        await expect(page.getByText('Admin Principal', { exact: true })).toBeVisible({ timeout: 45000 });

        // Navegar a la sección de Catalogación
        const navCatalogButton = page.locator('nav button:has-text("Catalogación")');
        await expect(navCatalogButton).toBeVisible();
        await navCatalogButton.click();
        await expect(page.locator('h2:has-text("Ingesta y Catalogación Avanzada")')).toBeVisible();
    });

    test('Caso Positivo: Registro exitoso de una obra con múltiples ejemplares y verificación en OPAC', async ({ page }) => {
        const randomIsbn = '978' + Math.floor(1000000000 + Math.random() * 9000000000).toString();
        const bookTitle = `Ingeniería de Software E2E-${randomIsbn.slice(-4)}`;

        // Rellenar el formulario
        await page.locator('input[placeholder*="ej: Clean Code"]').fill(bookTitle);
        await page.locator('input[placeholder*="ej: 978"]').fill(randomIsbn);
        await page.locator('input[placeholder*="ej: Robert C. Martin"]').fill('Roger S. Pressman');
        await page.locator('input[placeholder*="Escriba para filtrar"]').fill('Ingeniería de Software');

        // Seleccionar la categoría del dropdown para cerrar el backdrop overlay
        const categoryOption = page.locator('button:has-text("Ingeniería de Software")').first();
        await expect(categoryOption).toBeVisible();
        await categoryOption.click();

        await page.locator('input[placeholder*="ej: Prentice Hall"]').fill('McGraw-Hill');

        // Incrementar copias físicas a 2
        await page.locator('button:has-text("+")').click();

        // Guardar obra
        await page.locator('button:has-text("Guardar en Catálogo")').click();

        // Verificar mensaje de éxito
        await expect(page.locator('text=Obra ingresada correctamente')).toBeVisible({ timeout: 15000 });

        // Verificar reactividad en el catálogo OPAC
        await page.locator('nav button:has-text("Catálogo OPAC")').click();
        const searchInput = page.locator('input[placeholder*="Buscar por título, autor"]');
        await expect(searchInput).toBeVisible();
        await searchInput.fill(bookTitle);
        await page.locator('button:has-text("Buscar")').click();

        // Hacer clic en la tarjeta del libro
        const bookCard = page.locator('div.cursor-pointer', { hasText: bookTitle }).first();
        await expect(bookCard).toBeVisible();
        await bookCard.click();

        // Comprobar la visualización de los ejemplares físicos asignados en el panel lateral
        await expect(page.locator('text=Copias físicas en Biblioteca')).toBeVisible();
        await expect(page.locator(`text=${randomIsbn}-C1`)).toBeVisible();
        await expect(page.locator(`text=${randomIsbn}-C2`)).toBeVisible();
    });

    test('Caso Negativo: Validación de campos obligatorios en el formulario de catalogación', async ({ page }) => {
        const titleInput = page.locator('input[placeholder*="ej: Clean Code"]');
        const isbnInput = page.locator('input[placeholder*="ej: 978"]');
        const authorInput = page.locator('input[placeholder*="ej: Robert C. Martin"]');

        // Verificar que los inputs clave tienen el atributo 'required'
        await expect(titleInput).toHaveAttribute('required');
        await expect(isbnInput).toHaveAttribute('required');
        await expect(authorInput).toHaveAttribute('required');

        // Intentar enviar formulario vacío y validar que falle la validación HTML5
        const form = page.locator('form');
        const isFormValid = await form.evaluate((el: HTMLFormElement) => el.checkValidity());
        expect(isFormValid).toBe(false);
    });

    test('Caso Límite: Rechazo de ISBN con formato incorrecto o caracteres alfabéticos (RN-1.3)', async ({ page }) => {
        // Rellenar formulario con ISBN inválido (letras)
        await page.locator('input[placeholder*="ej: Clean Code"]').fill('Libro Invalido ISBN');
        await page.locator('input[placeholder*="ej: 978"]').fill('ISBN-INVALIDO-CHARS');
        await page.locator('input[placeholder*="ej: Robert C. Martin"]').fill('Autor Ficticio');
        await page.locator('input[placeholder*="ej: Prentice Hall"]').fill('Editorial Ficticia');
        await page.locator('button:has-text("Guardar en Catálogo")').click();

        // Verificar que salta el Modal de Infracción de Regla de Negocio (ISBN Inválido)
        const errorModal = page.locator('h3:has-text("Infracción de Regla de Negocio")');
        await expect(errorModal).toBeVisible();
        await expect(page.locator('code:has-text("ERR_INVALID_ISBN")')).toBeVisible();
        await expect(page.locator('text=El formato del ISBN ingresado no es válido')).toBeVisible();

        // Cerrar el modal
        await page.locator('button:has-text("Entendido, Cerrar")').click();
        await expect(errorModal).not.toBeVisible();
    });

    test.skip('Flujo de Modificación: Editar stock y reflejarlo en la tabla (Requiere implementación en el UI)', async ({ page }) => {
        // NOTA DE QA STAFF: Este test se marca como skipped (omitido) debido a que la interfaz actual de 
        // LibriKeep Pro (App.tsx) no cuenta con controles en el UI de catalogación o libros para editar existencias 
        // de libros registrados. Una vez implementada la vista de "Administrar Inventario", este test debe completarse.
    });

    test.skip('Flujo de Eliminación/Baja: Dar de baja un libro con confirmación (Requiere implementación en el UI)', async ({ page }) => {
        // NOTA DE QA STAFF: Este test se marca como skipped (omitido) debido a que la interfaz actual de 
        // LibriKeep Pro (App.tsx) no cuenta con controles en el UI para la eliminación o dar de baja libros 
        // o ejemplares. Una vez implementados los botones y modal de baja, este test debe activarse.
    });
});
