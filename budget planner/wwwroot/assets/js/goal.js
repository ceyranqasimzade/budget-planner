// Hədəf və ya hər hansı elementi SweetAlert ilə silmək üçün funksiya
function confirmDelete(formId, goalName) {
    Swal.fire({
        title: 'Silmək istədiyinizə əminsiniz?',
        text: `"${goalName}" hədəfi birdəfəlik silinəcək!`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Bəli, sil!',
        cancelButtonText: 'Ləğv et'
    }).then((result) => {
        if (result.isConfirmed) {
            document.getElementById(formId).submit();
        }
    });
}