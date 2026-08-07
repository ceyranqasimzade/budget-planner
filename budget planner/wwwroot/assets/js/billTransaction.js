var html5QrcodeScanner = window.html5QrcodeScanner || null;

// ==========================================
// 1. QR CODE SCANNER LOGIC
// ==========================================
function startQRScanner() {
    const modalEl = document.getElementById('qrModal');
    if (!modalEl) return;

    const qrModal = bootstrap.Modal.getOrCreateInstance(modalEl);
    qrModal.show();

    // Modal bağlandıqda kameranı zəmanətli dayandırırıq
    modalEl.addEventListener('hidden.bs.modal', stopQRScanner, { once: true });

    if (!html5QrcodeScanner) {
        html5QrcodeScanner = new Html5QrcodeScanner("reader", { fps: 10, qrbox: 250 }, false);
    }
    html5QrcodeScanner.render(onScanSuccess, onScanFailure);
}

function stopQRScanner() {
    if (html5QrcodeScanner) {
        html5QrcodeScanner.clear().catch(err => console.error("QR Dayandırılma xətası:", err));
    }
}

function onScanSuccess(decodedText, decodedResult) {
    stopQRScanner();

    const modalEl = document.getElementById('qrModal');
    if (modalEl) {
        const modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (modalInstance) modalInstance.hide();
    }

    try {
        let url = new URL(decodedText);
        let fiscalId = url.searchParams.get("n") || "";
        let amount = url.searchParams.get("s") || "";

        window.location.href = `/Transaction/Create?amount=${encodeURIComponent(amount)}&fiscalId=${encodeURIComponent(fiscalId)}`;
    } catch (e) {
        window.location.href = `/Transaction/Create?fiscalId=${encodeURIComponent(decodedText)}`;
    }
}

function onScanFailure(error) {
    // Skaner axtarışda olarkən davamlı tetiklenen xətaları saxlayırıq
}

// ==========================================
// 2. MANUAL FISKAL ID LOGIC
// ==========================================
function processFiskalId() {
    const fiskalInp = document.getElementById('fiskalIdInp');
    const val = fiskalInp ? fiskalInp.value.trim() : '';

    if (!val || val.length < 10) {
        Swal.fire({
            icon: 'warning',
            title: 'Düzgün Fiskal ID daxil edin!',
            text: 'Fiskal ID ən azı 10 simvoldan ibarət olmalıdır.',
            confirmButtonColor: '#ffc107'
        });
        return;
    }
    window.location.href = `/Transaction/Create?fiscalId=${encodeURIComponent(val)}`;
}

// ==========================================
// 3. AI RECEIPT UPLOAD (DRAG & DROP) & FILE DISPLAY
// ==========================================
document.addEventListener('DOMContentLoaded', function () {
    const dropZone = document.getElementById('receiptDropZone');
    const fileInput = document.getElementById('receiptFileInp');
    const fileNameDisplay = document.getElementById('fileNameDisplay');

    // Fayl seçildikdə adın ekranda yenilənməsi məntiqi
    if (fileInput && fileNameDisplay) {
        fileInput.addEventListener('change', function () {
            if (this.files && this.files.length > 0) {
                fileNameDisplay.textContent = this.files[0].name;
            } else {
                fileNameDisplay.textContent = 'Fayl seçilməyib';
            }
        });
    }

    if (dropZone && fileInput) {
        dropZone.addEventListener('click', () => fileInput.click());

        dropZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            dropZone.classList.add('border-purple');
        });

        dropZone.addEventListener('dragleave', () => {
            dropZone.classList.remove('border-purple');
        });

        dropZone.addEventListener('drop', (e) => {
            e.preventDefault();
            dropZone.classList.remove('border-purple');
            if (e.dataTransfer.files.length > 0) {
                // Drag & Drop zamanı fayl adını da yeniləyirik
                if (fileNameDisplay) {
                    fileNameDisplay.textContent = e.dataTransfer.files[0].name;
                }
                handleFileUpload(e.dataTransfer.files[0]);
            }
        });

        fileInput.addEventListener('change', () => {
            if (fileInput.files.length > 0) {
                handleFileUpload(fileInput.files[0]);
            }
        });
    }
});

function handleFileUpload(file) {
    // Fayl növünün yoxlanılması
    if (!file || !file.type.startsWith('image/')) {
        Swal.fire({
            icon: 'error',
            title: 'Yanlış Fayl Formatı',
            text: 'Lütfən yalnız şəkil faylı (JPG, PNG və s.) yükləyin.',
            confirmButtonColor: '#dc3545'
        });
        return;
    }

    Swal.fire({
        title: '<i class="fa-solid fa-spinner fa-spin text-primary me-2"></i>Şəkil Oxunur...',
        text: 'AI qəbzi təhlil edir, zəhmət olmasa gözləyin.',
        allowOutsideClick: false,
        showConfirmButton: false
    });

    let formData = new FormData();
    formData.append('receiptImage', file);

    // Anti-Forgery Token mövcuddursa headers-ə əlavə edirik
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    let headers = {};
    if (tokenInput) {
        headers['RequestVerificationToken'] = tokenInput.value;
    }

    fetch('/Transaction/ProcessReceiptImage', {
        method: 'POST',
        headers: headers, // Headers bura əlavə olundu
        body: formData
    })
        .then(response => {
            if (!response.ok) {
                throw new Error("Server cavab vermədi.");
            }
            return response.json();
        })
        .then(data => {
            if (data.success) {
                Swal.fire({
                    title: '<i class="fa-solid fa-circle-check text-success me-2"></i>Qəbz Oxundu!',
                    text: 'Formaya yönləndirilirsiniz...',
                    icon: 'success',
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => {
                    const amount = encodeURIComponent(data.amount || '');
                    const store = encodeURIComponent(data.store || '');
                    const description = encodeURIComponent(data.description || ''); // Description əlavə olundu
                    const date = encodeURIComponent(data.date || '');

                    // URL-ə description da daxil edildi
                    window.location.href = `/Transaction/Create?amount=${amount}&store=${store}&description=${description}&date=${date}`;
                });
            } else {
                Swal.fire({
                    icon: 'warning',
                    title: 'Xəta Baş Verdi',
                    text: data.message || 'Məlumat oxunmadı.',
                    confirmButtonColor: '#ffc107'
                });
            }
        })
        .catch(err => {
            console.error(err);
            Swal.fire({
                icon: 'error',
                title: 'Xəta Baş Verdi',
                text: 'Şəkil göndərilərkən xəta yarandı.',
                confirmButtonColor: '#dc3545'
            });
        })
        .finally(() => {
            // Eyni şəkil təkrar seçilə bilsin deyə input və ad sıfırlanır
            const fileInput = document.getElementById('receiptFileInp');
            const fileNameDisplay = document.getElementById('fileNameDisplay');
            if (fileInput) fileInput.value = '';
            if (fileNameDisplay) fileNameDisplay.textContent = 'Fayl seçilməyib';
        });
}