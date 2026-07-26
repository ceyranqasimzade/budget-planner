document.addEventListener('DOMContentLoaded', () => {
    const themeToggleBtn = document.getElementById('theme-toggle');
    const themeIcon = document.getElementById('theme-icon');
    const htmlElement = document.documentElement;

    // Yaddaşdakı temanı oxu (yoxdursa 'light' götür)
    const savedTheme = localStorage.getItem('budget_theme') || 'light';

    // Temanı tətbiq edən funksiya
    function applyTheme(theme) {
        htmlElement.setAttribute('data-bs-theme', theme);
        htmlElement.setAttribute('data-theme', theme);
        localStorage.setItem('budget_theme', theme);

        if (themeIcon) {
            if (theme === 'dark') {
                // Qaranlıq rejimdə Günəş ikonu göstərilir
                themeIcon.className = 'bi bi-sun-fill text-warning';
            } else {
                // İşıqlı rejimdə Ay ikonu göstərilir
                themeIcon.className = 'bi bi-moon-stars-fill text-dark';
            }
        }
    }

    // Əvvəlcədən seçilmiş temanı yükle
    applyTheme(savedTheme);

    // Düyməyə kliklədikdə
    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', (e) => {
            e.preventDefault();

            const currentTheme = htmlElement.getAttribute('data-bs-theme') || 'light';
            const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';

            applyTheme(nextTheme);
        });
    }
});