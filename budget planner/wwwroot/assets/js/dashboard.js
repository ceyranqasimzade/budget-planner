// ==========================================
// QLOBAL DƏYİŞƏNLƏR VƏ VALYUTA FUNKSİYALARI
// ==========================================
const currencySymbols = {
    "AZN": "₼", "USD": "$", "EUR": "€", "TRY": "₺",
    "RUB": "₽", "GBP": "£", "GEL": "₾", "AED": "د.إ",
    "CHF": "CHF", "CNY": "¥", "CAD": "$"
};

let expenseChart = null; // Əvvəlki qrafik (Doughnut) obyekti
let originalChartValues = []; // Qrafikin orijinal AZN dəyərləri

// Dropdown dəyişəndə işə düşür (HTML-dən çağırılır)
function changeGlobalCurrency() {
    const selectedCurrency = document.getElementById("globalCurrency").value;
    localStorage.setItem("globalCurrency", selectedCurrency);

    updateAmounts(selectedCurrency);
    updateChartCurrency(selectedCurrency);

    // Əgər Gəlir/Xərc qrafiki funksiyası varsa, onu da yeniləyirik
    if (typeof renderPolarBudgetChart === "function" && document.getElementById('myBudgetChart')) {
        renderPolarBudgetChart();
    }
}

// RƏQƏMLƏRİ YENİLƏYƏN FUNKSİYA (Balans, Gəlir, Xərc)
function updateAmounts(targetCurrency) {
    const amountElements = document.querySelectorAll('.dynamic-amount');
    let rate = 1;

    if (targetCurrency !== "AZN" && window.exchangeRates && window.exchangeRates[targetCurrency]) {
        rate = window.exchangeRates[targetCurrency];
    }

    amountElements.forEach(el => {
        const baseAmountAzn = parseFloat(el.getAttribute('data-base-amount')) || 0;
        let convertedAmount = targetCurrency !== "AZN" ? baseAmountAzn / rate : baseAmountAzn;

        const symbol = currencySymbols[targetCurrency] || targetCurrency;
        el.innerText = `${convertedAmount.toFixed(2)} ${symbol}`;
    });
}

// QRAFİKİN RƏQƏMLƏRİNİ YENİLƏYƏN FUNKSİYA (Doughnut chart üçün)
function updateChartCurrency(targetCurrency) {
    if (!expenseChart || originalChartValues.length === 0) return;

    let rate = 1;
    if (targetCurrency !== "AZN" && window.exchangeRates && window.exchangeRates[targetCurrency]) {
        rate = window.exchangeRates[targetCurrency];
    }

    const newChartValues = originalChartValues.map(val => {
        return targetCurrency !== "AZN" ? val / rate : val;
    });

    expenseChart.data.datasets[0].data = newChartValues;
    expenseChart.update();
}

// ==========================================
// MODAL, AUTH VƏ DİGƏR FUNKSİYALAR
// ==========================================

// KART XANASINI GÖSTƏR / GİZLƏ FUNKSİYASI
function toggleCardField(selectElement) {
    if (!selectElement) return;

    const modalBody = selectElement.closest('.modal-body') || selectElement.closest('.modal');
    if (!modalBody) return;

    const cardGroup = modalBody.querySelector('.card-select-group');
    if (!cardGroup) return;

    const cardSelect = cardGroup.querySelector('select[name="CardId"]');

    if (selectElement.value === 'card') {
        cardGroup.style.display = 'block';
        if (cardSelect) {
            cardSelect.removeAttribute('disabled');
            cardSelect.setAttribute('required', 'required');
        }
    } else {
        cardGroup.style.display = 'none';
        if (cardSelect) {
            cardSelect.setAttribute('disabled', 'disabled');
            cardSelect.removeAttribute('required');
            cardSelect.value = '';
        }
    }
}

// AUTH YOXLANIŞI VƏ MODAL AÇILIŞI
function openModalIfAuth(modalId) {
    if (!window.isUserAuthenticated) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'warning',
                title: 'Diqqət!',
                text: 'Əməliyyat etmək üçün zəhmət olmasa sistemə daxil olun və ya qeydiyyatdan keçin.',
                showCancelButton: true,
                confirmButtonText: 'Daxil Ol',
                cancelButtonText: 'Bağla',
                confirmButtonColor: '#198754'
            }).then((result) => {
                if (result.isConfirmed) {
                    window.location.href = '/Account/Login';
                }
            });
        } else {
            if (confirm('Əməliyyat etmək üçün zəhmət olmasa sistemə daxil olun.')) {
                window.location.href = '/Account/Login';
            }
        }
    } else {
        var targetElement = document.querySelector(modalId);
        if (targetElement && typeof bootstrap !== 'undefined') {
            var myModal = new bootstrap.Modal(targetElement);
            myModal.show();
        }
    }
}

