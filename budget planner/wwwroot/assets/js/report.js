// wwwroot/assets/js/report.js

function initReportCharts(data) {
    // Mövcud mövzunun (Light/Dark) mətn rəngini təyin edək (həm Bootstrap data-bs-theme, həm də data-theme dəstəklənir)
    const isDarkMode = document.documentElement.getAttribute('data-bs-theme') === 'dark' ||
        document.documentElement.getAttribute('data-theme') === 'dark' ||
        document.body.classList.contains('dark-mode') ||
        document.body.classList.contains('dark');

    const textColor = isDarkMode ? '#94a3b8' : '#4b5563';
    const gridColor = isDarkMode ? 'rgba(255, 255, 255, 0.08)' : 'rgba(0, 0, 0, 0.06)';

    // Global Chart.js Font və Tor (Grid) Rəngləri
    Chart.defaults.color = textColor;
    Chart.defaults.borderColor = gridColor;

    // 1. Maliyyə Sağlamlığı Gauge Chart
    const healthScore = Number(data?.kpi?.healthScore) || 0;
    const scoreColor = healthScore < 40 ? '#ef4444' : healthScore < 70 ? '#f59e0b' : '#10b981';

    const scoreTextElem = document.getElementById('scoreText');
    if (scoreTextElem) {
        scoreTextElem.style.color = scoreColor;
    }

    const healthCtx = document.getElementById('healthGaugeChart');
    if (healthCtx) {
        new Chart(healthCtx, {
            type: 'doughnut',
            data: {
                datasets: [{
                    data: [healthScore, 100 - healthScore],
                    backgroundColor: [scoreColor, isDarkMode ? '#2b2d32' : '#e5e7eb'],
                    borderWidth: 0
                }]
            },
            options: {
                rotation: -90,
                circumference: 180,
                cutout: '78%',
                responsive: true,
                maintainAspectRatio: false, // Sonsuz uzanmanın qarşısını alır
                plugins: { tooltip: { enabled: false } }
            }
        });
    }

    // 2. 6 Aylıq Trend Chart
    const trendCtx = document.getElementById('trendChart');
    if (trendCtx) {
        new Chart(trendCtx, {
            type: 'line',
            data: {
                labels: (data?.trend?.labels && data.trend.labels.length) ? data.trend.labels : ['Yan', 'Fev', 'Mar', 'Apr', 'May', 'İyun'],
                datasets: [
                    {
                        label: 'Gəlir',
                        data: (data?.trend?.income && data.trend.income.length) ? data.trend.income : [0, 0, 0, 0, 0, 0],
                        borderColor: '#10b981',
                        backgroundColor: 'rgba(16, 185, 129, 0.12)',
                        fill: true,
                        tension: 0.35,
                        pointRadius: 4
                    },
                    {
                        label: 'Xərc',
                        data: (data?.trend?.expense && data.trend.expense.length) ? data.trend.expense : [0, 0, 0, 0, 0, 0],
                        borderColor: '#ef4444',
                        backgroundColor: 'rgba(239, 68, 68, 0.12)',
                        fill: true,
                        tension: 0.35,
                        pointRadius: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false, // Sonsuz uzanmanın qarşısını alır
                plugins: {
                    legend: { position: 'top', labels: { boxWidth: 12 } }
                },
                scales: {
                    x: { ticks: { color: textColor }, grid: { color: gridColor } },
                    y: { ticks: { color: textColor }, grid: { color: gridColor } }
                }
            }
        });
    }

    // 3. 50/30/20 Qaydası Chart
    const totalIncome = Number(data?.kpi?.monthlyIncome) || 100;
    const totalExpense = Number(data?.kpi?.monthlyExpense) || 0;
    const needs = totalExpense * 0.5;
    const wants = totalExpense * 0.3;
    const savings = Math.max(0, totalIncome - totalExpense);

    const ruleCtx = document.getElementById('rule503020Chart');
    if (ruleCtx) {
        new Chart(ruleCtx, {
            type: 'doughnut',
            data: {
                labels: ['Ehtiyaclar (50%)', 'İstəklər (30%)', 'Yığım (20%)'],
                datasets: [{
                    data: (totalIncome === 0 && totalExpense === 0) ? [50, 30, 20] : [needs, wants, savings],
                    backgroundColor: ['#3b82f6', '#f59e0b', '#10b981'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false, // Sonsuz uzanmanın qarşısını alır
                plugins: { legend: { display: false } }
            }
        });
    }

    // 4. Kateqoriya Bölgüsü Chart
    const catCtx = document.getElementById('categoryChart');
    if (catCtx) {
        const hasCatData = data?.categories?.expenses && data.categories.expenses.length > 0;
        new Chart(catCtx, {
            type: 'pie',
            data: {
                labels: hasCatData ? data.categories.labels : ['Məlumat Yoxdur'],
                datasets: [{
                    data: hasCatData ? data.categories.expenses : [1],
                    backgroundColor: hasCatData
                        ? ['#ec4899', '#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#64748b']
                        : [isDarkMode ? '#374151' : '#e5e7eb'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false, // Sonsuz uzanmanın qarşısını alır
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 10 } }
                }
            }
        });
    }

    // 5. Həftəlik Xərc Chart
    const weekdayCtx = document.getElementById('weekdayChart');
    if (weekdayCtx) {
        new Chart(weekdayCtx, {
            type: 'bar',
            data: {
                labels: (data?.weekdays?.labels && data.weekdays.labels.length) ? data.weekdays.labels : ['B.E', 'Ç.Ə', 'Ç', 'C.A', 'C', 'Ş', 'B'],
                datasets: [{
                    label: 'Xərc',
                    data: (data?.weekdays?.expenses && data.weekdays.expenses.length) ? data.weekdays.expenses : [0, 0, 0, 0, 0, 0, 0],
                    backgroundColor: '#6366f1',
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false, // Sonsuz uzanmanın qarşısını alır
                plugins: { legend: { display: false } },
                scales: {
                    x: { ticks: { color: textColor }, grid: { display: false } },
                    y: { ticks: { color: textColor }, grid: { color: gridColor } }
                }
            }
        });
    }
}
document.addEventListener("DOMContentLoaded", function () {
    // 2-ci şəkildəki kimi Azərbaycan dilli, gözəl açılan təqvimi aktivləşdirir
    flatpickr(".date-picker-input", {
        locale: "az",               // Azərbaycan dili (B.e., Ç.ə., İyul və s.)
        dateFormat: "d.m.Y",        // GG.AA.İİİİ formatı
        allowInput: true,
        disableMobile: "true",      // Mobil cihazlarda da eyni gözəl təqvim açılsın
        theme: "material_green"     // Yaşıl-teal rəng konsepti
    });
});