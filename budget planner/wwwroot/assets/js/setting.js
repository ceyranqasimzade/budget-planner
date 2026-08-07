document.addEventListener("DOMContentLoaded", function () {

    // ==========================================
    // SABİTLƏR (CONSTANTS) VƏ MÖVZU
    // ==========================================
    const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
    const EMERALD = "#10b981";

    // Backend ilə sinxron olan qəbul edilmiş formatlar
    const ALLOWED_TYPES = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    const swalTheme = {
        background: "#1e293b",
        color: "#fff",
        confirmButtonColor: EMERALD
    };

    // ==========================================
    // ORTAQ FUNKSİYALAR
    // ==========================================

    function showLoading() {
        Swal.fire({
            ...swalTheme,
            title: "Gözləyin...",
            text: "Sorğu göndərilir.",
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false,
            didOpen: () => Swal.showLoading()
        });
    }

    function attachConfirmation(buttonId, options) {
        const btn = document.getElementById(buttonId);
        if (!btn) return;

        btn.addEventListener("click", function (e) {
            e.preventDefault();
            const form = this.closest("form");
            const currentBtn = this;

            Swal.fire({ ...swalTheme, ...options }).then((result) => {
                if (result.isConfirmed) {
                    if (form) {
                        showLoading();
                        currentBtn.disabled = true;
                        form.submit();
                    }
                }
            });
        });
    }

    // Brauzer "Geri" (Back) düyməsi ilə qayıdanda yalnız bizim disable etdiyimiz düymələri aktivləşdiririk
    window.addEventListener("pageshow", () => {
        ["btnResetData", "btnDeleteAccount", "btnChangePasswordSubmit"].forEach(id => {
            const btn = document.getElementById(id);
            if (btn) btn.disabled = false;
        });
    });

    // ==========================================
    // 1. AVATAR CANLI ÖNİZLƏMƏ VƏ VALİDASİYA
    // ==========================================
    const avatarInput = document.getElementById("avatarInput");
    const avatarPreview = document.getElementById("avatarPreview");

    if (avatarInput && avatarPreview) {
        const defaultAvatar = avatarPreview.src;

        avatarInput.addEventListener("change", function () {
            const file = this.files?.[0];

            if (!file) {
                this.value = "";
                avatarPreview.src = defaultAvatar;
                return;
            }

            if (!ALLOWED_TYPES.includes(file.type)) {
                Swal.fire({
                    ...swalTheme,
                    icon: 'warning',
                    title: 'Xəta',
                    text: 'Zəhmət olmasa, düzgün şəkil formatı seçin (JPEG, PNG, WEBP, GIF).'
                });
                this.value = "";
                avatarPreview.src = defaultAvatar;
                return;
            }

            if (file.size > MAX_FILE_SIZE) {
                Swal.fire({
                    ...swalTheme,
                    icon: "warning",
                    title: "Fayl çox böyükdür",
                    text: "Şəklin ölçüsü maksimum 5 MB ola bilər."
                });
                this.value = "";
                avatarPreview.src = defaultAvatar;
                return;
            }

            const reader = new FileReader();

            reader.onload = () => {
                const result = reader.result;

                if (typeof result === "string") {
                    avatarPreview.src = result;

                    Swal.fire({
                        ...swalTheme,
                        toast: true,
                        position: "top-end",
                        icon: "success",
                        title: "Şəkil seçildi",
                        showConfirmButton: false,
                        timer: 1500
                    });
                }
            };

            reader.onerror = () => {
                Swal.fire({
                    ...swalTheme,
                    icon: "error",
                    title: "Xəta",
                    text: "Şəkil oxunarkən problem yarandı."
                });
                this.value = "";
                avatarPreview.src = defaultAvatar;
            };

            reader.readAsDataURL(file);
        });
    }

    // ==========================================
    // 2. TƏSDİQ PƏNCƏRƏLƏRİ (DANGER ZONE)
    // ==========================================
    attachConfirmation("btnResetData", {
        title: 'Məlumatları sıfırlamağa əminsiniz?',
        text: "Bütün əməliyyatlarınız silinəcək və bu prosesi geri qaytarmaq mümkün deyil!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#eab308',
        cancelButtonColor: '#64748b',
        confirmButtonText: '<i class="bi bi-check-circle"></i> Bəli, sıfırla',
        cancelButtonText: 'Ləğv et'
    });

    attachConfirmation("btnDeleteAccount", {
        title: 'Hesabınızı silməyə əminsiniz?',
        text: "Hesabınız və bütün məlumatlarınız birdəfəlik silinəcək. Bu əməliyyat geri qaytarıla bilməz!",
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#64748b',
        confirmButtonText: '<i class="bi bi-trash"></i> Bəli, hesabımı sil',
        cancelButtonText: 'Ləğv et'
    });

    // ==========================================
    // 3. ŞİFRƏNİ DƏYİŞ MODALI JS VALİDASİYASI
    // ==========================================
    const changePasswordForm = document.querySelector("#changePasswordModal form");

    if (changePasswordForm) {
        changePasswordForm.addEventListener("submit", function (e) {

            // 2. Defensive Programming: Elementlərin mövcudluğunu yoxlayırıq
            const newPasswordInput = this.querySelector('input[name="NewPassword"]');
            const confirmPasswordInput = this.querySelector('input[name="ConfirmPassword"]');

            if (!newPasswordInput || !confirmPasswordInput) return;

            const newPassword = newPasswordInput.value;
            const confirmPassword = confirmPasswordInput.value;

            // Peşəkar Şifrə Yoxlaması (Minimum 8 simvol, 1 böyük hərf, 1 kiçik hərf, 1 rəqəm)
            const passwordRegex =
                /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).{8,}$/;

            if (!passwordRegex.test(newPassword)) {
                e.preventDefault();

                Swal.fire({
                    ...swalTheme,
                    icon: 'warning',
                    title: 'Zəif Şifrə',
                    text: "Şifrə ən azı 8 simvol olmalı, minimum 1 böyük hərf, 1 kiçik hərf, 1 rəqəm və 1 xüsusi simvol (!@#$%^&*) daxil etməlidir."
                });
                return;
            }

            // Şifrələr uyğun gəlmirsə dayandırırıq
            if (newPassword !== confirmPassword) {
                e.preventDefault();

                Swal.fire({
                    ...swalTheme,
                    icon: 'error',
                    title: 'Xəta',
                    text: 'Yeni şifrə və təkrar şifrə uyğun gəlmir!'
                });
                return;
            }

            // Hər şey qaydasındadırsa düyməni tap və disable et
            const submitBtn = document.getElementById("btnChangePasswordSubmit");
            if (submitBtn) {
                submitBtn.disabled = true;
            }

            showLoading();
        });
    }

});