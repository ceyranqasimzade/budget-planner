document.addEventListener("DOMContentLoaded", function () {
    const canvas = document.getElementById('expenseChart');

    if (canvas) {
        const ctx = canvas.getContext('2d');
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Qida', 'Kommunal', 'Nəqliyyat'],
                datasets: [{
                    data: [120.50, 45.00, 30.00],
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
    }
});