document.addEventListener("DOMContentLoaded", function () {

    // ==========================================
    // 1. MALİYYƏ HİKMƏTİ (SİTATLAR) LOGİKASI
    // ==========================================
    const quoteTextEl = document.getElementById("quoteText");
    const quoteAuthorEl = document.getElementById("quoteAuthor");

    if (quoteTextEl) {
        const financialQuotes = [
            { text: "Xərcləmələrinizdən sonra qalanı yığmayın, yığımınızdan sonra qalanı xərcləyin.", author: "Warren Buffett" },
            { text: "Kiçik xərclərdən ehtiyatlı olun; kiçik bir sızma böyük bir gəmini batıra bilər.", author: "Benjamin Franklin" },
            { text: "Büdcə tutmaq azadlığı məhdudlaşdırmır, əksinə pulunuza haraya gedəcəyini öyrədir.", author: "John C. Maxwell" },
            { text: "Pul yaxşı xidmətçidir, amma pis ağadır.", author: "Francis Bacon" },
            { text: "Qənaət olunmuş hər bir qəpik gələcək müstəqilliyinizə yatırılan investisiyadır.", author: "Benjamin Franklin" },
            { text: "Zənginlik çox pulun olması deyil, az ehtiyacın olmasıdır.", author: "Epiktet" },
            { text: "Heç vaxt bütün yumurtaları bir səbətə qoymayın.", author: "İnvestisiya Qaydası" },
            { text: "Ehtiyacınız olmayan şeyi alırsınızsa, tezliklə ehtiyacınız olanı satmalı olacaqsınız.", author: "Warren Buffett" },
            { text: "Maliyyə məqsədi olmayan büdcə, xəritəsiz səyahət kimidir.", author: "Maliyyə Məsləhəti" },
            { text: "İnvestisiya səbir işidir; toxumu bu gün əkib sabah kölgəsində otura bilməzsiniz.", author: "Warren Buffett" },
            { text: "Pulunuzu idarə etməsinizsə, pulunuz sizi idarə edər.", author: "Maliyyə Qaydası" },
            { text: "Hər bir böyük var-dövlət kiçik yığımların nəticəsidir.", author: "Maliyyə Hikməti" },
            { text: "Gəliriniz nə qədər olursa olsun, xərcləriniz gəlirinizi üstələməməlidir.", author: "Büdcə Qaydası" },
            { text: "Maliyyə savadlılığı ən gəlirli investisiyadır.", author: "Maliyyə Məsləhəti" },
            { text: "Borc gələcəkdəki azadlığınızdan götürülmüş girovdur.", author: "Maliyyə Qaydası" },
            { text: "Məqsədsiz yığılan pul tez xərclənər; hədəflərinizi dəqiq müəyyən edin.", author: "Maliyyə Hikməti" },
            { text: "Qiymət ödədiyiniz şeydir, dəyər isə əldə etdiyiniz şeydir.", author: "Warren Buffett" },
            { text: "Pulunuzun haraya getdiyini təəccüblə soruşmaqdansa, ona haraya gedəcəyini deyin.", author: "Dave Ramsey" },
            { text: "Maliyyə müstəqilliyi yalnız böyük gəlirlə deyil, doğru vərdişlərlə qazanılır.", author: "Maliyyə Qaydası" },
            { text: "Hər ay gəlirinizin minimum 10%-ni gələcək özünüz üçün saxlayın.", author: "Yığım Qaydası" },
            { text: "Emosional alış-verişlərdən qaçın; almazdan əvvəl 24 saat gözləyin.", author: "Smart Shopping" },
            { text: "Böyük var-dövlətlər bir gecədə deyil, hər gün atılan kiçik addımlarla qurulur.", author: "Maliyyə Hikməti" },
            { text: "İşlədiyiniz pul sizə işləməyə başlayanda maliyyə azadlığı gəlir.", author: "Robert Kiyosaki" },
            { text: "Maliyyə intizamı bu gün istədiyinizlə ən çox istədiyiniz arasında seçim etməkdir.", author: "Maliyyə Qaydası" },
            { text: "Zəngin görünməyə çalışmaq zəngin olmağın ən böyük düşmənidir.", author: "Morgan Housel" },
            { text: "Uğurlu maliyyə planlaması bəxtdən deyil, sistemdən asılıdır.", author: "Maliyyə Məsləhəti" },
            { text: "Qazanmaq bir dəfəlik addımdır, yığmaq isə davamlı vərdişdir.", author: "Maliyyə Hikməti" },
            { text: "Xərclərinizi izləmək maliyyə sağlamlığının ilk addımıdır.", author: "Büdcə Qaydası" },
            { text: "Risk nə etdiyinizi bilmədikdə yaranır.", author: "Warren Buffett" },
            { text: "Gəliriniz artdıqca həyat standartınızı deyil, yığım nisbətinizi artırın.", author: "Maliyyə Qaydası" },
            { text: "Ən yaxşı investisiya öz bilik və bacarıqlarınıza etdiyiniz investisiyadır.", author: "Benjamin Franklin" },
            { text: "Maliyyə yastığı (təhlükəsizlik fondu) stressiz həyatın təməlidir.", author: "Maliyyə Məsləhəti" },
            { text: "Pulla satın alına biləcək ən qiymətli şey maliyyə sərbəstliyidir.", author: "Maliyyə Hikməti" },
            { text: "Hər ayın sonunda deyil, başında yığım edin.", author: "Yığım Qaydası" },
            { text: "Xırda qənaətlər vaxt keçdikdə böyük fürsətlərə çevrilir.", author: "Maliyyə Məsləhəti" },
            { text: "Maliyyə azadlığı çox pul xərcləmək deyil, pul haqqında narahat olmamaqdır.", author: "Maliyyə Qaydası" },
            { text: "Zaman maliyyə investisiyasının ən yaxşı dostudur.", author: "Maliyyə Hikməti" },
            { text: "Pis gün üçün ayrılan pul, yaxşı günlərin zəmanətidir.", author: "Maliyyə Məsləhəti" },
            { text: "İmkanlarınız daxilində deyil, ehtiyaclarınız daxilində yaşamağı öyrənin.", author: "Atalar Sözü" },
            { text: "Pulun dəyərini bilmək istəyirsinizsə, borc almağa çalışın.", author: "Benjamin Franklin" },
            { text: "Ağıllı alıcı qiymətə deyil, keyfiyyət və ehtiyaca baxar.", author: "Maliyyə Qaydası" },
            { text: "Uğurlu yığımın sirri məbləğdə deyil, müntəzəmlikdədir.", author: "Maliyyə Hikməti" },
            { text: "Hədəfinizə gedən yolda kiçik addımlar böyük nəticələr doğurur.", author: "Maliyyə Məsləhəti" },
            { text: "Maliyyə planı hazırlamaq gələcəyinizi nəzarətə almaqdır.", author: "Büdcə Məsləhəti" },
            { text: "Büdcənizi hər gün izləmək maliyyə azadlığının açarıdır.", author: "Maliyyə Qaydası" },
            { text: "Hədəflərinizi hər gün göz önündə saxlayın və onlara doğru addımlayın.", author: "Motivasiya" },
            { text: "Yığım etmək gələcək özünüzə verə biləcəyiniz ən yaxşı hədiyyədir.", author: "Maliyyə Hikməti" },
            { text: "Qazanmaq çətindir, amma idarə edə bilməmək daha böyük itkidir.", author: "Maliyyə Məsləhəti" },
            { text: "Gələcəyinizi bu gün idarə etməyə başlayın – hər bir qəpik hesablanır!", author: "Büdcəm AI" }
        ];

        const randomIndex = Math.floor(Math.random() * financialQuotes.length);
        const randomQuote = financialQuotes[randomIndex];

        quoteTextEl.textContent = randomQuote.text;
        if (quoteAuthorEl) {
            quoteAuthorEl.textContent = `- ${randomQuote.author}`;
        }
    }

    // ==========================================
    // 2. SAYĞAC VƏ PROQRES BAR ANİMASİYALARI
    // ==========================================
    const counters = document.querySelectorAll(".counter");
    counters.forEach(counter => {
        const target = parseInt(counter.getAttribute("data-target")) || 0;
        if (target === 0) {
            counter.textContent = "0";
            return;
        }

        let count = 0;
        const speed = 200 / target;
        const updateCount = () => {
            count++;
            counter.textContent = count;
            if (count < target) {
                setTimeout(updateCount, speed);
            } else {
                counter.textContent = target;
            }
        };
        updateCount();
    });

    const progressBars = document.querySelectorAll(".animated-bar");
    setTimeout(() => {
        progressBars.forEach(bar => {
            const width = bar.getAttribute("data-width");
            if (width) {
                bar.style.width = width;
            }
        });
    }, 150);

    // ==========================================
    // 3. CANLI ÖNİZLƏMƏ VƏ FORM LOGİKASI
    // ==========================================
    const nameInput = document.getElementById("inputName");
    const targetInput = document.getElementById("inputTarget");
    const currentInput = document.getElementById("inputCurrent");
    const currencySelect = document.getElementById("inputCurrency");
    const deadlineInput = document.getElementById("inputDeadline");
    const iconHiddenInput = document.getElementById("IconClassInput");

    const prevName = document.getElementById("prevName");
    const prevCurrent = document.getElementById("prevCurrent");
    const prevTarget = document.getElementById("prevTarget");
    const prevRemaining = document.getElementById("prevRemaining");
    const prevPercentage = document.getElementById("prevPercentage");
    const prevProgressBar = document.getElementById("prevProgressBar");
    const prevIcon = document.getElementById("prevIcon");
    const prevDeadline = document.getElementById("prevDeadline");

    if (deadlineInput && typeof flatpickr !== "undefined") {
        flatpickr(deadlineInput, {
            locale: flatpickr.l10ns.az,
            dateFormat: "Y-m-d",
            minDate: "today",
            onChange: function (selectedDates, dateStr) {
                updateDeadlinePreview(dateStr);
            }
        });
    }

    const currencyMap = {
        "AZN": "₼",
        "USD": "$",
        "EUR": "€",
        "TRY": "₺",
        "RUB": "₽",
        "GBP": "£",
        "CNY": "¥",
        "GEL": "₾",
        "AED": "د.إ",
        "CHF": "Fr",
        "CAD": "$"
    };

    function getCurrencySymbol() {
        if (!currencySelect) return "₼";
        const val = currencySelect.value || "AZN";
        return currencyMap[val] || val + " ";
    }

    function updateDeadlinePreview(dateVal) {
        if (!prevDeadline) return;

        const val = dateVal || (deadlineInput ? deadlineInput.value : "");
        if (val) {
            const targetDate = new Date(val);
            const today = new Date();

            today.setHours(0, 0, 0, 0);
            targetDate.setHours(0, 0, 0, 0);

            const diffTime = targetDate - today;
            const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

            if (diffDays > 0) {
                prevDeadline.innerText = `${diffDays} gün qaldı`;
            } else if (diffDays === 0) {
                prevDeadline.innerText = "Bu gün son gündür!";
            } else {
                prevDeadline.innerText = "Müddət bitib";
            }
        } else {
            prevDeadline.innerText = "Müddət seçilməyib";
        }
    }

    function updatePreview() {
        const symbol = getCurrencySymbol();
        const targetAmount = parseFloat(targetInput ? targetInput.value : 0) || 0;
        const currentAmount = parseFloat(currentInput ? currentInput.value : 0) || 0;
        const remainingAmount = Math.max(0, targetAmount - currentAmount);

        // Hədəf Adı
        if (prevName && nameInput) {
            prevName.innerText = nameInput.value.trim() !== "" ? nameInput.value : "Hədəf Adı";
        }

        // Məbləğlər
        if (prevCurrent) prevCurrent.innerText = `${symbol}${currentAmount.toFixed(2)}`;
        if (prevTarget) prevTarget.innerText = `${symbol}${targetAmount.toFixed(2)}`;
        if (prevRemaining) prevRemaining.innerText = `${symbol}${remainingAmount.toFixed(2)}`;

        // Faiz və Proqres bar
        let percent = 0;
        if (targetAmount > 0) {
            percent = Math.min(100, Math.round((currentAmount / targetAmount) * 100));
        }
        if (prevPercentage) prevPercentage.innerText = `${percent}%`;
        if (prevProgressBar) prevProgressBar.style.width = `${percent}%`;

        // Tarix önizləməsi
        updateDeadlinePreview();
    }

    // İkon Seçimi
    const iconButtons = document.querySelectorAll(".icon-select-btn");
    iconButtons.forEach(btn => {
        btn.addEventListener("click", function () {
            iconButtons.forEach(b => b.classList.remove("active-icon"));
            this.classList.add("active-icon");

            const iconClass = this.getAttribute("data-icon");
            if (iconHiddenInput) iconHiddenInput.value = iconClass;
            if (prevIcon) prevIcon.className = `bi ${iconClass}`;
        });
    });

    // Event Listeners
    if (nameInput) nameInput.addEventListener("input", updatePreview);
    if (targetInput) targetInput.addEventListener("input", updatePreview);
    if (currentInput) currentInput.addEventListener("input", updatePreview);
    if (currencySelect) currencySelect.addEventListener("change", updatePreview);
    if (deadlineInput) {
        deadlineInput.addEventListener("input", updatePreview);
        deadlineInput.addEventListener("change", updatePreview);
    }

    // İlk yüklənmədə hesabla
    if (nameInput || targetInput) {
        updatePreview();
    }
});

// ==========================================
// 4. SİLMƏK ÜÇÜN XƏBƏRDARLIQ (Global olmalıdır!)
// ==========================================
function confirmDelete(event, form) {
    event.preventDefault();

    Swal.fire({
        title: 'Təsdiqləyin',
        text: "Bu hədəfi silmək istədiyinizə əminsiniz?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Bəli, sil',
        cancelButtonText: 'Xeyr'
    }).then((result) => {
        if (result.isConfirmed) {
            form.submit();
        }
    });
}