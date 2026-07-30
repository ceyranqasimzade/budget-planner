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

    // Düymənin ilkin HTML mətni/ikonu DOM yüklənən anda yadda saxlanılır
    if (btn) {
        TransactionCreatePage.originalButtonText = btn.innerHTML;
    }

    initializeTransactionForm();
    initializeAmountInput();
    initializeTypeChange();
    setTodayDate();
});

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
        // 1. Təsdiqlənmiş submit zamanı bir dəfəlik bypass edir və bayrağı təmizləyir
        if (TransactionCreatePage.confirmedSubmit) {
            TransactionCreatePage.confirmedSubmit = false;
            return;
        }

        // 2. Double submit mühafizəsi (sürətli klik zamanı eventi dərhal dayandırırıq)
        if (TransactionCreatePage.isSubmitting) {
            e.preventDefault();
            return;
        }

        e.preventDefault();

        const amount = document.getElementById("Amount");
        const description = document.getElementById("Description");
        const category = document.getElementById("CategoryId");

        // Məbləğ yoxlaması (isNaN və strict Number parsing)
        const amountValue = Number(amount?.value);

        if (!amount || isNaN(amountValue) || amountValue <= 0) {
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
                text: "Kateqoriya seçin"
            });
            category?.focus();
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

            // Əvvəl icazə verilir, sonra proses başladılır (Düzgün State Sıralaması)
            TransactionCreatePage.confirmedSubmit = true;
            TransactionCreatePage.isSubmitting = true;

            const btn = form.querySelector("button[type='submit']");
            if (btn) {
                TransactionCreatePage.originalButtonText ??= btn.innerHTML;
                btn.disabled = true;
                btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-1"></i> Göndərilir...';
            }

            // requestSubmit() vasitəsilə AntiforgeryToken və Unobtrusive validation
            // pipeline-ı qorunaraq forma MVC Controller-ə göndərilir
            form.requestSubmit();
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

        // 1. Mənfi işarəsini dərhal ləğv edirik (-500 -> 500)
        if (value.includes("-")) {
            value = value.replace(/-/g, "");
        }

        // 2. Azərbaycan klaviaturası üçün vergülü avtomatik nöqtəyə çeviririk (25,50 -> 25.50)
        value = value.replace(/,/g, ".");

        // 3. Yalnız rəqəm və nöqtəyə icazə
        value = value.replace(/[^0-9.]/g, "");

        // 4. İlk nöqtədən sonrakı bütün əlavə nöqtələri silirik (məs: 12.5.7 -> 12.57)
        const firstDot = value.indexOf(".");
        if (firstDot !== -1) {
            value =
                value.substring(0, firstDot + 1) +
                value.substring(firstDot + 1).replace(/\./g, "");
        }

        // 5. Nöqtədən sonra maksimum 2 rəqəm limiti (Qəpik/Cent üçün)
        const parts = value.split(".");
        if (parts.length >= 2) {
            value = parts[0] + "." + parts[1].slice(0, 2);
        }

        // 6. Öndəki lüzumsuz sıfırların təhlükəsiz təmizlənməsi (000.50 -> 0.50 qorunur)
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

// ==========================================
// DATE DEFAULT TODAY (Local Timezone Safe)
// ==========================================

function setTodayDate() {
    const dateInput = document.getElementById("Date");
    if (!dateInput) return;

    if (!dateInput.value) {
        const today = new Date();
        const year = today.getFullYear();
        const month = String(today.getMonth() + 1).padStart(2, "0");
        const day = String(today.getDate()).padStart(2, "0");

        dateInput.value = `${year}-${month}-${day}`;
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
        confirmButtonText: "Bəli",
        cancelButtonText: "Xeyr",
        confirmButtonColor: "#d33",
        cancelButtonColor: "#6c757d"
    }).then(result => {
        if (result.isConfirmed) {
            TransactionCreatePage.isSubmitting = false;
            TransactionCreatePage.confirmedSubmit = false;

            const form = document.getElementById("transactionForm");
            if (form) {
                form.reset();

                // Form reset olunduqda düymənin vəziyyətini də tam bərpa edirik
                const btn = form.querySelector("button[type='submit']");
                if (btn) {
                    btn.disabled = false;
                    btn.innerHTML = TransactionCreatePage.originalButtonText ?? "Yadda saxla";
                }

                const incomeRadio = document.getElementById("incomeType");
                const expenseRadio = document.getElementById("expenseType");
                const amount = document.getElementById("Amount");

                if (amount) amount.classList.remove("text-success", "text-danger");

                if (incomeRadio && incomeRadio.checked && amount) {
                    amount.classList.add("text-success");
                } else if (expenseRadio && expenseRadio.checked && amount) {
                    amount.classList.add("text-danger");
                }

                setTodayDate();
            }
        }
    });
}