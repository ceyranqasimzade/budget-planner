// ==========================================
// PAGE STATE & NAMESPACE
// ==========================================

const TransactionCreatePage = {
    isSubmitting: false,
    confirmedSubmit: false,
    originalButtonText: null
};

// Brauzerin 'Back/Forward Cache' (bfcache) və ya server validation 
// xətalarından dönüşündə submit statusunu və düymənin ilkin vəziyyətini bərpa edir
window.addEventListener("pageshow", function () {
    TransactionCreatePage.isSubmitting = false;
    TransactionCreatePage.confirmedSubmit = false;

    const form = document.getElementById("transactionForm");
    const btn = form?.querySelector("button[type='submit']");

    if (btn) {
        btn.disabled = false;
        btn.innerHTML = TransactionCreatePage.originalButtonText ?? "Yadda saxla";
    }
});

// ==========================================
// CREATE TRANSACTION PAGE INITIALIZER
// ==========================================

document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("transactionForm");
    const btn = form?.querySelector("button[type='submit']");

    if (btn) {
        TransactionCreatePage.originalButtonText = btn.innerHTML;
    }

    initializeDatePicker();
    initializeTransactionForm();
    initializeAmountInput();
    initializeTypeChange();
});

// ==========================================
// FLATPICKR / DATE INITIALIZATION
// ==========================================

function initializeDatePicker() {
    const dateInput = document.getElementById("transactionDate") || document.getElementById("Date");
    if (!dateInput) return;

    if (typeof flatpickr === "function") {
        flatpickr(dateInput, {
            locale: "az",
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d.m.Y",
            defaultDate: dateInput.value || "today",
            disableMobile: "true"
        });
    } else {
        setTodayDate(dateInput);
    }
}

function setTodayDate(targetInput) {
    const dateInput = targetInput || document.getElementById("transactionDate") || document.getElementById("Date");
    if (!dateInput || dateInput.value) return;

    const today = new Date();
    const year = today.getFullYear();
    const month = String(today.getMonth() + 1).padStart(2, "0");
    const day = String(today.getDate()).padStart(2, "0");

    dateInput.value = `${year}-${month}-${day}`;
}

// ==========================================
// SWEETALERT FALLBACK HELPER (SAFE GUARD)
// ==========================================

function showAlert(options) {
    if (window.Swal && typeof Swal.fire === "function") {
        return Swal.fire(options);
    }

    if (options.icon === "question" || options.showCancelButton) {
        const confirmed = confirm(options.title + (options.text ? "\n" + options.text : ""));
        return Promise.resolve({ isConfirmed: confirmed });
    }

    alert(options.title + (options.text ? "\n" + options.text : ""));
    return Promise.resolve({ isConfirmed: true });
}

// ==========================================
// FORM INITIALIZATION & VALIDATION
// ==========================================

