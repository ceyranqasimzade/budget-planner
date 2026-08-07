// ==========================================
// ƏMƏLİYYATA DÜZƏLİŞ (EDIT TRANSACTION) SCRIPTI
// ==========================================

/**
 * Cədvəldən "Düzəliş et" düyməsinə kliklədikdə Modal pəncərəni açır və datanı doldurur
 * @param {number} id - Əməliyyatın ID-si
 */
async function openEditModal(id) {
    try {
        // Backend-dən əməliyyatın cari məlumatlarını gətiririk
        const response = await fetch(`/Transaction/GetEditData/${id}`);

        if (!response.ok) {
            throw new Error('Əməliyyat məlumatları yüklənə bilmədi.');
        }

        const data = await response.json();

        if (!data.success) {
            Swal.fire('Xəta', data.message || 'Məlumat tapılmadı.', 'error');
            return;
        }

        const item = data.data;

        // Modal daxilindəki inputları doldururuq
        document.getElementById('editTransactionId').value = item.id;
        document.getElementById('editDescription').value = item.description;
        document.getElementById('editAmount').value = item.amount;
        document.getElementById('editCategory').value = item.category || '';
        document.getElementById('editCurrency').value = item.currency || 'AZN';

        // Gəlir / Xərc radio düymələri
        if (item.isIncome) {
            document.getElementById('editTypeIncome').checked = true;
        } else {
            document.getElementById('editTypeExpense').checked = true;
        }

        // Modalı göstəririk (Bootstrap 5)
        const modalElement = document.getElementById('editTransactionModal');
        const modal = new bootstrap.Modal(modalElement);
        modal.show();

    } catch (error) {
        console.error("Edit load error:", error);
        Swal.fire('Xəta', 'Düzəliş məlumatları yüklənərkən texniki xəta baş verdi.', 'error');
    }
}

// Form göndərilərkən (Submit) AJAX istifadəsi
document.addEventListener('DOMContentLoaded', () => {
    const editForm = document.getElementById('editTransactionForm');

    if (editForm) {
        editForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const formData = new FormData(this);
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            try {
                const response = await fetch('/Transaction/Edit', {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'RequestVerificationToken': token || ''
                    }
                });

                const result = await response.json();

                if (result.success) {
                    Swal.fire({
                        title: 'Uğurlu!',
                        text: result.message || 'Əməliyyat uğurla yeniləndi.',
                        icon: 'success',
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        window.location.reload(); // Cədvəli və balansları yeniləyirik
                    });
                } else {
                    Swal.fire('Xəta', result.message || 'Yenilənmə zamanı xəta baş verdi.', 'error');
                }
            } catch (err) {
                console.error("Edit submit error:", err);
                Swal.fire('Xəta', 'Serverlə əlaqə zamanı xəta baş verdi.', 'error');
            }
        });
    }
});
document.addEventListener("DOMContentLoaded", function () {
    const dateInput = document.getElementById("azDatePicker");

    // Flatpickr-ın yükləndiyini və input-un varlığını yoxlayırıq
    if (dateInput && typeof flatpickr !== "undefined") {
        flatpickr(dateInput, {
            enableTime: true,
            dateFormat: "Y-m-d H:i",
            time_24hr: true,
            allowInput: false,
            disableMobile: true, // Mobil brauzerin mane olmasını engəlləyir

            // Xarici az.js-dən asılılığı aradan qaldırmaq üçün Azərbaycan dilini burada təyin edirik:
            locale: {
                firstDayOfWeek: 1,
                weekdays: {
                    shorthand: ["B.", "B.E.", "Ç.Ə.", "Ç.", "C.A.", "C.", "Ş."],
                    longhand: ["Bazar", "Bazar ertəsi", "Çərşənbə axşamı", "Çərşənbə", "Cümə axşamı", "Cümə", "Şənbə"]
                },
                months: {
                    shorthand: ["Yan", "Fev", "Mar", "Apr", "May", "İyn", "İyl", "Avq", "Sen", "Okt", "Noy", "Dek"],
                    longhand: ["Yanvar", "Fevral", "Mart", "Aprel", "May", "İyun", "İyul", "Avqust", "Sentyabr", "Oktyabr", "Noyabr", "Dekabr"]
                }
            }
        });
    } else {
        console.error("Flatpickr və ya #azDatePicker tapılmadı!");
    }
});