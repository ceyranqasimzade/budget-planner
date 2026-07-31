// ==========================================
// 0. GLOBAL DEYİŞƏNLƏR (Flatpickr üçün)
// ==========================================
let startPicker = null;
let endPicker = null;

// ==========================================
// 1. HELPER FUNCTIONS (DRY & Performans)
// ==========================================

/**
 * Ekranda yalnız görünən cədvəl sətirlərindəki checkbox-ları qaytarır.
 * (hidden və Bootstrap d-none nəzərə alınır: offsetParent !== null)
 */
function getVisibleCheckboxes() {
    return [...document.querySelectorAll("#tableBody tr")]
        .filter(row => row.offsetParent !== null && row.querySelector(".row-checkbox"))
        .map(row => row.querySelector(".row-checkbox"));
}

function getCsrfToken() {
    return document.querySelector('#csrfForm input[name="__RequestVerificationToken"]')?.value || "";
}

// ==========================================
// 2. TABLE FILTER & NO-DATA MESSAGING
// ==========================================

function filterTable() {
    const searchVal = (document.getElementById("searchInp")?.value || "").trim().toLocaleLowerCase('az-AZ');
    const typeVal = document.getElementById("typeFilter")?.value || "all";
    const startVal = document.getElementById("startDate")?.value || "";
    const endVal = document.getElementById("endDate")?.value || "";

    // Performans: Sətirləri birbaşa tableBody üzərindən əldə edirik
    const tableBody = document.getElementById("tableBody");
    if (!tableBody) return;

    const rows = tableBody.getElementsByTagName("tr");
    let visibleCount = 0;

    Array.from(rows).forEach(row => {
        const dataType = row.dataset.type;
        const rowDate = row.dataset.date; // YYYY-MM-DD formatında olmalıdır

        if (row.id === "noDataRow") return;
        if (!dataType || !rowDate) return;

        // Performans üçün innerText əvəzinə textContent
        const text = (row.textContent || "").toLocaleLowerCase('az-AZ');

        const matchesSearch = !searchVal || text.includes(searchVal);
        const matchesType = (typeVal === "all" || dataType === typeVal);
        const matchesStart = (!startVal || rowDate >= startVal);
        const matchesEnd = (!endVal || rowDate <= endVal);

        const show = matchesSearch && matchesType && matchesStart && matchesEnd;
        row.hidden = !show;

        if (show) {
            visibleCount++;
        } else {
            const checkbox = row.querySelector(".row-checkbox");
            if (checkbox) checkbox.checked = false;
        }
    });

    toggleNoDataMessage(visibleCount === 0);
    updateSelectAllStatus();
    updateBulkDeleteButton();
}

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
                    <i class="bi bi-search display-6 d-block mb-2"></i>
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

/**
 * Universal və TƏK Reset funksiyası
 */
function resetFilters() {
    if (startPicker) startPicker.clear();
    if (endPicker) endPicker.clear();

    document.querySelectorAll("#searchInp, #startDate, #endDate")
        .forEach(x => x.value = "");

    const type = document.getElementById("typeFilter");
    if (type) type.value = "all";

    filterTable();
}

// ==========================================
// 3. CHECKBOX & SELECTION MANAGEMENT
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
    selectAll.checked = checkedCount === checkboxes.length;
    selectAll.indeterminate = checkedCount > 0 && checkedCount < checkboxes.length;
}

function updateBulkDeleteButton() {
    const selected = getVisibleCheckboxes().filter(cb => cb.checked);
    const button = document.getElementById("btnBulkDelete");
    const count = document.getElementById("selectedCount");

    if (!button || !count) return;

    count.innerText = selected.length;
    button.classList.toggle("d-none", selected.length === 0);
}

document.addEventListener("change", function (e) {
    if (e.target && e.target.classList.contains("row-checkbox")) {
        updateSelectAllStatus();
        updateBulkDeleteButton();
    }
});

// ==========================================
// 4. BULK & SINGLE DELETE
// ==========================================