function initializeTransactionForm() {
    const form = document.getElementById("transactionForm");
    if (!form) return;

    form.addEventListener("submit", function (e) {
        // 1. Təsdiqlənmiş submit zamanı bir dəfəlik bypass edir
        if (TransactionCreatePage.confirmedSubmit) {
            TransactionCreatePage.confirmedSubmit = false;
            return;
        }

        // 2. Double submit mühafizəsi
        if (TransactionCreatePage.isSubmitting) {
            e.preventDefault();
            return;
        }

        e.preventDefault();

        const amount = document.getElementById("Amount");
        const description = document.getElementById("Description");
        const category = document.getElementById("CategoryId");

        const amountValue = amount ? Number(amount.value) : 0;

        // Məbləğ yoxlaması
        if (
            !amount ||
            !amount.value.trim() ||
            !Number.isFinite(amountValue) ||
            amountValue <= 0
        ) {
            showAlert({
                icon: "warning",
                title: "Məbləğ düzgün deyil",
                text: "0-dan böyük təhlükəsiz məbləğ daxil edin"
            });
            amount?.focus();
            return;
        }

        // Description yoxlaması
        if (!description || !description.value.trim()) {
            showAlert({
                icon: "warning",
                title: "Təsvir boş ola bilməz",
                text: "Əməliyyat haqqında məlumat daxil edin"
            });
            description?.focus();
            return;
        }

        // Kateqoriya yoxlaması
        if (!category || !category.value) {
            showAlert({
                icon: "warning",
                title: "Kateqoriya seçilməyib",
                text: "Lütfən kateqoriya seçin"
            });
            category?.focus();
            return;
        }

        // Native Browser HTML5 Validation Check
        if (typeof form.checkValidity === "function" && !form.checkValidity()) {
            if (typeof form.reportValidity === "function") {
                form.reportValidity();
            }
            return;
        }

        // Təsdiqləmə Modal-ı
        showAlert({
            title: "Əməliyyat əlavə edilsin?",
            text: "Məlumatlar yadda saxlanılacaq",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Bəli, əlavə et",
            cancelButtonText: "Ləğv et",
            confirmButtonColor: "#1b5e20",
            cancelButtonColor: "#6c757d"
        }).then(result => {
            if (!result.isConfirmed) return;

            TransactionCreatePage.confirmedSubmit = true;
            TransactionCreatePage.isSubmitting = true;

            const btn = form.querySelector("button[type='submit']");
            if (btn) {
                TransactionCreatePage.originalButtonText ??= btn.innerHTML;
                btn.disabled = true;
                btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-1"></i> Göndərilir...';
            }

            if (typeof form.requestSubmit === "function") {
                form.requestSubmit();
            } else {
                form.submit();
            }
        });
    });
}

// ==========================================
// AMOUNT INPUT FORMAT (AZN Comma Support & Dot Protection)
// ==========================================

function initializeAmountInput() {
    const amountInput = document.getElementById("Amount");
    if (!amountInput) return;

    amountInput.addEventListener("input", function () {
        let value = this.value;

        // Mənfi işarələrin təmizlənməsi
        value = value.replace(/-/g, "");

        // AZN klaviaturası üçün vergülü nöqtəyə çevirmə
        value = value.replace(/,/g, ".");

        // Yalnız rəqəm və nöqtə saxlanılır
        value = value.replace(/[^0-9.]/g, "");

        // Tək nöqtə limiti
        const firstDot = value.indexOf(".");
        if (firstDot !== -1) {
            value =
                value.substring(0, firstDot + 1) +
                value.substring(firstDot + 1).replace(/\./g, "");
        }

        // Qəpik üçün 2 rəqəm limiti
        const parts = value.split(".");
        if (parts.length >= 2) {
            value = parts[0] + "." + parts[1].slice(0, 2);
        }

        // Öndəki artıq sıfırların təmizlənməsi (000.50 -> 0.50)
        let cleanParts = value.split(".");
        cleanParts[0] = cleanParts[0].replace(/^0+(?=\d)/, "");

        if (cleanParts[0] === "") {
            cleanParts[0] = "0";
        }

        this.value = cleanParts.join(".");
    });
}

// ==========================================
// INCOME / EXPENSE COLOR CHANGE (HYBRID SUPPORT)
// ==========================================

function initializeTypeChange() {
    const isIncomeSelect = document.getElementById("IsIncome");
    const incomeRadio = document.getElementById("incomeType");
    const expenseRadio = document.getElementById("expenseType");
    const amount = document.getElementById("Amount");

    if (!amount) return;

    function applyColor(isIncome) {
        amount.classList.remove("text-success", "text-danger");
        if (isIncome === true) {
            amount.classList.add("text-success");
        } else if (isIncome === false) {
            amount.classList.add("text-danger");
        }
    }

    // 1. Dropdown (<select id="IsIncome">) dəstəyi
    if (isIncomeSelect) {
        const updateFromSelect = () => {
            const val = isIncomeSelect.value;
            applyColor(val === "true" ? true : val === "false" ? false : null);
        };
        isIncomeSelect.addEventListener("change", updateFromSelect);
        updateFromSelect();
    }
    // 2. Radio düymə dəstəyi (#incomeType / #expenseType)
    else if (incomeRadio && expenseRadio) {
        const updateFromRadio = () => {
            if (incomeRadio.checked) applyColor(true);
            else if (expenseRadio.checked) applyColor(false);
        };
        incomeRadio.addEventListener("change", updateFromRadio);
        expenseRadio.addEventListener("change", updateFromRadio);
        updateFromRadio();
    }
}

