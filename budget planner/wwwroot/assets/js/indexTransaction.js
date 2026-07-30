// ==========================================
// HELPER FUNCTIONS (DRY & Visible Rows Only)
// ==========================================

/**
 * Ekranda yalnız görünən (gizlədilməmiş) cədvəl sətirlərindəki checkbox-ları qaytarır
 */
function getVisibleCheckboxes() {
    return [...document.querySelectorAll("#tableBody tr")]
        .filter(row => !row.hidden && row.querySelector(".row-checkbox"))
        .map(row => row.querySelector(".row-checkbox"));
}

// ==========================================
// TABLE FILTER (Pure HTML5 'hidden' attribute)
// ==========================================

function filterTable() {
    const searchVal = (document.getElementById("searchInp")?.value || "").trim().toLocaleLowerCase('az-AZ');
    const typeVal = document.getElementById("typeFilter")?.value || "all";
    const startVal = document.getElementById("startDate")?.value || "";
    const endVal = document.getElementById("endDate")?.value || "";

    const rows = document.querySelectorAll("#tableBody tr");
    let visibleCount = 0;

    rows.forEach(row => {
        const dataType = row.dataset.type;
        const rowDate = row.dataset.date;

        // "Nəticə tapılmadı" sətridirsə, kənara qoyuruq
        if (row.id === "noDataRow") return;

        // Məlumatı və ya tarixi olmayan adi sətirlər filtrlənmir
        if (!dataType || !rowDate) return;

        const text = row.innerText.toLocaleLowerCase('az-AZ');

        const matchesSearch = !searchVal || text.includes(searchVal);
        const matchesType = (typeVal === "all" || dataType === typeVal);
        const matchesStart = (!startVal || rowDate >= startVal);
        const matchesEnd = (!endVal || rowDate <= endVal);

        const show = matchesSearch && matchesType && matchesStart && matchesEnd;

        // Vahid `hidden` istifadəsi
        row.hidden = !show;

        if (show) visibleCount++;

        // Ekranda görünməyən sətrin checkbox-ı uncheck edilir
        if (!show) {
            const checkbox = row.querySelector(".row-checkbox");
            if (checkbox) checkbox.checked = false;
        }
    });

    // Əgər heç bir sətir tapılmadısa "Nəticə tapılmadı" mesajını göstər
    toggleNoDataMessage(visibleCount === 0);

    updateSelectAllStatus();
    updateBulkDeleteButton();
}

/**
 * Filter nəticəsində heç bir məlumat tapılmadıqda mesaj göstərmək üçün
 */
function toggleNoDataMessage(show) {
    let noDataRow = document.getElementById("noDataRow");

    if (show) {
        if (!noDataRow) {
            const tbody = document.getElementById("tableBody");
            if (!tbody) return;

            noDataRow = document.createElement("tr");
            noDataRow.id = "noDataRow";
            noDataRow.innerHTML = `
                <td colspan="10" class="text-center py-4 text-muted">
                    <i class="bi bi-search display-6 d-block mb-2">
                    <p class="mb-0">Axtarışınıza uyğun heç bir əməliyyat tapılmadı.</p>
                </td>
            `;
            tbody.appendChild(noDataRow);
        }
        noDataRow.hidden = false;
    } else if (noDataRow) {
        noDataRow.hidden = true;
    }
}

// ==========================================
// RESET FILTER
// ==========================================

function resetFilters() {
    document.querySelectorAll("#searchInp, #startDate, #endDate")
        .forEach(x => x.value = "");

    const type = document.getElementById("typeFilter");
    if (type) type.value = "all";

    filterTable();
}

// ==========================================
// CHECKBOX MANAGEMENT
// ==========================================

function toggleSelectAll(source) {
    getVisibleCheckboxes().forEach(cb => {
        cb.checked = source.checked;
    });

    updateSelectAllStatus();
    updateBulkDeleteButton();
}

function updateSelectAllStatus() {
    const checkboxes = getVisibleCheckboxes();
    const selectAll = document.getElementById("selectAll");

    if (!selectAll) return;

    if (checkboxes.length === 0) {
        selectAll.checked = false;
        selectAll.indeterminate = false;
        return;
    }

    const checkedCount = checkboxes.filter(x => x.checked).length;

    // ☑️ Hamısı seçilibsə
    selectAll.checked = checkedCount === checkboxes.length;

    // ➖ Bir neçəsi seçilibsə (Indeterminate state)
    selectAll.indeterminate = checkedCount > 0 && checkedCount < checkboxes.length;
}

function updateBulkDeleteButton() {
    // Yalnız görünən və seçilən checkbox-lar hesaba alınır
    const selected = getVisibleCheckboxes().filter(cb => cb.checked);
    const button = document.getElementById("btnBulkDelete");
    const count = document.getElementById("selectedCount");

    if (!button || !count) return;

    count.innerText = selected.length;
    button.classList.toggle("d-none", selected.length === 0);
}

// Checkbox event listener (Event Delegation)
document.addEventListener("change", function (e) {
    if (e.target && e.target.classList.contains("row-checkbox")) {
        updateSelectAllStatus();
        updateBulkDeleteButton();
    }
});

// ==========================================
// BULK DELETE (FETCH POST + CSRF)
// ==========================================

