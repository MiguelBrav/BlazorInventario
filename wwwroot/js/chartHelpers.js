// Global variable to track if Chart.js is loaded
let chartJsReady = typeof Chart !== 'undefined';

// Check for Chart.js periodically
if (!chartJsReady) {
    const checkChart = setInterval(() => {
        if (typeof Chart !== 'undefined') {
            chartJsReady = true;
            console.log('Chart.js is now ready');
            clearInterval(checkChart);
        }
    }, 100);
}

const waitForChart = (maxWait = 5000) => {
    return new Promise((resolve, reject) => {
        let waited = 0;
        const interval = 100;
        const checkChart = setInterval(() => {
            if (typeof Chart !== 'undefined') {
                clearInterval(checkChart);
                console.log('Chart.js loaded successfully');
                resolve();
            } else if (waited >= maxWait) {
                clearInterval(checkChart);
                reject(new Error('Chart.js did not load within ' + maxWait + 'ms'));
            }
            waited += interval;
        }, interval);
    });
};

window.renderMonthlyMovementsChart = async (canvasId, labels, data) => {
    console.log('renderMonthlyMovementsChart called');
    console.log('  canvasId:', canvasId);
    console.log('  labels:', labels);
    console.log('  data:', data);

    try {
        await waitForChart();

        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return;
        }

        if (window._monthlyChartInstance) {
            try {
                window._monthlyChartInstance.destroy();
            } catch (e) {
                console.warn('Error destroying previous chart:', e);
            }
            window._monthlyChartInstance = null;
        }

        // Asegúrate de que data es un array de números
        const chartData = Array.isArray(data) ? data : [];
        console.log('Chart data (converted):', chartData);

        window._monthlyChartInstance = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels || [],
                datasets: [
                    {
                        label: 'Total Movimientos',
                        data: chartData,
                        backgroundColor: 'rgba(54, 162, 235, 0.8)',
                        borderColor: 'rgba(54, 162, 235, 1)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        min: 0
                    }
                }
            }
        });

        console.log('Monthly movements chart rendered successfully');
    } catch (e) {
        console.error('renderMonthlyMovementsChart error:', e);
        console.error('Stack trace:', e.stack);
    }
};

window.renderEntriesVsExitsChart = async (canvasId, labels, entriesData, exitsData) => {
    console.log('renderEntriesVsExitsChart called with:', { canvasId, labelsCount: labels?.length, entriesCount: entriesData?.length, exitsCount: exitsData?.length });

    try {
        await waitForChart();
        console.log('Chart.js ready, finding canvas:', canvasId);

        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return;
        }

        console.log('Canvas found, destroying previous instance if exists');
        if (window._entriesExitsChartInstance) {
            try {
                window._entriesExitsChartInstance.destroy();
            } catch (e) {
                console.warn('Error destroying previous chart:', e);
            }
            window._entriesExitsChartInstance = null;
        }

        console.log('Creating new chart with labels:', labels);

        window._entriesExitsChartInstance = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels || [],
                datasets: [
                    {
                        label: 'Entradas',
                        data: entriesData || [],
                        backgroundColor: 'rgba(75, 192, 75, 0.8)',
                        borderColor: 'rgba(75, 192, 75, 1)',
                        borderWidth: 1
                    },
                    {
                        label: 'Salidas',
                        data: exitsData || [],
                        backgroundColor: 'rgba(255, 99, 132, 0.8)',
                        borderColor: 'rgba(255, 99, 132, 1)',
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: 'Cantidad' }
                    },
                    x: {
                        stacked: false
                    }
                }
            }
        });

        console.log('Entries/Exits chart rendered successfully');
    } catch (e) {
        console.error('renderEntriesVsExitsChart error:', e);
    }
};