// ==========================================
// RESET FORM CONFIRM & RE-SYNC UI
// ==========================================

function resetTransactionForm() {
    showAlert({
        title: "Məlumatlar silinsin?",
        text: "Daxil etdiyiniz bütün məlumatlar sıfırlanacaq",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Bəli, sıfırla",
        cancelButtonText: "Xeyr",
        confirmButtonColor: "#d33",
        cancelButtonColor: "#6c757d"
    }).then(result => {
        if (!result.isConfirmed) return;

        TransactionCreatePage.isSubmitting = false;
        TransactionCreatePage.confirmedSubmit = false;

        const form = document.getElementById("transactionForm");
        if (form) {
            form.reset();

            // Düymənin vəziyyətini bərpa et
            const btn = form.querySelector("button[type='submit']");
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = TransactionCreatePage.originalButtonText ?? "Yadda saxla";
            }

            // Rəngləri və Tarixi yenidən sinxronlaşdır
            initializeTypeChange();
            initializeDatePicker();
        }
    });
}
function saveNewCategory() {
    var nameInput = document.getElementById('quickCategoryName');
    var catName = nameInput.value.trim();

    if (!catName) {
        alert('Zəhmət olmasa kateqoriya adını yazın.');
        return;
    }

    // CSRF Token götürülür
    var csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Transaction/CreateCategoryAjax', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': csrfToken
        },
        body: JSON.stringify({ name: catName })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // 1. Yeni kateqoriyanı select-ə əlavə edirik və avtomatik seçirik
                var selectBox = document.getElementById('CategoryId');
                var newOption = new Option(data.name, data.id, true, true);
                selectBox.add(newOption);

                // 2. Input-u təmizləyirik və Modalı bağlayırıq
                nameInput.value = '';
                var modalEl = document.getElementById('quickCategoryModal');
                var modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) {
                    modal.hide();
                }
            } else {
                alert(data.message || 'Xəta baş verdi.');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Sistemdə texniki xəta baş verdi.');
        });
}
function saveNewCategory() {
    var nameInput = document.getElementById('quickCategoryName');
    var catName = nameInput.value.trim();

    if (!catName) {
        alert('Zəhmət olmasa kateqoriya adını yazın.');
        return;
    }

    // CSRF Token götürülür
    var csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    fetch('/Transaction/CreateCategoryAjax', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': csrfToken
        },
        body: JSON.stringify({ name: catName })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                // 1. Yeni kateqoriyanı select-ə əlavə edirik və avtomatik seçirik
                var selectBox = document.getElementById('CategoryId');
                var newOption = new Option(data.name, data.id, true, true);
                selectBox.add(newOption);

                // 2. Input-u təmizləyirik və Modalı bağlayırıq
                nameInput.value = '';
                var modalEl = document.getElementById('quickCategoryModal');
                var modal = bootstrap.Modal.getInstance(modalEl);
                if (modal) {
                    modal.hide();
                }
            } else {
                alert(data.message || 'Xəta baş verdi.');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Sistemdə texniki xəta baş verdi.');
        });
}
document.addEventListener("DOMContentLoaded", function () {
    var datePickerElement = document.querySelector("#transactionDate");

    if (datePickerElement) {
        flatpickr(datePickerElement, {
            locale: "az",
            enableTime: true,
            time_24hr: true,
            dateFormat: "Y-m-d H:i", // C#-ın asanlıqla oxuduğu ISO formatı (Məs: 2026-08-04 14:30)
            altInput: true,          // Ekranda istifadəçi üçün gözəl format göstərir
            altFormat: "d.m.Y H:i",   // Ekran görünüşü (Məs: 04.08.2026 14:30)
            defaultDate: new Date()
        });
    }
});