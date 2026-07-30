// ==========================================
// PAGE STATE & NAMESPACE
// ==========================================

const TransactionUpdatePage = {
    isSubmitting: false,
    confirmedSubmit: false,
    originalButtonText: null
};

// ==========================================
// PAGE RESTORE (bfcache / validation return)
// ==========================================

window.addEventListener("pageshow", function () {
    TransactionUpdatePage.isSubmitting = false;
    TransactionUpdatePage.confirmedSubmit = false;

    const form = document.getElementById("transactionForm");
    const btn = form?.querySelector("button[type='submit']");

    if (btn) {
        btn.disabled = false;
        btn.innerHTML =
            TransactionUpdatePage.originalButtonText ?? "Dəyişiklikləri saxla";
    }
});

// ==========================================
// INITIALIZER
// ==========================================

document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("transactionForm");
    const btn = form?.querySelector("button[type='submit']");

    if (btn) {
        TransactionUpdatePage.originalButtonText = btn.innerHTML;
    }

    initializeTransactionUpdateForm();
    initializeAmountInput();
    initializeTypeChange();
});

// ==========================================
// SWEETALERT FALLBACK (SAFE GUARD)
// ==========================================

function showAlert(options) {
    if (window.Swal && typeof Swal.fire === "function") {
        return Swal.fire(options);
    }

    if (options.icon === "question" || options.showCancelButton) {
        const confirmed = confirm(
            options.title + (options.text ? "\n" + options.text : "")
        );

        return Promise.resolve({
            isConfirmed: confirmed
        });
    }

    alert(options.title + (options.text ? "\n" + options.text : ""));

    return Promise.resolve({
        isConfirmed: true
    });
}

// ==========================================
// UPDATE FORM VALIDATION & SUBMIT
// ==========================================

function initializeTransactionUpdateForm() {
    const form = document.getElementById("transactionForm");
    if (!form) return;

    form.addEventListener("submit", function (e) {
        // 1. Təsdiqlənmiş submit zamanı bir dəfəlik bypass edir və bayrağı təmizləyir
        if (TransactionUpdatePage.confirmedSubmit) {
            TransactionUpdatePage.confirmedSubmit = false;
            return;
        }

        // 2. Double submit mühafizəsi (sürətli təkrar kliklərin qarşısı alınır)
        if (TransactionUpdatePage.isSubmitting) {
            e.preventDefault();
            return;
        }

        e.preventDefault();

        const amount = document.getElementById("Amount");
        const description = document.getElementById("Description");
        const category = document.getElementById("CategoryId");

        const amountValue = amount ? Number(amount.value) : 0;

        // Strict Amount Validation (Boş input, NaN, Infinity və <=0 hallarına qarşı)
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

        // Description validation
        if (!description || !description.value.trim()) {
            showAlert({
                icon: "warning",
                title: "Təsvir boş ola bilməz",
                text: "Əməliyyat haqqında məlumat daxil edin"
            });
            description?.focus();
            return;
        }

        // Category validation
        if (!category || !category.value) {
            showAlert({
                icon: "warning",
                title: "Kateqoriya seçilməyib",
                text: "Kateqoriya seçin"
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

        // Confirmation Modal
        showAlert({
            title: "Dəyişikliklər yadda saxlanılsın?",
            text: "Əməliyyat məlumatları yenilənəcək",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Bəli, yenilə",
            cancelButtonText: "Ləğv et",
            confirmButtonColor: "#1b5e20",
            cancelButtonColor: "#6c757d"
        }).then((result) => {
            if (!result.isConfirmed) return;

            // State Sıralaması: Əvvəl təsdiq flag-i, sonra isSubmitting aktiv olunur
            TransactionUpdatePage.confirmedSubmit = true;
            TransactionUpdatePage.isSubmitting = true;

            const btn = form.querySelector("button[type='submit']");
            if (btn) {
                TransactionUpdatePage.originalButtonText ??= btn.innerHTML;
                btn.disabled = true;
                btn.innerHTML =
                    '<i class="fa-solid fa-spinner fa-spin me-1"></i> Yenilənir...';
            }

            // Cross-browser safe submit call
            if (typeof form.requestSubmit === "function") {
                form.requestSubmit();
            } else {
                form.submit();
            }
        });
    });
}

// ==========================================
// AMOUNT INPUT FORMAT (AZN Comma & Dot Protection)
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

        // Tək nöqtə limiti (çoxlu nöqtələrin təmizlənməsi)
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
// INCOME / EXPENSE COLOR CHANGE
// ==========================================

function initializeTypeChange() {
    const incomeRadio = document.getElementById("incomeType");
    const expenseRadio = document.getElementById("expenseType");
    const amount = document.getElementById("Amount");

    if (!incomeRadio || !expenseRadio || !amount) return;

    function changeColor() {
        amount.classList.remove("text-success", "text-danger");

        if (incomeRadio.checked) {
            amount.classList.add("text-success");
        } else if (expenseRadio.checked) {
            amount.classList.add("text-danger");
        }
    }

    incomeRadio.addEventListener("change", changeColor);
    expenseRadio.addEventListener("change", changeColor);

    changeColor();
}