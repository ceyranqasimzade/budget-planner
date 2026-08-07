let reportChartInstances = {};

function normalizeReportData(rawData) {
    if (!rawData) return {};

    function getProp(obj, ...keys) {
        if (!obj) return null;
        for (let key of keys) {
            if (obj[key] !== undefined && obj[key] !== null) return obj[key];
        }
        return null;
    }

    const kpiObj = getProp(rawData, 'kpi', 'Kpi') || rawData;
    const trendObj = getProp(rawData, 'trend', 'Trend') || rawData;
    const catObj = getProp(rawData, 'categories', 'Categories') || rawData;
    const weekObj = getProp(rawData, 'weekdays', 'Weekdays') || rawData;

    const monthlyExp = Number(getProp(kpiObj, 'monthlyExpense', 'MonthlyExpense')) || 0;
    const monthlyInc = Number(getProp(kpiObj, 'monthlyIncome', 'MonthlyIncome')) || 0;

    let rawCatLabels = getProp(catObj, 'categoryNames', 'CategoryNames') || [];
    let catExpenses = getProp(catObj, 'categoryExpenses', 'CategoryExpenses') || [];
    const topCats = getProp(catObj, 'topCategories', 'TopCategories');

    if ((!rawCatLabels || rawCatLabels.length === 0) && Array.isArray(topCats) && topCats.length > 0) {
        rawCatLabels = topCats.map(c => getProp(c, 'categoryName', 'CategoryName') || 'Kateqoriyasız');
        catExpenses = topCats.map(c => Number(getProp(c, 'amount', 'Amount')) || 0);
    }

    const cleanedCatLabels = rawCatLabels.map(name => {
        if (!name || name.trim() === "" || name.trim().toLowerCase() === "qq") {
            return "Kateqoriyasız";
        }
        return name;
    });

    return {
        kpi: {
            healthScore: Number(getProp(kpiObj, 'healthScore', 'HealthScore')) || 0,
            monthlyIncome: monthlyInc,
            monthlyExpense: monthlyExp,
            monthlySavings: Number(getProp(kpiObj, 'monthlySavings', 'MonthlySavings')) || (monthlyInc - monthlyExp)
        },
        rule503020: {
            needs: Number(getProp(kpiObj, 'needsAmount', 'NeedsAmount')) || (monthlyExp * 0.5),
            wants: Number(getProp(kpiObj, 'wantsAmount', 'WantsAmount')) || (monthlyExp * 0.3),
            savings: Number(getProp(kpiObj, 'savingsAmount', 'SavingsAmount')) || (monthlyExp * 0.2)
        },
        trend: {
            labels: Array.isArray(getProp(trendObj, 'monthlyLabels', 'MonthlyLabels')) ? getProp(trendObj, 'monthlyLabels', 'MonthlyLabels') : [],
            income: Array.isArray(getProp(trendObj, 'monthlyIncomeData', 'MonthlyIncomeData')) ? getProp(trendObj, 'monthlyIncomeData', 'MonthlyIncomeData') : [],
            expense: Array.isArray(getProp(trendObj, 'monthlyExpenseData', 'MonthlyExpenseData')) ? getProp(trendObj, 'monthlyExpenseData', 'MonthlyExpenseData') : []
        },
        categories: {
            labels: cleanedCatLabels,
            expenses: Array.isArray(catExpenses) ? catExpenses.map(v => Number(v) || 0) : [],
            topCategories: topCats || [] // 🟢 Top Kateqoriyalar buraya ötürüldü
        },
        weekdays: {
            labels: Array.isArray(getProp(weekObj, 'dayNames', 'DayNames')) ? getProp(weekObj, 'dayNames', 'DayNames') : [],
            expenses: Array.isArray(getProp(weekObj, 'dayExpenses', 'DayExpenses')) ? getProp(weekObj, 'dayExpenses', 'DayExpenses') : []
        }
    };
}