function bulkDeleteTransactions() {
    const selectedCheckboxes = getVisibleCheckboxes().filter(x => x.checked);
    const ids = selectedCheckboxes.map(x => Number(x.value));

    if (ids.length === 0) return;

    const token = document.querySelector('#csrfForm input[name="__RequestVerificationToken"]')?.value;

    Swal.fire({
        title: "Əminsiniz?",
        text: `${ids.length} ədəd əməliyyat silinəcək!`,
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Bəli, sil",
        cancelButtonText: "Ləğv et",
        confirmButtonColor: "#d33",
        cancelButtonColor: "#6c757d",
        reverseButtons: true
    }).then(result => {
        if (!result.isConfirmed) return;

        fetch("/Transaction/BulkDelete", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "RequestVerificationToken": token || ""
            },
            body: JSON.stringify(ids)
        })
            .then(async res => {
                if (res.ok) {
                    Swal.fire({
                        icon: "success",
                        title: "Silindi!",
                        text: "Seçilmiş əməliyyatlar uğurla silindi.",
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    const errData = await res.json().catch(() => null);
                    Swal.fire("Xəta", errData?.message || "Silinmə zamanı xəta baş verdi.", "error");
                }
            })
            .catch(() => Swal.fire("Xəta", "Serverlə əlaqə kəsildi", "error"));
    });
}

// ==========================================
// SINGLE DELETE (Event Delegation + Dynamic POST Form)
// ==========================================

document.addEventListener("click", function (e) {
    const btn = e.target.closest(".delete-btn");
    if (!btn) return;

    const deleteUrl = btn.dataset.url;
    let name = "Bu əməliyyat";

    if (btn.dataset.description) {
        try {
            name = JSON.parse(btn.dataset.description);
        } catch {
            name = btn.dataset.description;
        }
    }

    Swal.fire({
        title: "Silmək istəyirsiniz?",
        text: `"${name}" silinəcək`,
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Bəli, sil",
        cancelButtonText: "Ləğv et",
        confirmButtonColor: "#d33",
        cancelButtonColor: "#6c757d",
        reverseButtons: true
    }).then(result => {
        if (!result.isConfirmed) return;

        const form = document.createElement("form");
        form.method = "POST";
        form.action = deleteUrl;

        const token = document.querySelector('#csrfForm input[name="__RequestVerificationToken"]')?.value;

        if (token) {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "__RequestVerificationToken";
            input.value = token;
            form.appendChild(input);
        }

        document.body.appendChild(form);
        form.submit();
    });
});

// ==========================================
// FISKAL ID / RECEIPT DEMO
// ==========================================

function processFiskalId() {
    const input = document.getElementById("fiskalIdInp");
    if (!input) return;

    const value = input.value.trim();
    if (value.length !== 12) {
        Swal.fire("Diqqət", "Xahiş olunur 12 simvollu Fiskal ID daxil edin", "warning");
        return;
    }

    Swal.fire("Uğurlu", "Fiskal ID qəbul edildi: " + value.toUpperCase(), "success");
    input.value = "";
}

function startQRScanner() {
    Swal.fire("Məlumat", "QR skaner tezliklə istifadəyə veriləcək!", "info");
}

// ==========================================
// LIVE EVENT LISTENERS (INPUT & DATE CHANGE)
// ==========================================

document.addEventListener("DOMContentLoaded", function () {
    // Axtarış sahəsində hər simvol yazıldıqda dərhal filtrlə
    document.getElementById("searchInp")?.addEventListener("input", filterTable);

    // Filter dəyişikliklərini dinlə
    document.getElementById("typeFilter")?.addEventListener("change", filterTable);
    document.getElementById("startDate")?.addEventListener("change", filterTable);
    document.getElementById("endDate")?.addEventListener("change", filterTable);
});
document.addEventListener("DOMContentLoaded", function () {

    // ==========================================
    // AZƏRBAYCAN DİLİNDƏ TƏQVİM (FLATPICKR)
    // ==========================================
    const commonDateConfig = {
        locale: "az",               // Azərbaycan dili
        dateFormat: "Y-m-d",        // Bazaya və filtrləməyə uyğun format (2026-07-30)
        altInput: true,             // İstifadəçiyə daha oxunaqlı göstər
        altFormat: "j F Y",         // İstifadəçi üçün görünüş (məs: 30 İyul 2026)
        allowInput: true,           // Əllə də yazmağa icazə ver
        disableMobile: "true",      // Mobil cihazlarda da eyni interfeysi göstər
    };

    // Başlama tarixi təqvimi
    const startPicker = flatpickr("#startDate", {
        ...commonDateConfig,
        placeholder: "Başlama tarixi seçin",
        onChange: function (selectedDates, dateStr) {
            // Başlama tarixi seçildikdə bitmə tarixinin minimumunu yeniləyirik
            endPicker.set("minDate", dateStr);
            filterTable(); // Dərhal filtrlə
        }
    });

    // Bitmə tarixi təqvimi
    const endPicker = flatpickr("#endDate", {
        ...commonDateConfig,
        placeholder: "Bitmə tarixi seçin",
        onChange: function (selectedDates, dateStr) {
            // Bitmə tarixi seçildikdə başlama tarixinin maksimumunu yeniləyirik
            startPicker.set("maxDate", dateStr);
            filterTable(); // Dərhal filtrlə
        }
    });

    // Reset düyməsi sıfırlandıqda təqvimləri də təmizləmək üçün
    const originalResetFilters = window.resetFilters;
    window.resetFilters = function () {
        startPicker.clear();
        endPicker.clear();
        if (typeof originalResetFilters === "function") {
            originalResetFilters();
        }
    };

    // Axtarış sahəsində hər simvol yazıldıqda dərhal filtrlə
    document.getElementById("searchInp")?.addEventListener("input", filterTable);
    document.getElementById("typeFilter")?.addEventListener("change", filterTable);
});