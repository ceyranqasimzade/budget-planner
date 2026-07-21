let myChartInstance = null;
async function renderChart() {
    const ctx = document.getElementById('myBudgetChart');
    if (!ctx) return;
    const targetCurrency = document.getElementById('chartCurrencySelector').value;
    const rawData = JSON.parse(ctx.getAttribute('data-totals') || '[]');
    let rates = {};
    try {
        const response = await fetch('https://open.er-api.com/v6/latest/AZN');
        const data = await response.json();
        rates = data.rates;
    } catch (error) {
        console.error("İnternet və ya API xətası! Standart məzənnələr istifadə olunur.", error);
        rates = { "AZN": 1, "USD": 0.588, "EUR": 0.54, "TRY": 19.0, "RUB": 54.0, "GBP": 0.46 };
    }
    let totalIncomeConverted = 0;
    let totalExpenseConverted = 0;
    rawData.forEach(item => {
        const itemCurrency = item.currency || "AZN";
        const itemRate = rates[itemCurrency] || 1;
        const targetRate = rates[targetCurrency] || 1;
        const incomeInAZN = item.income / itemRate;
        const expenseInAZN = item.expense / itemRate;
        totalIncomeConverted += incomeInAZN * targetRate;
        totalExpenseConverted += expenseInAZN * targetRate;
    });
    if (myChartInstance) {
        myChartInstance.destroy();
    }
    myChartInstance = new Chart(ctx, {
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
            scales: {
                r: { ticks: { display: false }, grid: { color: 'rgba(0, 0, 0, 0.08)' } }
            },
            plugins: {
                legend: { position: 'bottom', labels: { usePointStyle: true, padding: 15 } },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            return ` ${context.label}: ${context.raw.toFixed(2)} ${targetCurrency}`;
                        }
                    }
                }
            }
        }
    });
}
document.addEventListener("DOMContentLoaded", renderChart);