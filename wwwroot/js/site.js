// 🌗 Tema geçiş butonu
const toggleBtn = document.getElementById("theme-toggle");

// Tercihi localStorage'dan al
const currentTheme = localStorage.getItem("theme");
if (currentTheme === "light") {
    document.body.classList.remove("dark-mode");
    document.body.classList.add("light-mode");
    toggleBtn.textContent = "☀️";
}

// Butona tıklanınca geçiş yap
toggleBtn.addEventListener("click", () => {
    if (document.body.classList.contains("dark-mode")) {
        document.body.classList.remove("dark-mode");
        document.body.classList.add("light-mode");
        toggleBtn.textContent = "☀️";
        localStorage.setItem("theme", "light");
    } else {
        document.body.classList.remove("light-mode");
        document.body.classList.add("dark-mode");
        toggleBtn.textContent = "🌙";
        localStorage.setItem("theme", "dark");
    }
});
