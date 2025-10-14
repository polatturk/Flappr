// 🌗 Tema geçiş elemanlarını seç
const toggleBtn = document.getElementById("theme-toggle");
const themeIcon = document.getElementById("theme-icon"); // Sadece ikonu seçiyoruz

// Sayfa yüklendiğinde tema tercihini localStorage'dan al ve uygula
document.addEventListener("DOMContentLoaded", () => {
    const currentTheme = localStorage.getItem("theme");
    if (currentTheme === "light") {
        document.body.classList.remove("dark-mode");
        document.body.classList.add("light-mode");
        themeIcon.textContent = "☀️"; // Sadece ikonu değiştir
    } else {
        // Varsayılan olarak karanlık mod ve ay ikonu
        document.body.classList.add("dark-mode");
        themeIcon.textContent = "🌙";
    }
});

// Butona tıklanınca temayı değiştir
toggleBtn.addEventListener("click", () => {
    if (document.body.classList.contains("dark-mode")) {
        // Aydınlık moda geç
        document.body.classList.remove("dark-mode");
        document.body.classList.add("light-mode");
        themeIcon.textContent = "☀️"; // İkonu güneşe çevir
        localStorage.setItem("theme", "light");
    } else {
        // Karanlık moda geç
        document.body.classList.remove("light-mode");
        document.body.classList.add("dark-mode");
        themeIcon.textContent = "🌙"; // İkonu aya çevir
        localStorage.setItem("theme", "dark");
    }
});