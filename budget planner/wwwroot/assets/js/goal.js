document.addEventListener("DOMContentLoaded", function () {

    // ==========================================================================
    // 1. MOTİVASİYA SÖZLƏRİ GENERATORU (Fade-in animasiyası ilə)
    // ==========================================================================
    const quotes = [
        { text: '"Xərclədikdən sonra qalanı yığmayın, yığdıqdan sonra qalanı xərcləyin."', author: "- Uorren Baffet" },
        { text: '"Böyük bir sərvət bir gecədə deyil, hər gün edilən kiçik qənaətlərlə yaranır."', author: "- Maliyyə Hikməti" },
        { text: '"Hədəfi olmayan gəmiyə heç bir külək kömək edə bilməz."', author: "- Seneka" },
        { text: '"Bu gün etdiyiniz qənaət, sabahkı azadlığınızın biletidir."', author: "- Anonim" },
        { text: '"Büdcə, pulunuzun hara getdiyini düşünmək əvəzinə, ona hara gedəcəyini söyləməkdir."', author: "- Con Maksvell" },
        { text: '"Zənginlik çox pul qazanmaq deyil, əldə olanı düzgün idarə etməkdir."', author: "- Epiktet" },
        { text: '"Pul yaxşı bir xidmətçi, amma pis bir ağadır."', author: "- Frensis Bekon" },
        { text: '"Əgər ehtiyacınız olmayan şeyləri alsanız, tezliklə ehtiyacınız olanları satmalı olacaqsınız."', author: "- Uorren Baffet" },
        { text: '"Kiçik xərclərə diqqət edin; kiçik bir dəlik böyük bir gəmini batıra bilər."', author: "- Bencamin Franklin" },
        { text: '"Bir qəpik qənaət etmək, bir qəpik qazanmaq deməkdir."', author: "- Bencamin Franklin" },
        { text: '"Ən yaxşı investisiya özünüzə etdiyiniz investisiyadır."', author: "- Uorren Baffet" },
        { text: '"Pul sizin üçün işləməlidir, siz pul üçün deyil."', author: "- Robert Kiyosaki" },
        { text: '"Daha çox pul qazanmaq problemləri həll etmir, pulu düzgün idarə etmək həll edir."', author: "- Robert Kiyosaki" },
        { text: '"Qazandığınız hər qəpiyə hörmət edin, çünki o, gələcək sərvətinizin toxumudur."', author: "- Anonim" },
        { text: '"Qənaət – ən etibarlı gəlir mənbəyidir."', author: "- Mark Tulli Siseron" },
        { text: '"Pul yalnız bir alətdir; səni istədiyin yerə aparar, amma sənin əvəzinə sükanı idarə etməz."', author: "- Ayn Rand" },
        { text: '"Damlaya-damlaya göl olar, az-az yığılan pul sərvətə çevrilər."', author: "- Atalar sözü" }
    ];

    const quoteText = document.getElementById("quoteText");
    const quoteAuthor = document.getElementById("quoteAuthor");

    if (quoteText && quoteAuthor) {
        const selected = quotes[Math.floor(Math.random() * quotes.length)];
        quoteText.style.opacity = '0';
        quoteAuthor.style.opacity = '0';
        quoteText.style.transition = 'opacity 0.4s ease';
        quoteAuthor.style.transition = 'opacity 0.4s ease';

        setTimeout(() => {
            quoteText.innerText = selected.text;
            quoteAuthor.innerText = selected.author;
            quoteText.style.opacity = '1';
            quoteAuthor.style.opacity = '1';
        }, 200);
    }

    // ==========================================================================
    // 2. PROGRESS BAR ANIMASIYASI VƏ CONFETTI TƏBRİKİ
    // ==========================================================================
    const progressBars = document.querySelectorAll('.animated-bar');
    setTimeout(() => {
        progressBars.forEach(bar => {
            const width = bar.getAttribute('data-width') || '0%';
            bar.style.width = width;
        });
    }, 300);

    const completedCards = document.querySelectorAll('.completed-goal');
    if (completedCards.length > 0 && typeof confetti === 'function') {
        setTimeout(() => {
            confetti({
                particleCount: 80,
                spread: 70,
                origin: { y: 0.6 }
            });
        }, 800);
    }

    // ==========================================================================
    // 3. STATİSTİKA SAYĞAC ANIMASİYASI (CountUp)
    // ==========================================================================
    const counters = document.querySelectorAll('.counter');
    counters.forEach(counter => {
        const target = parseFloat(counter.getAttribute('data-target')) || 0;
        let count = 0;
        const speed = target / 30;

        if (target > 0) {
            const updateCount = () => {
                count += speed;
                if (count < target) {
                    counter.innerText = Math.ceil(count).toLocaleString('az-AZ');
                    requestAnimationFrame(updateCount);
                } else {
                    counter.innerText = target.toLocaleString('az-AZ');
                }
            };
            updateCount();
        } else {
            counter.innerText = '0';
        }
    });

    // ==========================================================================
    // 4. CANLI ÖNİZLƏMƏ (Real-Time UI Update) & VALYUTA MƏZƏNNƏLƏRİ
    // ==========================================================================
    const inputName = document.getElementById('inputName');
    const inputTarget = document.getElementById('inputTarget');
    const inputCurrency = document.getElementById('inputCurrency');
    const inputColorClass = document.getElementById('inputColorClass');

    const prevCard = document.getElementById('prevCard');
    const prevName = document.getElementById('prevName');
    const prevTarget = document.getElementById('prevTarget');
    const prevRemaining = document.getElementById('prevRemaining');
    const prevIcon = document.getElementById('prevIcon');
    const prevIconBox = document.getElementById('prevIconBox');
    const prevPercentage = document.getElementById('prevPercentage');
    const prevProgressBar = document.getElementById('prevProgressBar');

    // Bütün valyutaların simvolları
    const getCurrencySymbol = (code) => {
        switch (code) {
            case 'USD': return '$';
            case 'EUR': return '€';
            case 'TRY': return '₺';
            case 'RUB': return '₽';
            case 'GBP': return '£';
            case 'GEL': return '₾';
            case 'AED': return 'د.إ';
            case 'CNY': return '¥';
            case 'CHF': return 'Fr';
            case 'CAD': return '$';
            case 'AZN':
            default: return '₼';
        }
    };

    // Məbləğ Formatlaması (0.00 şəklində)
    const formatMoney = (val, currencyCode) => {
        const symbol = getCurrencySymbol(currencyCode);
        const formattedVal = val.toLocaleString('az-AZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        return `${symbol}${formattedVal}`;
    };

    const updatePreviewValues = () => {
        const nameVal = inputName ? inputName.value.trim() : '';
        const targetVal = parseFloat(inputTarget ? inputTarget.value : 0) || 0;
        const currCode = inputCurrency ? inputCurrency.value : 'AZN';

        if (prevName) prevName.innerText = nameVal.length > 0 ? nameVal : "Hədəf Adı";
        if (prevTarget) prevTarget.innerText = formatMoney(targetVal, currCode);
        if (prevRemaining) prevRemaining.innerText = formatMoney(targetVal, currCode);
    };

    if (inputName) inputName.addEventListener('input', updatePreviewValues);
    if (inputTarget) inputTarget.addEventListener('input', updatePreviewValues);
    if (inputCurrency) inputCurrency.addEventListener('change', updatePreviewValues);

    if (inputColorClass && prevCard) {
        inputColorClass.addEventListener('change', (e) => {
            const selectedClass = e.target.value;
            prevCard.className = `card border-0 rounded-4 shadow-sm p-4 border-top border-4 border-${selectedClass}`;
            if (prevIconBox) prevIconBox.className = `stat-icon bg-${selectedClass}-subtle text-${selectedClass} rounded-3 p-3 me-3`;
            if (prevPercentage) prevPercentage.className = `text-${selectedClass} fw-bold`;
            if (prevProgressBar) prevProgressBar.className = `progress-bar bg-${selectedClass} rounded-pill`;
        });
    }

    // İlk yüklənmədə canlı önizləməni yenilə
    updatePreviewValues();

    // ==========================================================================
    // 5. İKON SEÇİMİ VƏ PREVIEW SİNXRONLAŞDIRILMASI
    // ==========================================================================
    const hiddenInput = document.getElementById("IconClassInput") || document.getElementById("IconClass") || document.getElementById("inputIcon");
    const buttons = document.querySelectorAll(".icon-select-btn");

    if (hiddenInput && buttons.length > 0) {
        const activeIcon = hiddenInput.value || "bi-bullseye";

        buttons.forEach(btn => {
            const btnIcon = btn.getAttribute("data-icon");

            // Səhifə yüklənəndə aktiv olan ikona 'active-icon' ver
            if (btnIcon === activeIcon) {
                btn.classList.add("active-icon");
                if (prevIcon) prevIcon.className = `bi ${activeIcon} fs-3`;
            }

            btn.addEventListener("click", function (e) {
                e.preventDefault(); // Formun submit olmasının qarşısını alır

                // 1. Aktiv stil tənzimlənməsi
                buttons.forEach(b => b.classList.remove("active-icon"));
                this.classList.add("active-icon");

                // 2. Hidden input dəyərini yenilə
                const selectedIcon = this.getAttribute("data-icon");
                hiddenInput.value = selectedIcon;

                // 3. Önizləmə daxilindəki ikonu anında dəyiş
                if (prevIcon) {
                    prevIcon.className = `bi ${selectedIcon} fs-3`;
                }
            });
        });
    }
});