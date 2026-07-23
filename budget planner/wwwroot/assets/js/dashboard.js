document.addEventListener("DOMContentLoaded", function () {

    // --- 1. DARK MODE & İKON ---
    const themeToggleBtn = document.getElementById("theme-toggle");
    const themeIcon = document.getElementById("theme-icon");
    const savedTheme = localStorage.getItem("theme");

    // İkonu təhlükəsiz şəkildə dəyişmək üçün köməkçi funksiya
    function updateIcon(isDark) {
        if (!themeIcon) return; // İkon tapılmasa xəta verməsin

        if (isDark) {
            themeIcon.classList.remove("fa-moon", "bi-moon", "bi-moon-stars");
            themeIcon.classList.add("fa-sun", "bi-sun");
        } else {
            themeIcon.classList.remove("fa-sun", "bi-sun");
            themeIcon.classList.add("fa-moon", "bi-moon");
        }
    }

    // Yüklənərkən saxlanılmış temanı tətbiq et
    if (savedTheme === "dark") {
        document.documentElement.setAttribute("data-theme", "dark");
        updateIcon(true);
    }

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener("click", function (e) {
            e.preventDefault();
            let isDark = document.documentElement.hasAttribute("data-theme");

            if (isDark) {
                document.documentElement.removeAttribute("data-theme");
                localStorage.setItem("theme", "light");
                updateIcon(false);
            } else {
                document.documentElement.setAttribute("data-theme", "dark");
                localStorage.setItem("theme", "dark");
                updateIcon(true);
            }
        });
    }

    // --- 2. QRAFİK DATA (Fetch API ilə Controller-dən çəkmək) ---
    const canvas = document.getElementById('expenseChart');
    if (canvas && typeof Chart !== 'undefined') {

        fetch('/Home/GetExpenseChartData')
            .then(response => response.json())
            .then(data => {
                const ctx = canvas.getContext('2d');
                new Chart(ctx, {
                    type: 'doughnut',
                    data: {
                        labels: data.labels,
                        datasets: [{
                            data: data.values,
                            backgroundColor: ['#ff6b6b', '#4dabf7', '#fcc419'],
                            borderWidth: 0
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        cutout: '75%',
                        plugins: { legend: { position: 'bottom' } }
                    }
                });
            })
            .catch(error => console.error("Qrafik məlumatları yüklənərkən xəta:", error));
    }
});