function updateKpiCards(kpiData, currencySymbol = "₼") {
    if (!kpiData) return;

    const incomeEl = document.getElementById("kpiMonthlyIncome");
    const expenseEl = document.getElementById("kpiMonthlyExpense");
    const savingsEl = document.getElementById("kpiMonthlySavings");

    const inc = Number(kpiData.monthlyIncome) || 0;
    const exp = Number(kpiData.monthlyExpense) || 0;
    const sav = Number(kpiData.monthlySavings) || 0;

    if (incomeEl) incomeEl.innerText = `${inc.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} ${currencySymbol}`;
    if (expenseEl) expenseEl.innerText = `${exp.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} ${currencySymbol}`;
    if (savingsEl) savingsEl.innerText = `${sav.toLocaleString('az-AZ', { minimumFractionDigits: 2 })} ${currencySymbol}`;
}

function initReportCharts(rawData, selectedCurrency = "AZN") {
    console.log("📊 Gələn Data:", rawData); // Yoxlamaq üçün log

    const data = normalizeReportData(rawData);

    const currencySymbols = {
        'AZN': '₼', 'USD': '$', 'EUR': '€', 'TRY': '₺',
        'RUB': '₽', 'GBP': '£', 'CHF': 'CHF', 'CAD': 'CA$',
        'AUD': 'A$', 'CNY': 'CN¥', 'JPY': '¥'
    };
    const symbol = currencySymbols[selectedCurrency?.toUpperCase()] || selectedCurrency;

    if (data.kpi) {
        updateKpiCards(data.kpi, symbol);
    }

    const isDarkMode = document.documentElement.getAttribute('data-bs-theme') === 'dark' ||
        document.body.classList.contains('dark-mode');

    const textColor = isDarkMode ? '#94a3b8' : '#4b5563';
    const gridColor = isDarkMode ? 'rgba(255, 255, 255, 0.08)' : 'rgba(0, 0, 0, 0.06)';

    if (typeof Chart !== "undefined") {
        Chart.defaults.color = textColor;
        Chart.defaults.borderColor = gridColor;
    }

    function createOrUpdateChart(canvasId, config) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        if (reportChartInstances[canvasId]) {
            reportChartInstances[canvasId].destroy();
        }
        reportChartInstances[canvasId] = new Chart(ctx, config);
    }

    // 1. Health Gauge
    const healthScore = Number(data?.kpi?.healthScore) || 0;
    const scoreColor = healthScore < 40 ? '#ef4444' : healthScore < 70 ? '#f59e0b' : '#10b981';

    const scoreTextElem = document.getElementById('scoreText');
    if (scoreTextElem) {
        scoreTextElem.innerText = healthScore;
        scoreTextElem.style.color = scoreColor;
    }

    createOrUpdateChart('healthGaugeChart', {
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
            maintainAspectRatio: false,
            plugins: { tooltip: { enabled: false } }
        }
    });

    // 2. Trend Chart
    createOrUpdateChart('trendChart', {
        type: 'line',
        data: {
            labels: data?.trend?.labels?.length ? data.trend.labels : ['Yan', 'Fev', 'Mar', 'Apr', 'May', 'İyun'],
            datasets: [
                {
                    label: `Gəlir (${symbol})`,
                    data: data?.trend?.income?.length ? data.trend.income : [0, 0, 0, 0, 0, 0],
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.12)',
                    fill: true,
                    tension: 0.35
                },
                {
                    label: `Xərc (${symbol})`,
                    data: data?.trend?.expense?.length ? data.trend.expense : [0, 0, 0, 0, 0, 0],
                    borderColor: '#ef4444',
                    backgroundColor: 'rgba(239, 68, 68, 0.12)',
                    fill: true,
                    tension: 0.35
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'top' } }
        }
    });

    // 3. Rule 50/30/20
    const needs = Number(data?.rule503020?.needs) || 0;
    const wants = Number(data?.rule503020?.wants) || 0;
    const savings = Number(data?.rule503020?.savings) || 0;

    createOrUpdateChart('rule503020Chart', {
        type: 'doughnut',
        data: {
            labels: ['Ehtiyaclar (50%)', 'İstəklər (30%)', 'Yığım (20%)'],
            datasets: [{
                data: (needs === 0 && wants === 0 && savings === 0) ? [50, 30, 20] : [needs, wants, savings],
                backgroundColor: ['#3b82f6', '#f59e0b', '#10b981'],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } }
        }
    });

    // 4. Category Chart
    const hasCatData = data?.categories?.expenses && data.categories.expenses.length > 0;
    createOrUpdateChart('categoryChart', {
        type: 'pie',
        data: {
            labels: hasCatData ? data.categories.labels : ['Məlumat Yoxdur'],
            datasets: [{
                data: hasCatData ? data.categories.expenses : [1],
                backgroundColor: hasCatData ? ['#ec4899', '#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#64748b'] : ['#e5e7eb'],
                borderWidth: 0
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'bottom' } }
        }
    });

    // 5. Weekday Chart
    createOrUpdateChart('weekdayChart', {
        type: 'bar',
        data: {
            labels: data?.weekdays?.labels?.length ? data.weekdays.labels : ['B.E', 'Ç.Ə', 'Ç', 'C.A', 'C', 'Ş', 'B'],
            datasets: [{
                label: `Xərc (${symbol})`,
                data: data?.weekdays?.expenses?.length ? data.weekdays.expenses : [0, 0, 0, 0, 0, 0, 0],
                backgroundColor: '#6366f1',
                borderRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } }
        }
    });

    // 🟢 Top Xərc Kateqoriyalarının siyahısını yeniləyirik:
    if (data.categories && data.categories.topCategories) {
        renderTopCategories(data.categories.topCategories, symbol);
    }
}

