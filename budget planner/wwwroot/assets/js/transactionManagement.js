// 1. CANLI FİLTRLƏMƏ VƏ TARİX ARALIĞI SÜZGƏCİ
function filterTable() {
    const searchVal = document.getElementById("searchInp").value.toLowerCase();
    const typeVal = document.getElementById("typeFilter").value;
    const startDate = document.getElementById("startDate").value;
    const endDate = document.getElementById("endDate").value;
    const rows = document.querySelectorAll("#tableBody tr");
    rows.forEach(row => {
        const textContent = row.innerText.toLowerCase();
        const rowType = row.getAttribute("data-type");
        const rowDate = row.getAttribute("data-timestamp");
        let matchesSearch = textContent.includes(searchVal);
        let matchesType = (typeVal === "all") || (rowType === typeVal);
        let matchesDate = true;
        if (startDate && rowDate < startDate) matchesDate = false;
        if (endDate && rowDate > endDate) matchesDate = false;
        if (matchesSearch && matchesType && matchesDate) {
            row.style.display = "";
        } else {
            row.style.display = "none";
        }
    });
}
function resetFilters() {
    document.getElementById("searchInp").value = "";
    document.getElementById("typeFilter").value = "all";
    document.getElementById("startDate").value = "";
    document.getElementById("endDate").value = "";
    filterTable();
}
// 2. DİNAMİK SÜTUN SIRALAMASI
let sortDirections = {};
function sortTable(colIndex, type) {
    const table = document.getElementById("transactionTable");
    const tbody = table.querySelector("tbody");
    const rows = Array.from(tbody.querySelectorAll("tr"));

    sortDirections[colIndex] = !sortDirections[colIndex];
    const isAscending = sortDirections[colIndex];

    rows.sort((rowA, rowB) => {
        let cellA = rowA.cells[colIndex];
        let cellB = rowB.cells[colIndex];

        let valA, valB;

        if (type === 'number') {
            valA = parseFloat(cellA.getAttribute("data-amount")) || 0;
            valB = parseFloat(cellB.getAttribute("data-amount")) || 0;
        } else if (type === 'date') {
            valA = rowA.getAttribute("data-timestamp");
            valB = rowB.getAttribute("data-timestamp");
        } else {
            valA = cellA.innerText.toLowerCase().trim();
            valB = cellB.innerText.toLowerCase().trim();
        }

        if (valA < valB) return isAscending ? -1 : 1;
        if (valA > valB) return isAscending ? 1 : -1;
        return 0;
    });

    rows.forEach(row => tbody.appendChild(row));
}

// 3. EXCEL EKSPORT
function exportTableToCSV(filename) {
    let csv = [];
    const rows = document.querySelectorAll("#transactionTable tr");

    for (let i = 0; i < rows.length; i++) {
        let row = [], cols = rows[i].querySelectorAll("td, th");
        for (let j = 1; j < cols.length - 1; j++) {
            let data = cols[j].innerText.replace(/(\r\n|\n|\r)/gm, "").replace(/₼/g, "AZN").trim();
            row.push('"' + data + '"');
        }
        csv.push(row.join(","));
    }

    let csvFile = new Blob([new Uint8Array([0xEF, 0xBB, 0xBF]), csv.join("\n")], { type: "text/csv;charset=utf-8;" });
    let downloadLink = document.createElement("a");
    downloadLink.download = filename;
    downloadLink.href = window.URL.createObjectURL(csvFile);
    downloadLink.style.display = "none";
    document.body.appendChild(downloadLink);
    downloadLink.click();
    document.body.removeChild(downloadLink);
}

// 4. PDF EKSPORT
function exportTableToPDF() {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();

    doc.setFontSize(18);
    doc.text("Maliyye Hesabati - Emeliyyatlar Siyahisi", 14, 20);
    doc.setFontSize(10);
    doc.text(`Tarix: ${new Date().toLocaleDateString()}`, 14, 28);

    let bodyData = [];
    const rows = document.querySelectorAll("#tableBody tr");

    rows.forEach(row => {
        if (row.style.display !== "none") {
            let cells = row.querySelectorAll("td");
            bodyData.push([
                cells[1].innerText,
                cells[2].innerText,
                cells[3].innerText,
                cells[4].innerText
            ]);
        }
    });

    doc.autoTable({
        startY: 35,
        head: [['Tarix', 'Tesvir', 'Kateqoriya', 'Mebleg']],
        body: bodyData,
        theme: 'striped',
        headStyles: { fillColor: [33, 37, 41] },
        styles: { font: "Helvetica", fontSize: 10 }
    });

    doc.save("əməliyyatlar_hesabatı.pdf");
}

// 5. TOPLU SİLMƏ
function toggleSelectAll(masterCheckbox) {
    const checkboxes = document.querySelectorAll(".row-checkbox");
    checkboxes.forEach(cb => {
        if (cb.closest('tr').style.display !== 'none') {
            cb.checked = masterCheckbox.checked;
        }
    });
    updateBulkDeleteButton();
}

function updateBulkDeleteButton() {
    const checkedBoxes = document.querySelectorAll(".row-checkbox:checked");
    const bulkBtn = document.getElementById("btnBulkDelete");
    const countSpan = document.getElementById("selectedCount");

    if (checkedBoxes.length > 0) {
        bulkBtn.classList.remove("d-none");
        countSpan.innerText = checkedBoxes.length;
    } else {
        bulkBtn.classList.add("d-none");
        document.getElementById("selectAll").checked = false;
    }
}
function bulkDeleteTransactions() {
    const checkedBoxes = document.querySelectorAll(".row-checkbox:checked");
    let ids = Array.from(checkedBoxes).map(cb => cb.value);

    if (confirm(`Seçilmiş ${ids.length} əməliyyatı birdəfəlik silmək istədiyinizdən əminsiniz?`)) {
        alert("Backend linki qoşulanda bu ID-lər silinəcək: " + ids.join(", "));
    }
}
/* wwwroot/assets/js/transactionManagement.js */

document.addEventListener("DOMContentLoaded", function () {
    // Əgər səhifədə date-picker varsa, işə sal
    if (document.querySelectorAll(".date-picker").length > 0) {
        flatpickr(".date-picker", {
            locale: "az",
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d.m.Y",
            onChange: function (selectedDates, dateStr, instance) {
                if (typeof filterTable === "function") {
                    filterTable();
                }
            }
        });
    }
});