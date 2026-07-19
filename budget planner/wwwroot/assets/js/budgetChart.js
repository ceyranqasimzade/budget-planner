document.addEventListener("DOMContentLoaded", function () {
    const canvas = document.getElementById('myBudgetChart');
    if (!canvas) return;

    const inc = canvas.getAttribute('data-income');
    const exp = canvas.getAttribute('data-expense');

    const ctx = canvas.getContext('2d');
    new Chart(ctx, {
        type: 'polarArea',
        data: {
            labels: ['Gəlir', 'Xərc'],
            datasets: [{
                data: [inc, exp],
                backgroundColor: [
                    'rgba(46, 204, 113, 0.6)',
                    'rgba(231, 76, 60, 0.6)'
                ],
                borderColor: [
                    'rgba(46, 204, 113, 1)',
                    'rgba(231, 76, 60, 1)'
                ],
                borderWidth: 2,
                hoverBorderWidth: 4,
                hoverBackgroundColor: [
                    'rgba(46, 204, 113, 0.8)',
                    'rgba(231, 76, 60, 0.8)'
                ]
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false, 
            animation: {
                animateRotate: true,
                animateScale: true,
                duration: 1500,
                easing: 'easeOutQuart'
            },
            scales: {
                r: {
                    ticks: { display: false },
                    grid: { color: 'rgba(0, 0, 0, 0.1)' }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { font: { size: 14 }, padding: 20 }
                }
            }
        }
    });
});