// ==========================================
// 0. GLOBAL DEYİŞƏNLƏR
// ==========================================
let startPicker = null;
let endPicker = null;
let currentPage = 1;
let rowsPerPage = 10;

// ==========================================
// 1. HELPER FUNCTIONS
// ==========================================
/**
 * Ekranda görünən sətirlərdəki checkbox-ları tapır.
 */
function getVisibleCheckboxes() {
    return [...document.querySelectorAll("#tableBody tr")]
        .filter(row => row.style.display !== "none" && row.id !== "noDataRow" && row.querySelector(".row-checkbox"))
        .map(row => row.querySelector(".row-checkbox"));
}

/**
 * CSRF tokenini əldə edir.
 */
function getCsrfToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
}

/**
 * Dinamik olaraq seçilmiş sətir sayını oxuyur (10, 25, 50 və s.)
 */
function getPageSize() {
    const pageSizeSelect = document.getElementById("rowsPerPage") || document.getElementById("pageSizeSelect");
    if (pageSizeSelect) {
        return parseInt(pageSizeSelect.value, 10) || 10;
    }
    return rowsPerPage;
}

// ==========================================
// 2. TABLE FILTER, DYNAMIC PAGINATION & NO-DATA MESSAGING
// ==========================================
function filterTable() {
    rowsPerPage = getPageSize();

    const searchVal = (document.getElementById("searchInp")?.value || "").trim().toLocaleLowerCase('az-AZ');
    const typeVal = document.getElementById("typeFilter")?.value || "all";
    const startVal = document.getElementById("startDate")?.value || "";
    const endVal = document.getElementById("endDate")?.value || "";
    const tableBody = document.getElementById("tableBody");

    if (!tableBody) return;
    const rows = Array.from(tableBody.querySelectorAll("tr:not(#noDataRow)"));

    // 1. Filtrləmə məntiqi (data-type və data-date təhlükəsiz oxunur)
    const matchingRows = rows.filter(row => {
        const dataType = row.dataset.type || "all";
        const rowDate = row.dataset.date || "";

        const text = (row.textContent || "").toLocaleLowerCase('az-AZ');
        const matchesSearch = !searchVal || text.includes(searchVal);
        const matchesType = (typeVal === "all" || dataType === typeVal);
        const matchesStart = (!startVal || !rowDate || rowDate >= startVal);
        const matchesEnd = (!endVal || !rowDate || rowDate <= endVal);

        return matchesSearch && matchesType && matchesStart && matchesEnd;
    });

    const totalMatching = matchingRows.length;
    const maxPage = Math.ceil(totalMatching / rowsPerPage) || 1;
    if (currentPage > maxPage) currentPage = maxPage;

    const startIndex = (currentPage - 1) * rowsPerPage;
    const endIndex = startIndex + rowsPerPage;

    // 2. Bütün sətirləri gizlədirik
    rows.forEach(row => {
        row.style.display = "none";
        const checkbox = row.querySelector(".row-checkbox");
        if (checkbox) checkbox.checked = false;
    });

    // 3. Cari səhifə diapazonuna düşən sətirləri göstəririk
    matchingRows.forEach((row, idx) => {
        if (idx >= startIndex && idx < endIndex) {
            row.style.display = "";
        }
    });

    toggleNoDataMessage(totalMatching === 0);
    renderPagination(totalMatching);
    updateSelectAllStatus();
    updateBulkDeleteButton();
}

function renderPagination(totalItems) {
    let container = document.getElementById("paginationContainer");

    if (!container) {
        const tableContainer = document.querySelector(".table-responsive") || document.getElementById("transactionTable")?.parentNode;
        if (!tableContainer) return;

        container = document.createElement("div");
        container.id = "paginationContainer";
        container.className = "d-flex align-items-center justify-content-between mt-3 px-2";
        tableContainer.after(container);
    }

    const totalPages = Math.ceil(totalItems / rowsPerPage);
    const startItem = totalItems === 0 ? 0 : (currentPage - 1) * rowsPerPage + 1;
    const endItem = Math.min(currentPage * rowsPerPage, totalItems);

    let html = `<span class="small text-white-50">${totalItems} əməliyyatdan ${startItem}-${endItem} arası göstərilir</span>`;

    if (totalPages > 1) {
        html += `<ul class="pagination pagination-sm mb-0">`;

        // Geri
        html += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                    <button type="button" class="page-link bg-dark text-light border-secondary" onclick="changePage(${currentPage - 1})">&laquo;</button>
                 </li>`;

        // Nömrələr
        for (let i = 1; i <= totalPages; i++) {
            const activeClass = i === currentPage ? 'bg-primary text-white border-primary fw-bold' : 'bg-dark text-light border-secondary';
            html += `<li class="page-item ${i === currentPage ? 'active' : ''}">
                        <button type="button" class="page-link ${activeClass}" onclick="changePage(${i})">${i}</button>
                     </li>`;
        }

        // İrəli
        html += `<li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                    <button type="button" class="page-link bg-dark text-light border-secondary" onclick="changePage(${currentPage + 1})">&raquo;</button>
                 </li>`;

        html += `</ul>`;
    }

    container.innerHTML = html;
}

function changePage(page) {
    currentPage = page;
    filterTable();
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
                <td colspan="10" class="text-center py-4 text-white-50">
                    <i class="fa-solid fa-magnifying-glass fs-2 d-block mb-2 opacity-50"></i>
                    <p class="mb-0 fw-semibold">Axtarışınıza uyğun heç bir əməliyyat tapılmadı.</p>
                </td>
            `;
            tbody.appendChild(noDataRow);
        }
        noDataRow.style.display = "";
    } else if (noDataRow) {
        noDataRow.style.display = "none";
    }
}

