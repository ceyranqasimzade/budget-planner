function filterTable() {
    var searchVal = document.getElementById("searchInp").value.toLowerCase();
    var typeVal = document.getElementById("typeFilter").value;
    var rows = document.querySelectorAll("#tableBody tr");
    rows.forEach(function (row) {
        if (row.getAttribute("data-type")) {
            var text = row.innerText.toLowerCase();
            var type = row.getAttribute("data-type");
            var matchesSearch = text.includes(searchVal);
            var matchesType = (typeVal === "all" || type === typeVal);
            if (matchesSearch && matchesType) {
                row.style.display = "";
            } else {
                row.style.display = "none";
            }
        }
    });
}
function resetFilters() {
    document.getElementById("searchInp").value = "";
    document.getElementById("typeFilter").value = "all";
    if (document.getElementById("startDate")) document.getElementById("startDate").value = "";
    if (document.getElementById("endDate")) document.getElementById("endDate").value = "";
    filterTable();
}
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
function processFiskalId() {
    const fiskalInp = document.getElementById("fiskalIdInp");
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
document.addEventListener("DOMContentLoaded", function () {
    const dropZone = document.querySelector('.receipt-drop-zone');
    const fileInput = document.getElementById('receiptFileInp');

    if (dropZone && fileInput) {
        dropZone.addEventListener('click', () => fileInput.click());
        fileInput.addEventListener('change', function () {
            if (this.files.length > 0) {
                alert("Şəkil uğurla yükləndi: " + this.files[0].name);
            }
        });
    }
});