// Data çəkmək üçün köməkçi funksiya
function fetchReportData() {
    const startDate = document.getElementById("startDate")?.value || "";
    const endDate = document.getElementById("endDate")?.value || "";
    const currency = document.getElementById("currencySelect")?.value || "AZN";
    const applyBtn = document.getElementById("applyFilterBtn");

    if (applyBtn) {
        applyBtn.disabled = true;
        applyBtn.innerHTML = `<span class="spinner-border spinner-border-sm me-2"></span> Yüklənir...`;
    }

    fetch(`/Report/GetReportData?startDate=${encodeURIComponent(startDate)}&endDate=${encodeURIComponent(endDate)}&displayCurrency=${encodeURIComponent(currency)}`, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(res => res.json())
        .then(resData => initReportCharts(resData, currency))
        .catch(err => console.error("Filtr xətası:", err))
        .finally(() => {
            if (applyBtn) {
                applyBtn.disabled = false;
                applyBtn.innerHTML = `<i class="bi bi-funnel-fill"></i> Tətbiq Et`;
            }
        });
}

// Event Listeners
document.addEventListener("DOMContentLoaded", function () {
    if (typeof flatpickr !== "undefined") {
        flatpickr(".date-picker-input", {
            locale: "az",
            dateFormat: "Y-m-d",
            allowInput: true
        });
    }

    // 🟢 Səhifə açılan kimi dataları çəkirik:
    fetchReportData();

    // 🟢 Filtr düyməsinə klikləyəndə dataları çəkirik:
    const applyBtn = document.getElementById("applyFilterBtn");
    if (applyBtn) {
        applyBtn.addEventListener("click", fetchReportData);
    }

    // 🟢 Valyuta seçimi dəyişdikdə də avtomatik sorğu göndərilir:
    const currencySelect = document.getElementById("currencySelect");
    if (currencySelect) {
        currencySelect.addEventListener("change", fetchReportData);
    }
});

function renderTopCategories(topCats, currencySymbol = "AZN") {
    const container = document.getElementById("topCategoriesList");
    if (!container) return;

    if (!topCats || !Array.isArray(topCats) || topCats.length === 0) {
        container.innerHTML = `<p class="text-muted text-center py-4">Bu ay üçün heç bir xərc məlumatı tapılmadı.</p>`;
        return;
    }

    let html = "";
    topCats.forEach(cat => {
        let rawName = cat.categoryName || cat.CategoryName;
        const name = (!rawName || rawName.trim() === "" || rawName.trim().toLowerCase() === "qq") ? "Kateqoriyasız" : rawName;
        const amount = Number(cat.amount || cat.Amount || 0).toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        const percentage = Number(cat.percentage || cat.Percentage || 0).toFixed(1);

        html += `
            <div class="mb-3">
                <div class="d-flex justify-content-between small mb-1">
                    <span class="fw-semibold">${name}</span>
                    <span class="fw-bold">${amount} ${currencySymbol} (${percentage}%)</span>
                </div>
                <div class="progress" style="height: 6px;">
                    <div class="progress-bar bg-success" role="progressbar" style="width: ${percentage}%"></div>
                </div>
            </div>
        `;
    });

    container.innerHTML = html;
}