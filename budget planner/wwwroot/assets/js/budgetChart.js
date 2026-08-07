// ==========================================
// GƏLİR VƏ XƏRC QRAFİKİ (POLAR AREA + LIVE CONVERSION)
// ==========================================

let budgetChartInstance = null;

async function renderPolarBudgetChart() {
    const canvas = document.getElementById('myBudgetChart');
    if (!canvas) return;

    const currencySelector = document.getElementById('chartCurrencySelector') || document.getElementById('globalCurrency');
    const targetCurrency = currencySelector ? currencySelector.value : 'AZN';

    // JSON parse xətalarının qarşısını almaq üçün təhlükəsiz oxunma
    let rawData = [];
    try {
        rawData = JSON.parse(canvas.getAttribute('data-totals') || '[]');
    } catch (e) {
        console.error("data-totals daxilindəki JSON formatı yanlışdır:", e);
        return;
    }

    // 1. Valyuta məzənnələrinin API-dən çəkilməsi (Fallback ilə)
    let rates = {};
    try {
        const response = await fetch('https://open.er-api.com/v6/latest/AZN');
        const data = await response.json();
        rates = data.rates || {};
    } catch (error) {
        console.error("İnternet və ya API xətası! Standart məzənnələr istifadə olunur.", error);
        rates = { "AZN": 1, "USD": 0.588, "EUR": 0.54, "TRY": 19.0, "RUB": 54.0, "GBP": 0.46 };
    }

    // 2. Bütün əməliyyatların hədəf valyutaya konvertasiya olunması
    let totalIncomeConverted = 0;
    let totalExpenseConverted = 0;

    rawData.forEach(item => {
        const itemCurrency = item.currency || "AZN";
        const itemRate = rates[itemCurrency] || 1;
        const targetRate = rates[targetCurrency] || 1;

        // Əvvəlcə AZN-ə, sonra hədəf valyutaya çevrilir
        const incomeInAZN = item.income / itemRate;
        const expenseInAZN = item.expense / itemRate;

        totalIncomeConverted += incomeInAZN * targetRate;
        totalExpenseConverted += expenseInAZN * targetRate;
    });

    const ctx = canvas.getContext("2d");

    // Əvvəlki qrafik obyektini təmizləyirik (Chart instance leak qarşısını almaq üçün)
    if (budgetChartInstance) {
        budgetChartInstance.destroy();
    }

    // 3. Əgər heç bir gəlir və ya xərc yoxdursa -> Boş vəziyyət göstər
    if (totalIncomeConverted === 0 && totalExpenseConverted === 0) {
        budgetChartInstance = new Chart(ctx, {
            type: 'polarArea',
            data: {
                labels: ['Məlumat Yoxdur'],
                datasets: [{
                    data: [1],
                    backgroundColor: ['rgba(220, 224, 230, 0.6)'],
                    borderColor: ['#ced4da'],
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    r: { ticks: { display: false } }
                },
                plugins: {
                    legend: { position: 'bottom' },
                    tooltip: { enabled: false }
                }
            }
        });
        return;
    }

    // 4. Əsas Polar Area Qrafikinin qurulması
    budgetChartInstance = new Chart(ctx, {
        type: 'polarArea',
        data: {
            labels: ['Gəlir', 'Xərc'],
            datasets: [{
                data: [totalIncomeConverted, totalExpenseConverted],
                backgroundColor: ['rgba(40, 167, 69, 0.75)', 'rgba(220, 53, 69, 0.75)'],
                borderColor: ['#28a745', '#dc3545'],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
                padding: 0
            },
            scales: {
                r: {
                    ticks: { display: false },
                    grid: { color: 'rgba(0, 0, 0, 0.05)' }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        usePointStyle: true,
                        padding: 10,
                        boxWidth: 8
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            const val = context.raw.toLocaleString('az-AZ', {
                                minimumFractionDigits: 2,
                                maximumFractionDigits: 2
                            });
                            return ` ${context.label}: ${val} ${targetCurrency}`;
                        }
                    }
                }
            }
        }
    });
}

// Index.cshtml-də onchange="renderChart()" çağırıldığı üçün dublikat/alias təyin edirik
const renderChart = renderPolarBudgetChart;

// Səhifə yükləndikdə işə salınır
document.addEventListener("DOMContentLoaded", function () {
    renderPolarBudgetChart();
});