function bulkDeleteTransactions() {
    const selectedCheckboxes = getVisibleCheckboxes().filter(x => x.checked);
    const ids = selectedCheckboxes.map(x => Number(x.value));

    if (ids.length === 0) return;

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
                "RequestVerificationToken": getCsrfToken()
            },
            body: JSON.stringify(ids)
        })
            .then(async res => {
                if (res.ok) {
                    // DOM-dan birbaşa silmək
                    selectedCheckboxes.forEach(cb => cb.closest("tr")?.remove());

                    // filterTable onsuz da statusları və düyməni yeniləyir
                    filterTable();

                    Swal.fire({
                        icon: "success",
                        title: "Silindi!",
                        text: "Seçilmiş əməliyyatlar uğurla silindi.",
                        timer: 1500,
                        showConfirmButton: false
                    });
                } else {
                    const errData = await res.json().catch(() => null);
                    Swal.fire("Xəta", errData?.message || "Silinmə zamanı xəta baş verdi.", "error");
                }
            })
            .catch(() => Swal.fire("Xəta", "Serverlə əlaqə kəsildi", "error"));
    });
}

document.addEventListener("click", function (e) {
    const btn = e.target.closest(".delete-btn");
    if (!btn) return;

    const deleteUrl = btn.dataset.url;
    if (!deleteUrl) {
        console.error("Silinmə URL-i (data-url) tapılmadı.");
        return;
    }

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

        const token = getCsrfToken();
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
// 5. FISKAL ID & DRAG-AND-DROP (KLİKLƏ SEÇİM DAXİL)
// ==========================================

/**
 * 12 simvollu Fiskal ID-ni RegExp ilə yoxlayıb serverə göndərir
 */
function processFiskalId() {
    const input = document.getElementById("fiskalIdInp");
    if (!input) return;

    const value = input.value.trim().toUpperCase();

    // Yalnız 12 rəqəm və hərfdən ibarət olub-olmaması (RegExp)
    if (!/^[A-Z0-9]{12}$/.test(value)) {
        Swal.fire("Diqqət", "Fiskal ID 12 simvoldan ibarət olmalı (yalnız rəqəm və ingilis hərfləri) və boş olmamalıdır.", "warning");
        return;
    }

    Swal.fire({
        title: "Yoxlanılır...",
        text: "Fiskal ID üzrə qəbz məlumatları axtarılır",
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    fetch("/Transaction/ProcessFiskalId", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            "RequestVerificationToken": getCsrfToken()
        },
        body: JSON.stringify({ fiskalId: value })
    })
        .then(async res => {
            const data = await res.json().catch(() => null);
            if (res.ok && data?.success) {
                Swal.fire({
                    icon: "success",
                    title: "Tapıldı!",
                    text: "Qəbz məlumatları uğurla əlavə edildi.",
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => location.reload());
            } else {
                Swal.fire("Xəta", data?.message || "Fiskal ID üzrə məlumat tapılmadı.", "error");
            }
        })
        .catch(() => Swal.fire("Xəta", "Serverlə əlaqə yaratmaq mümkün olmadı", "error"));
}

/**
 * Drag & Drop + Kliklə Fayl Seçimi (Input File)
 */
function initializeDragAndDrop() {
    const dropZone = document.getElementById("receiptDropZone");
    if (!dropZone) return;

    // 1. Hidden file input yaradırıq (kliklə də seçmək olsun)
    let hiddenInput = dropZone.querySelector("input[type='file']");
    if (!hiddenInput) {
        hiddenInput = document.createElement("input");
        hiddenInput.type = "file";
        hiddenInput.style.display = "none";
        hiddenInput.accept = ".jpg,.jpeg,.png,.webp,.pdf";
        dropZone.appendChild(hiddenInput);

        // Zona içindəki button və ya linklərə klik olunduqda dialog açılmasın
        dropZone.addEventListener("click", e => {
            if (e.target.closest("button, a")) return;
            hiddenInput.click();
        });

        // Input-da fayl seçilən kimi uploadReceiptFile çağırsın
        hiddenInput.addEventListener("change", e => {
            const files = e.target.files;
            if (files && files.length > 0) {
                uploadReceiptFile(files[0]);
                hiddenInput.value = ""; // Yenidən eyni faylı seçə bilmək üçün
            }
        });
    }

    // 2. Drag & Drop hadisələri
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, e => {
            e.preventDefault();
            e.stopPropagation();
        }, false);
    });

    ['dragenter', 'dragover'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => dropZone.classList.add('border-primary', 'bg-light'), false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
        dropZone.addEventListener(eventName, () => dropZone.classList.remove('border-primary', 'bg-light'), false);
    });

    dropZone.addEventListener('drop', e => {
        const files = e.dataTransfer.files;
        if (files && files.length > 0) {
            uploadReceiptFile(files[0]);
        }
    });
}

