document.addEventListener('DOMContentLoaded', () => {
    let themeToggleBtn = document.getElementById('theme-toggle');
    const htmlElement = document.documentElement;
    const savedTheme = localStorage.getItem('budget_theme');
    const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const initialTheme = savedTheme || (systemPrefersDark ? 'dark' : 'light');
    function setTheme(theme) {
        htmlElement.setAttribute('data-bs-theme', theme);
        htmlElement.setAttribute('data-theme', theme);
        localStorage.setItem('budget_theme', theme);

        if (themeToggleBtn) {
            if (theme === 'dark') {
                themeToggleBtn.innerHTML = '<i class="fa-solid fa-sun" style="font-size: 1.2rem;"></i>';
            } else {
                themeToggleBtn.innerHTML = '<i class="fa-solid fa-moon" style="font-size: 1.2rem;"></i>';
            }
        }
    }
    setTheme(initialTheme);
    if (themeToggleBtn) {
        const newBtn = themeToggleBtn.cloneNode(true);
        themeToggleBtn.parentNode.replaceChild(newBtn, themeToggleBtn);
        themeToggleBtn = newBtn; 
        themeToggleBtn.addEventListener('click', (e) => {
            e.preventDefault();
            const current = htmlElement.getAttribute('data-theme') || 'light';
            const next = current === 'dark' ? 'light' : 'dark';
            setTheme(next);
        });
    }
});