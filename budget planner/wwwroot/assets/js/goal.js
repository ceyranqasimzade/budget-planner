document.addEventListener("DOMContentLoaded", function () {
    // 50 ədəd Motivasiyaedici Maliyyə Sözləri
    const quotes = [
        { text: '"Xərclədikdən sonra qalanı yığmayın, yığdıqdan sonra qalanı xərcləyin."', author: "- Uorren Baffet" },
        { text: '"Böyük bir sərvət bir gecədə deyil, hər gün edilən kiçik qənaətlərlə yaranır."', author: "- Maliyyə Hikməti" },
        { text: '"Hədəfi olmayan gəmiyə heç bir külək kömək edə bilməz."', author: "- Seneka" },
        { text: '"Bu gün etdiyiniz qənaət, sabahkı azadlığınızın biletidir."', author: "- Anonim" },
        { text: '"Büdcə, pulunuzun hara getdiyini düşünmək əvəzinə, ona hara gedəcəyini söyləməkdir."', author: "- Con Maksvell" },
        { text: '"Özünə edəcəyin ən böyük yaxşılıq, gələcəyini sığortalamaqdır."', author: "- Anonim" },
        { text: '"Zənginlik çox pul qazanmaq deyil, əldə olanı düzgün idarə etməkdir."', author: "- Epiktet" },
        { text: '"Pul yaxşı bir xidmətçi, amma pis bir ağadır."', author: "- Frensis Bekon" },
        { text: '"Əgər ehtiyacınız olmayan şeyləri alsanız, tezliklə ehtiyacınız olanları satmalı olacaqsınız."', author: "- Uorren Baffet" },
        { text: '"Kiçik xərclərə diqqət edin; kiçik bir dəlik böyük bir gəmini batıra bilər."', author: "- Bencamin Franklin" },

        { text: '"Bir qəpik qənaət etmək, bir qəpik qazanmaq deməkdir."', author: "- Bencamin Franklin" },
        { text: '"Pul ağacda bitmir, amma doğru qərarlarla çoxala bilir."', author: "- Maliyyə Hikməti" },
        { text: '"İnvestisiya gələcəyə bu gün göndərdiyiniz bir hədiyyədir."', author: "- Anonim" },
        { text: '"Qazandığınız hər qəpiyə hörmət edin, çünki o, gələcək sərvətinizin toxumudur."', author: "- Anonim" },
        { text: '"Kasıblıq pulun olmaması deyil, xəyalın və hədəfin olmamasıdır."', author: "- Anonim" },
        { text: '"Ən yaxşı investisiya özünüzə etdiyiniz investisiyadır."', author: "- Uorren Baffet" },
        { text: '"Heç vaxt tək bir gəlir mənbəyindən asılı qalmayın. İkincisini yaratmaq üçün investisiya edin."', author: "- Uorren Baffet" },
        { text: '"Pul sizin üçün işləməlidir, siz pul üçün deyil."', author: "- Robert Kiyosaki" },
        { text: '"Daha çox pul qazanmaq problemləri həll etmir, pulu düzgün idarə etmək həll edir."', author: "- Robert Kiyosaki" },
        { text: '"Uğur qazanmaq üçün nə qədər qazandığınız deyil, nə qədər saxladığınız önəmlidir."', author: "- Robert Kiyosaki" },

        { text: '"Maliyyə azadlığı bir rəqəm deyil, düşüncə tərzidir."', author: "- Anonim" },
        { text: '"Gələcəyini təxmin etməyin ən yaxşı yolu onu yaratmaqdır."', author: "- Piter Druker" },
        { text: '"Vaxt puldur, ancaq pul vaxt ala bilməz."', author: "- Anonim" },
        { text: '"Səbr maliyyə uğurunun ən vacib açarıdır."', author: "- Maliyyə Hikməti" },
        { text: '"Borc gələcəkdəki azadlığınızdan götürülmüş bir kreditdir."', author: "- Anonim" },
        { text: '"Pulu xərcləyərkən deyil, qənaət edərkən zəngin olursunuz."', author: "- Anonim" },
        { text: '"Zəngin olmaq istəyirsinizsə, həm qazanmağı, həm də qorumağı öyrənin."', author: "- Corc S. Kleyson" },
        { text: '"Qazancınızın bir hissəsi yalnız sizə məxsusdur, onu özünüz üçün saxlayın."', author: "- Corc S. Kleyson" },
        { text: '"Planlaşdırılmamış xərclər, xəyallarınızın ən böyük düşmənidir."', author: "- Anonim" },
        { text: '"Bu günün ləzzətindən imtina etmək, sabahın rahatlığını təmin edir."', author: "- Anonim" },

        { text: '"Pul vəziyyəti deyil, xarakteri ortaya çıxarır."', author: "- Maliyyə Hikməti" },
        { text: '"Pula sahib olmaq kifayət deyil, onu idarə etməyi bilmək lazımdır."', author: "- Anonim" },
        { text: '"Ehtiyac ilə istək arasındakı fərqi bilmək, zənginliyin ilk qaydasıdır."', author: "- Anonim" },
        { text: '"Hər böyük sərvətin arxasında davamlı bir intizam dayanır."', author: "- Maliyyə Hikməti" },
        { text: '"Bu gün başlasanız, bir il sonra özünüzə təşəkkür edəcəksiniz."', author: "- Anonim" },
        { text: '"Risk etməmək, ən böyük riskdir."', author: "- Mark Zukerberq" },
        { text: '"İstəklərinizi cilovlayın ki, cüzdanınız sərbəst nəfəs alsın."', author: "- Anonim" },
        { text: '"Pul qazanmaq hünər, onu saxlamaq sənət, çoxaltmaq isə elmdir."', author: "- Anonim" },
        { text: '"Büdcə, xəyallarınızın rəqəmlərlə ifadəsidir."', author: "- Anonim" },
        { text: '"Damlaya-damlaya göl olar, az-az yığılan pul sərvətə çevrilər."', author: "- Atalar sözü" },

        { text: '"Qənaət – ən etibarlı gəlir mənbəyidir."', author: "- Mark Tulli Siseron" },
        { text: '"Borcla zənginləşmək, qum üzərində qəsr qurmağa bənzəyir."', author: "- Anonim" },
        { text: '"Öz büdcənizin rəhbəri olun, yoxsa başqasının planının bir hissəsi olarsınız."', author: "- Anonim" },
        { text: '"Sərmayə qoymaq bu günün toxumunu sabahın kölgəsi üçün əkməkdir."', author: "- Uorren Baffet" },
        { text: '"Əsl zənginlik ehtiyacların azlığındadır."', author: "- Epiktet" },
        { text: '"Dünənki israf, sabahın ehtiyacıdır."', author: "- Anonim" },
        { text: '"Pul yalnız bir alətdir; səni istədiyin yerə aparar, amma sənin əvəzinə sükanı idarə etməz."', author: "- Ayn Rand" },
        { text: '"Sabahkı rahatlıq, bugünkü intizamın bəhrəsidir."', author: "- Maliyyə Hikməti" },
        { text: '"Zənginlər vaxta, yoxsullar isə pula sərmayə qoyarlar."', author: "- Uorren Baffet" },
        { text: '"Qazancınız xərclərinizdən çox olduqda, azadlığa doğru ilk addımı atmış olursunuz."', author: "- Anonim" }
    ];

    const quoteTextElement = document.getElementById("quote-text");
    const quoteAuthorElement = document.getElementById("quote-author");

    // Əgər elementlər tapıldısa (Səhifə yüklənibsə)
    if (quoteTextElement && quoteAuthorElement) {
        const randomIndex = Math.floor(Math.random() * quotes.length);
        const selectedQuote = quotes[randomIndex];

        // Animasiya üçün əvvəlcə görünməz edirik
        quoteTextElement.style.opacity = "0";
        quoteAuthorElement.style.opacity = "0";

        // 100 millisaniyə sonra yazını təyin edib yavaşca göstəririk
        setTimeout(() => {
            quoteTextElement.innerText = selectedQuote.text;
            quoteAuthorElement.innerText = selectedQuote.author;

            quoteTextElement.style.transition = "opacity 0.8s ease-in-out";
            quoteAuthorElement.style.transition = "opacity 0.8s ease-in-out";

            quoteTextElement.style.opacity = "1";
            quoteAuthorElement.style.opacity = "1";
        }, 100);
    }
});