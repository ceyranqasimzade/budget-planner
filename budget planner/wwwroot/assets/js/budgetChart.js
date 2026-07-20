document.addEventListener("DOMContentLoaded", function () {
    var chartCanvas = document.getElementById('myBudgetChart');
    if (chartCanvas) {
        var income = parseFloat(chartCanvas.getAttribute('data-income')) || 0;
        var expense = parseFloat(chartCanvas.getAttribute('data-expense')) || 0;
        if (income === 0 && expense === 0) {
            income = 1;
            expense = 1;
        }
        new Chart(chartCanvas, {
            type: 'doughnut',
            data: {
                labels: ['Gəlir (₼)', 'Xərc (₼)'],
                datasets: [{
                    data: [income, expense],
                    backgroundColor: ['#198754', '#dc3545'], 
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }
});