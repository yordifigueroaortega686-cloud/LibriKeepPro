/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}"
  ],
  theme: {
    extend: {
      colors: {
        primary: "#B27052", // Botón principal Terracota
        secondary: "#C29C6C", // Botón secundario Arena tostado
        background: "#1D5C6A", // Fondo de la aplicación (Azul Océano apagado)
        card: "#F5EADB", // Tarjetas y paneles (Crema pálido suave)
        border: "#DEC7A5", // Bordes sutiles (Arena tostado)
        text: "#3A2A1A", // Texto sobre crema (Marrón café oscuro)
        textOnBg: "#F5EADB", // Texto sobre azul (crema)
        tableEven: "#F5EADB",
        tableOdd: "#EADCC9",
        placeholder: "#7D6A56"
      }
    }
  },
  plugins: []
};