// KART SİLMƏ MODALINI DİNAMİK AÇAN FUNKSİYA
function openDeleteCardModal(cardId, cardName) {
    document.getElementById('deleteCardId').value = cardId;
    document.getElementById('deleteCardName').textContent = cardName;

    var modalElement = document.getElementById('deleteCardModal');
    if (modalElement && typeof bootstrap !== 'undefined') {
        var modal = new bootstrap.Modal(modalElement);
        modal.show();
    }
}

// ==========================================
// SƏHİFƏ YÜKLƏNDİKDƏ İŞƏ DÜŞƏN PROSESLƏR
// ==========================================
document.addEventListener("DOMContentLoaded", function () {
    // VALYUTA İNİSİALİZASİYASI
    const savedCurrency = localStorage.getItem("globalCurrency") || "AZN";
    const currencySelect = document.getElementById("globalCurrency");
    if (currencySelect) {
        currencySelect.value = savedCurrency;
    }
    updateAmounts(savedCurrency);

    // MODALLARIN SIFIRLANMASI
    const modals = document.querySelectorAll('.modal');
    modals.forEach(modal => {
        modal.addEventListener('hidden.bs.modal', function () {
            const form = modal.querySelector('form');
            if (form) form.reset();

            const cardGroup = modal.querySelector('.card-select-group');
            if (cardGroup) {
                cardGroup.style.display = 'none';
                const cardSelect = cardGroup.querySelector('select[name="CardId"]');
                if (cardSelect) {
                    cardSelect.setAttribute('disabled', 'disabled');
                    cardSelect.removeAttribute('required');
                    cardSelect.value = '';
                }
            }
        });
    });

    // DARK MODE & İKON
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

    // QRAFİK DATA (CHART.JS - DOUGHNUT)
    const canvas = document.getElementById('expenseChart');
    if (canvas && typeof Chart !== 'undefined') {

        function renderChart(labels, values) {
            if (!labels || labels.length === 0 || !values || values.length === 0) return;

            originalChartValues = values;

            const ctx = canvas.getContext('2d');
            expenseChart = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: labels,
                    datasets: [{
                        data: values,
                        backgroundColor: ['#ff6b6b', '#4dabf7', '#fcc419', '#20c997', '#ff8787', '#845ef7'],
                        borderWidth: 0
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '75%',
                    plugins: {
                        legend: { position: 'bottom' },
                        tooltip: {
                            callbacks: {
                                label: function (context) {
                                    const curr = document.getElementById("globalCurrency") ? document.getElementById("globalCurrency").value : "AZN";
                                    const sym = currencySymbols[curr] || curr;
                                    return `${context.label}: ${context.raw.toFixed(2)} ${sym}`;
                                }
                            }
                        }
                    }
                }
            });

            updateChartCurrency(savedCurrency);
        }

        if (window.dashboardChartData && window.dashboardChartData.labels && window.dashboardChartData.labels.length > 0) {
            renderChart(window.dashboardChartData.labels, window.dashboardChartData.data);
        } else {
            fetch('/Home/GetExpenseChartData')
                .then(response => response.ok ? response.json() : null)
                .then(data => {
                    if (data && data.labels) {
                        renderChart(data.labels, data.values);
                    }
                })
                .catch(error => console.error("Qrafik məlumatları yüklənərkən xəta:", error));
        }
    }

    // ==========================================
    // ÖDƏNİŞ ÜSULU (KART) DƏYİŞDİKDƏ İŞƏ DÜŞƏN FUNKSİYA
    // ==========================================
    const paymentSelects = document.querySelectorAll('select[name="PaymentMethod"], select[name="CardId"]');

    paymentSelects.forEach(select => {
        select.addEventListener('change', function () {
            // Seçilmiş option-u tapırıq
            const selectedOption = this.options[this.selectedIndex];
            const cardCurrency = selectedOption.getAttribute('data-currency');

            // Eyni modalın içindəki valyuta xanasını tapırıq
            const modalBody = this.closest('.modal-body');

            // Əgər modal tapılarsa, içindəki .transaction-currency axtarırıq
            if (modalBody) {
                const currencyDropdown = modalBody.querySelector('.transaction-currency');

                // Əgər kartın valyutası varsa, dropdown-u o valyutaya dəyişirik
                if (cardCurrency && currencyDropdown) {
                    currencyDropdown.value = cardCurrency;
                }
            }
        });
    });
});

// ==========================================
// DATEPICKER (FLATPICKR) İNİSİALİZASİYASI
// ==========================================
    document.addEventListener("DOMContentLoaded", function () {
        // Təqvimin işə salınması
        flatpickr(".datepicker", {
            locale: "az",               // Azərbaycan dili
            dateFormat: "Y-m-d",        // Arxa planda serverə gedən gizli format (C# üçün ideal)
            altInput: true,             // İstifadəçiyə fərqli format göstərməyə icazə ver
            altFormat: "d.m.Y",         // Ekranda görünən format (26.07.2026)
            allowInput: true            // Əllə yazmağa icazə ver
        });
    });