function resetFilters() {
    currentPage = 1;
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
// 4. BULK & SINGLE DELETE (SweetAlert2)
// ==========================================
function bulkDeleteTransactions() {
    const selectedCheckboxes = getVisibleCheckboxes().filter(x => x.checked);
    const ids = selectedCheckboxes.map(x => Number(x.value));

    if (ids.length === 0) {
        Swal.fire('Xəbərdarlıq', 'Lütfən silmək üçün ən azı bir əməliyyat seçin.', 'warning');
        return;
    }

    Swal.fire({
        title: "Əminsiniz?",
        text: `${ids.length} ədəd əməliyyat silinəcək! Bu əməliyyat geri qaytarıla bilməz!`,
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
                    selectedCheckboxes.forEach(cb => cb.closest("tr")?.remove());
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
function initSingleDeleteButtons() {
    document.querySelectorAll('.delete-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const deleteUrl = this.getAttribute('data-url');
            const description = this.getAttribute('data-description') || 'Bu əməliyyat';

            if (!deleteUrl) return;

            Swal.fire({
                title: 'Silmək istədiyinizə əminsiniz?',
                text: `"${description}" silinəcək!`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc3545',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Bəli, sil!',
                cancelButtonText: 'Ləğv et',
                reverseButtons: true
            }).then((result) => {
                if (result.isConfirmed) {
                    fetch(deleteUrl, {
                        method: 'POST',
                        headers: {
                            'RequestVerificationToken': getCsrfToken()
                        }
                    })
                        .then(res => {
                            if (res.ok) {
                                Swal.fire({
                                    icon: 'success',
                                    title: 'Silindi!',
                                    text: 'Əməliyyat silindi.',
                                    timer: 1200,
                                    showConfirmButton: false
                                }).then(() => {
                                    window.location.reload();
                                });
                            } else {
                                Swal.fire('Xəta!', 'Silinmə həyata keçirilmədi.', 'error');
                            }
                        })
                        .catch(() => Swal.fire('Xəta!', 'Server xətası baş verdi.', 'error'));
                }
            });
        });
    });
}

// ==========================================
// 5. SÜNİ İNTELLEKT İLƏ QƏBZ OXUTMA
// ==========================================
document.addEventListener("DOMContentLoaded", function () {
    const dropZone = document.getElementById('receiptDropZone');
    const fileInput = document.getElementById('receiptFileInp');
    const defaultState = document.getElementById('dropZoneDefault');
    const processingState = document.getElementById('dropZoneProcessing');

    if (!dropZone || !fileInput) return; // Səhifədə dropzone yoxdursa işləməsin

    // İstənilən yerinə kliklədikdə fayl seçimi açılsın
    dropZone.addEventListener('click', () => fileInput.click());

    // Sürüşdürüb üstünə gətirəndə dizayn dəyişsin
    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.style.backgroundColor = 'rgba(156, 39, 176, 0.05)';
        dropZone.style.borderColor = '#9c27b0';
    });

    // Kənara çıxanda əvvəlki halına qayıtsın
    dropZone.addEventListener('dragleave', () => {
        dropZone.style.backgroundColor = 'transparent';
    });

    // Şəkli bıraxdıqda
    dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.style.backgroundColor = 'transparent';

        if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
            handleReceiptImage(e.dataTransfer.files[0]);
        }
    });

    // Fayl seçici ilə şəkli seçdikdə
    fileInput.addEventListener('change', (e) => {
        if (e.target.files && e.target.files.length > 0) {
            handleReceiptImage(e.target.files[0]);
        }
    });

    function handleReceiptImage(file) {
        // Yalnız şəkil və pdf yoxlanışı
        const allowedTypes = ["image/jpeg", "image/png", "image/webp", "image/jpg", "application/pdf"];
        if (!allowedTypes.includes(file.type)) {
            Swal.fire('Xəta!', 'Zəhmət olmasa yalnız şəkil (PNG, JPG, WEBP) və ya PDF formatında fayl yükləyin.', 'error');
            fileInput.value = '';
            return;
        }

        if (file.size > 5 * 1024 * 1024) {
            Swal.fire("Diqqət", "Faylın ölçüsü 5 MB-dan çox olmamalıdır.", "warning");
            fileInput.value = '';
            return;
        }

        // UI Dəyişikliyi - Yüklənmə animasiyasını göstər
        if (defaultState) defaultState.classList.add('d-none');
        if (processingState) processingState.classList.remove('d-none');
        dropZone.style.pointerEvents = 'none'; // Proses bitənə qədər klikləməni bağla

        const formData = new FormData();
        formData.append('receiptFile', file); // Controller-dəki parametrlə tam eyni (receiptFile)

        // CSRF Tokeni tapmaq (Həm input-dan, həm cookie-dən təhlükəsiz tapır)
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const headers = {};
        if (tokenInput) {
            headers["RequestVerificationToken"] = tokenInput.value;
            headers["X-CSRF-TOKEN"] = tokenInput.value;
        }

        // Backend-ə (C#-a) sorğu göndəririk - URL düzəldildi: /Transaction/ProcessReceiptImage
        fetch('/Transaction/ProcessReceiptImage', {
            method: 'POST',
            headers: headers,
            body: formData
        })
            .then(async response => {
                if (!response.ok) {
                    const errorData = await response.json().catch(() => ({}));
                    throw new Error(errorData.message || `Server xətası: ${response.status}`);
                }
                return response.json();
            })
            .then(data => {
                if (data.success) {
                    Swal.fire({
                        icon: "success",
                        title: "Uğurlu!",
                        text: data.message || "Qəbz məlumatları oxundu və əməliyyat əlavə edildi.",
                        timer: 2000,
                        showConfirmButton: false
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    Swal.fire('Oxunmadı', data.message || 'Qəbzdəki məlumatları təyin etmək mümkün olmadı.', 'warning');
                    resetDropZone();
                }
            })
            .catch(err => {
                console.error(err);
                Swal.fire('Sistem Xətası', err.message || 'Şəkil analiz edilərkən xəta baş verdi. Zəhmət olmasa bir az sonra təkrar yoxlayın.', 'error');
                resetDropZone();
            });
    }

    function resetDropZone() {
        if (defaultState) defaultState.classList.remove('d-none');
        if (processingState) processingState.classList.add('d-none');
        dropZone.style.pointerEvents = 'auto';
        if (fileInput) fileInput.value = ''; // Input-u sıfırla ki eyni faylı yenidən seçmək olsun
    }
});


