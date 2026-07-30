// ==========================================
// PAGE STATE & NAMESPACE
// ==========================================

const TransactionDeletePage = {
    confirmTitle: "Əməliyyat silinsin?",
    confirmText: "Bu əməliyyat geri qaytarıla bilməz!",
    deletingText: '<i class="fa-solid fa-spinner fa-spin me-1"></i> Silinir...'
};

// ==========================================
// PAGE RESTORE (bfcache / page restore)
// ==========================================

window.addEventListener("pageshow", function () {
    const deleteForms = document.querySelectorAll(".delete-transaction-form");

    deleteForms.forEach(form => {
        form.dataset.deleting = "false";

        const btn = form.querySelector("button[type='submit']");
        if (btn) {
            btn.disabled = false;

            // Strict undefined check: Boş HTML/string hallarını da tam bərpa edir
            if (btn.dataset.originalText !== undefined) {
                btn.innerHTML = btn.dataset.originalText;
            }
        }
    });
});

// ==========================================
// INITIALIZER
// ==========================================

document.addEventListener("DOMContentLoaded", function () {
    initializeTransactionDelete();
});

// ==========================================
// SWEETALERT FALLBACK (SAFE GUARD)
// ==========================================

function showDeleteAlert(options) {
    if (window.Swal && typeof Swal.fire === "function") {
        return Swal.fire(options);
    }

    return Promise.resolve({
        isConfirmed: confirm(
            options.title + (options.text ? "\n" + options.text : "")
        )
    });
}

// ==========================================
// DELETE TRANSACTION
// ==========================================

function initializeTransactionDelete() {
    const forms = document.querySelectorAll(".delete-transaction-form");
    if (!forms.length) return;

    forms.forEach(form => {
        form.addEventListener("submit", function (e) {
            // Təkrar klik (Double Submit) mühafizəsi
            if (form.dataset.deleting === "true") {
                e.preventDefault();
                return;
            }

            e.preventDefault();

            showDeleteAlert({
                title: TransactionDeletePage.confirmTitle,
                text: TransactionDeletePage.confirmText,
                icon: "warning",
                showCancelButton: true,
                confirmButtonText: "Bəli, sil",
                cancelButtonText: "Ləğv et",
                confirmButtonColor: "#dc3545",
                cancelButtonColor: "#6c757d"
            }).then(result => {
                if (!result.isConfirmed) {
                    return;
                }

                // Silmə əməliyyatı başladı
                form.dataset.deleting = "true";

                const btn = form.querySelector("button[type='submit']");
                if (btn) {
                    if (btn.dataset.originalText === undefined) {
                        btn.dataset.originalText = btn.innerHTML;
                    }
                    btn.disabled = true;
                    btn.innerHTML = TransactionDeletePage.deletingText;
                }

                // Direct native submit (ASP.NET AntiForgery, Action execution safe)
                form.submit();
            });
        });
    });
}