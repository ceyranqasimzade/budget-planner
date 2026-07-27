// ==========================================
// TARİX SEÇİCİ (FLATPICKR)
// ==========================================
document.addEventListener("DOMContentLoaded", function () {
    const dateInput = document.querySelector("#transactionDate");
    if (dateInput && typeof flatpickr !== 'undefined') {
        flatpickr("#transactionDate", {
            locale: "az",
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d.m.Y",
            allowInput: true
        });
    }
});

// ==========================================
// CƏDVƏL FİLTRLƏMƏ VƏ SIFIRLAMA
// ==========================================
function filterTable() {
    const searchInp = document.getElementById("searchInp");
    const typeFilter = document.getElementById("typeFilter");
    const rows = document.querySelectorAll("#tableBody tr");

    if (!rows.length) return;

    const searchVal = searchInp ? searchInp.value.toLowerCase() : "";
    const typeVal = typeFilter ? typeFilter.value : "all";

    rows.forEach(function (row) {
        const dataType = row.getAttribute("data-type");
        if (dataType) {
            const text = row.innerText.toLowerCase();
            const matchesSearch = text.includes(searchVal);
            const matchesType = (typeVal === "all" || dataType === typeVal);

            if (matchesSearch && matchesType) {
                row.style.display = "";
            } else {
                row.style.display = "none";
            }
        }
    });
}

function resetFilters() {
    const searchInp = document.getElementById("searchInp");
    const typeFilter = document.getElementById("typeFilter");
    const startDate = document.getElementById("startDate");
    const endDate = document.getElementById("endDate");

    if (searchInp) searchInp.value = "";
    if (typeFilter) typeFilter.value = "all";
    if (startDate) startDate.value = "";
    if (endDate) endDate.value = "";

    filterTable();
}

// ==========================================
// ÇOXLU SEÇİM VƏ SİLMƏ DÜYMƏSİ
// ==========================================
function toggleSelectAll(source) {
    var checkboxes = document.querySelectorAll('.row-checkbox');
    checkboxes.forEach(function (chk) {
        chk.checked = source.checked;
    });
    updateBulkDeleteButton();
}

function updateBulkDeleteButton() {
    var checkboxes = document.querySelectorAll('.row-checkbox:checked');
    var btnBulk = document.getElementById("btnBulkDelete");
    var countSpan = document.getElementById("selectedCount");

    if (btnBulk && countSpan) {
        countSpan.innerText = checkboxes.length;
        if (checkboxes.length > 0) {
            btnBulk.classList.remove("d-none");
        } else {
            btnBulk.classList.add("d-none");
        }
    }
}

// ==========================================
// FİSKAL ID VƏ QR Skaner
// ==========================================
function processFiskalId() {
    const fiskalInp = document.getElementById("fiskalIdInp");
    if (!fiskalInp) return;

    const idValue = fiskalInp.value.trim();

    if (idValue.length !== 12) {
        alert("Xahiş olunur 12 simvollu düzgün Fiskal ID daxil edin.");
        return;
    }
    alert("Fiskal ID qəbul olundu: " + idValue.toUpperCase() + "\nMəlumatlar yoxlanılır...");
    fiskalInp.value = "";
}

function startQRScanner() {
    alert("Kamera icazəsi istənilir... \n(Gələcəkdə bura html5-qrcode kitabxanası ilə kamera görüntüsü inteqrasiya ediləcək)");
}

// ==========================================
// QƏBZ YÜKLƏMƏ VƏ DRAG & DROP
// ==========================================
document.addEventListener("DOMContentLoaded", function () {
    const dropZone = document.querySelector('.receipt-drop-zone');
    const fileInput = document.getElementById('receiptFileInp');

    if (dropZone && fileInput) {
        // Kliklə seçmə
        dropZone.addEventListener('click', () => fileInput.click());

        // Fayl seçiləndə
        fileInput.addEventListener('change', function () {
            if (this.files.length > 0) {
                alert("Şəkil uğurla yükləndi: " + this.files[0].name);
            }
        });

        // Sürükləyib buraxma (Drag & Drop)
        dropZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropZone.classList.add('drag-over');
        });

        dropZone.addEventListener('dragleave', () => {
            dropZone.classList.remove('drag-over');
        });

        dropZone.addEventListener('drop', (e) => {
            e.preventDefault();
            dropZone.classList.remove('drag-over');

            if (e.dataTransfer.files.length > 0) {
                fileInput.files = e.dataTransfer.files;
                alert("Şəkil uğurla yükləndi: " + e.dataTransfer.files[0].name);
            }
        });
    }
});