// ==========================================
// 6. EXCEL (CSV) VƏ PDF EXPORT FUNKSİYALARI
// ==========================================
function exportTableToCSV(filename = 'əməliyyatlar.csv') {
    const table = document.getElementById('transactionTable');
    if (!table) return;
    let csv = [];
    let rows = table.querySelectorAll('tr');

    for (let i = 0; i < rows.length; i++) {
        if (rows[i].style.display === 'none' || rows[i].id === 'noDataRow') continue;

        let row = [];
        let cols = rows[i].querySelectorAll('th, td');

        for (let j = 1; j < cols.length - 1; j++) {
            let text = cols[j].innerText || cols[j].textContent || '';
            text = text.replace(/(\r\n|\n|\r)/gm, ' ').replace(/\s+/g, ' ').trim();
            text = text.replace(/"/g, '""');
            row.push(`"${text}"`);
        }

        if (row.length > 0) csv.push(row.join(','));
    }

    if (csv.length <= 1) {
        Swal.fire('Xəbərdarlıq', 'İxrac ediləcək aktiv əməliyyat tapılmadı.', 'warning');
        return;
    }

    let csvContent = '\ufeff' + csv.join('\n');
    let blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    let link = document.createElement('a');

    link.href = URL.createObjectURL(blob);
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function fixAzChars(str) {
    if (!str) return '';
    return str
        .replace(/Ə/g, 'E').replace(/ə/g, 'e')
        .replace(/I/g, 'I').replace(/ı/g, 'i')
        .replace(/İ/g, 'I')
        .replace(/Ş/g, 'S').replace(/ş/g, 's')
        .replace(/Ç/g, 'C').replace(/ç/g, 'c')
        .replace(/Ğ/g, 'G').replace(/ğ/g, 'g')
        .replace(/Ö/g, 'O').replace(/ö/g, 'o')
        .replace(/Ü/g, 'U').replace(/ü/g, 'u')
        .replace(/₼/g, 'AZN');
}

function exportTableToPDF() {
    if (!window.jspdf || !window.jspdf.jsPDF) {
        Swal.fire('Xəta', 'PDF generator kitabxanası (jsPDF) yüklənməyib!', 'error');
        return;
    }

    const { jsPDF } = window.jspdf;
    const doc = new jsPDF('p', 'mm', 'a4');

    doc.setFontSize(16);
    doc.text(fixAzChars("Əməliyyatlar Paneli Hesabatı"), 14, 15);
    doc.setFontSize(9);
    doc.text(fixAzChars(`Tarix: ${new Date().toLocaleDateString('az-AZ')}`), 14, 22);

    const table = document.getElementById('transactionTable');
    if (!table) return;

    const headers = [fixAzChars("Tarix"), fixAzChars("Təsvir"), fixAzChars("Kateqoriya"), fixAzChars("Məbləğ"), fixAzChars("Status")];
    const bodyData = [];
    const rows = table.querySelectorAll('tbody tr');

    rows.forEach(row => {
        if (row.style.display === 'none' || row.id === 'noDataRow') return;

        const cells = row.querySelectorAll('td');
        if (cells.length >= 6) {
            bodyData.push([
                fixAzChars(cells[1].innerText.trim()),
                fixAzChars(cells[2].innerText.trim()),
                fixAzChars(cells[3].innerText.trim()),
                fixAzChars(cells[4].innerText.trim()),
                fixAzChars(cells[5].innerText.trim())
            ]);
        }
    });

    if (bodyData.length === 0) {
        Swal.fire('Xəbərdarlıq', 'İxrac ediləcək aktiv əməliyyat tapılmadı.', 'warning');
        return;
    }

    doc.autoTable({
        head: [headers],
        body: bodyData,
        startY: 26,
        styles: { fontSize: 8, cellPadding: 2.5 },
        headStyles: { fillColor: [13, 110, 253], textColor: 255, fontStyle: 'bold' },
        alternateRowStyles: { fillColor: [248, 249, 250] }
    });

    doc.save('əməliyyatlar_hesabat.pdf');
}
// ==========================================
// 7. INITIALIZER
// ==========================================
document.addEventListener("DOMContentLoaded", function () {
    // 7.1 Filtrlər və Axtarış
    document.getElementById("searchInp")?.addEventListener("input", () => { currentPage = 1; filterTable(); });
    document.getElementById("typeFilter")?.addEventListener("change", () => { currentPage = 1; filterTable(); });

    // 7.2 Sətir sayı seçimi (10, 25, 50 sətir)
    const pageSizeSelect = document.getElementById("pageSizeSelect") || document.querySelector(".page-size-select");
    if (pageSizeSelect) {
        pageSizeSelect.addEventListener("change", function () {
            currentPage = 1;
            filterTable();
        });
    }

    // İlk yüklənmə
    filterTable();

    // Modullar (KÖHNƏ DROPZONE ÇAĞIRIŞI BURADAN SİLİNDİ)
    initSingleDeleteButtons();

    // 7.3 Flatpickr (Tarixlər)
    if (window.flatpickr) {
        const commonDateConfig = {
            locale: "az",
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "j F Y",
            allowInput: true,
            disableMobile: true
        };
        startPicker = flatpickr("#startDate", {
            ...commonDateConfig,
            onChange: function (selectedDates, dateStr) {
                if (endPicker) endPicker.set("minDate", dateStr);
                currentPage = 1;
                filterTable();
            }
        });
        endPicker = flatpickr("#endDate", {
            ...commonDateConfig,
            onChange: function (selectedDates, dateStr) {
                if (startPicker) startPicker.set("maxDate", dateStr);
                currentPage = 1;
                filterTable();
            }
        });

        const dateInput = document.getElementById("azDatePicker");
        if (dateInput) {
            flatpickr(dateInput, {
                enableTime: true,
                dateFormat: "Y-m-d H:i",
                time_24hr: true,
                locale: "az"
            });
        }
    }
});

// ==========================================
// SƏTİR SAYINI DƏYİŞƏN FUNKSİYA (GLOBAL)
// ==========================================
function changeRowsPerPage(elementOrValue) {
    let val = typeof elementOrValue === 'object' && elementOrValue !== null ? elementOrValue.value : elementOrValue;
    rowsPerPage = parseInt(val, 10) || 10;
    currentPage = 1;
    filterTable();
}