/**
 * Faylı serverə göndərən əsas funksiya (Rəsmi MIME typlar & 0 byte qorumalı)
 */
function uploadReceiptFile(file) {
    // 0 Byte fayl yoxlaması
    if (!file || file.size === 0) {
        Swal.fire("Diqqət", "Seçilmiş fayl boşdur (0 byte).", "warning");
        return;
    }

    // Rəsmi MIME type siyahısı
    const allowedTypes = [
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    ];

    if (!allowedTypes.includes(file.type)) {
        Swal.fire("Diqqət", "Yalnız JPG, PNG, WEBP və ya PDF formatında fayl yükləyə bilərsiniz.", "warning");
        return;
    }

    // Maksimum fayl ölçüsü (5 MB)
    if (file.size > 5 * 1024 * 1024) {
        Swal.fire("Diqqət", "Faylın ölçüsü 5 MB-dan çox olmamalıdır.", "warning");
        return;
    }

    const formData = new FormData();
    formData.append("receiptFile", file);

    Swal.fire({
        title: "Qəbz oxunur...",
        text: "Məlumatlar emal olunur, xahiş olunur gözləyin",
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    fetch("/Transaction/UploadReceipt", {
        method: "POST",
        headers: {
            "RequestVerificationToken": getCsrfToken()
        },
        body: formData
    })
        .then(async res => {
            const data = await res.json().catch(() => null);
            if (res.ok && data?.success) {
                Swal.fire({
                    icon: "success",
                    title: "Uğurlu!",
                    text: data.message || "Qəbz məlumatları oxundu və əməliyyat əlavə edildi.",
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => location.reload());
            } else {
                Swal.fire("Xəta", data?.message || "Qəbz şəkli oxuna bilmədi.", "error");
            }
        })
        .catch(() => {
            Swal.fire("Xəta", "Serverlə əlaqə zamanı xəta baş verdi.", "error");
        });
}

function startQRScanner() {
    Swal.fire("Məlumat", "QR Skaner modulu cihaz kamerası ilə əlaqəyə tam hazırdır.", "info");
}

// ==========================================
// 6. DOMContentLoaded INITIALIZER
// ==========================================

document.addEventListener("DOMContentLoaded", function () {
    // 1. Filtrləmə hadisələri
    document.getElementById("searchInp")?.addEventListener("input", filterTable);
    document.getElementById("typeFilter")?.addEventListener("change", filterTable);

    // 2. Drag & Drop (+ Kliklə Fayl Seçimi)
    initializeDragAndDrop();

    // 3. Flatpickr Təqvim inteqrasiyası
    if (window.flatpickr) {
        const commonDateConfig = {
            locale: "az",
            dateFormat: "Y-m-d", // HTML dataset date-lə eyni olmalıdır: data-date="YYYY-MM-DD"
            altInput: true,
            altFormat: "j F Y",
            allowInput: true,
            disableMobile: true
        };

        startPicker = flatpickr("#startDate", {
            ...commonDateConfig,
            onChange: function (selectedDates, dateStr) {
                if (endPicker) {
                    endPicker.set("minDate", dateStr);
                }
                filterTable();
            }
        });

        endPicker = flatpickr("#endDate", {
            ...commonDateConfig,
            onChange: function (selectedDates, dateStr) {
                if (startPicker) {
                    startPicker.set("maxDate", dateStr);
                }
                filterTable();
            }
        });
    }
});