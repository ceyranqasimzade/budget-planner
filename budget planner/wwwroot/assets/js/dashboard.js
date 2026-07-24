// --- 1. KART XANASINI GÖSTƏR / GİZLƏ FUNKSİYASI ---
function toggleCardField(selectElement) {
    // Form daxilindəki kart seçimi bölməsini tapırıq
    var modalBody = selectElement.closest('.modal-body');
    var cardGroup = modalBody.querySelector('.card-select-group');
    var cardSelect = cardGroup.querySelector('select[name="CardId"]');

    if (selectElement.value === 'card') {
        // "Bank Kartı" seçilərsə xananı göstər və məcburi et
        cardGroup.style.display = 'block';
        if (cardSelect) cardSelect.required = true;
    } else {
        // "Nağd pul" seçilərsə xananı gizlə və dəyərini təmizlə
        cardGroup.style.display = 'none';
        if (cardSelect) {
            cardSelect.required = false;
            cardSelect.value = ''; // Serverə null gedəcək
        }
    }
}

document.addEventListener("DOMContentLoaded", function () {

    // --- 2. MODAL BAĞLANANDA FORMANI SIFIRLAMAQ ---
    // Modal bağlandıqda kart siyahısını yenidən gizlədir və Nağd rejiminə qaytarır
    const modals = document.querySelectorAll('.modal');
    modals.forEach(modal => {
        modal.addEventListener('hidden.bs.modal', function () {
            const form = modal.querySelector('form');
            if (form) form.reset();

            const cardGroup = modal.querySelector('.card-select-group');
            if (cardGroup) cardGroup.style.display = 'none';
        });
    });

    // --- 3. DARK MODE & İKON ---
    const themeToggleBtn = document.getElementById("theme-toggle");
    const themeIcon = document.getElementById("theme-icon");
    const savedTheme = localStorage.getItem("theme");

    function updateIcon(isDark) {
        if (!themeIcon) return;

        if (isDark) {
            themeIcon.classList.remove("fa-moon", "bi-moon", "bi-moon-stars");
            themeIcon.classList.add("fa-sun", "bi-sun");
        } else {
            themeIcon.classList.remove("fa-sun", "bi-sun");
            themeIcon.classList.add("fa-moon", "bi-moon");
        }
    }

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

    // --- 4. QRAFİK DATA ---
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