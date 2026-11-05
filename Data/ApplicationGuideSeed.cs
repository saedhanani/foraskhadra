using Foras_Khadra.Models;

namespace Foras_Khadra.Data
{
    /// <summary>
    /// الدفعة الأولى من أدلة التقديم (المرحلة 4.1): ثمانية برامج متكررة. النصوص عامة وقابلة للتحرير من /Admin/Guides؛
    /// المواعيد «المعتادة» تقريبية ويذكّر كل دليل بالتحقق من الموقع الرسمي. المفاتيح تطابق Opportunity.ProgramKey في برومبت منصة الكتابة.
    /// </summary>
    public static class ApplicationGuideSeed
    {
        public static List<ApplicationGuide> All()
        {
            var list = new List<ApplicationGuide>();
            var order = 0;

            list.Add(Make("chevening", "chevening", order++, 8, 11, "https://www.chevening.org/",
                "دليل التقديم على منحة تشيفنينغ (بريطانيا)", "How to apply for the Chevening Scholarship (UK)", "Candidater à la bourse Chevening (Royaume-Uni)",
                "منحة الحكومة البريطانية لماستر سنة واحدة ممولة بالكامل للقادة الشباب؛ تطلب سنتَي خبرة عمل وخططًا واضحة للعودة.",
                "The UK government's fully funded one-year master's for young leaders; needs two years of work experience and a clear plan to return home.",
                "La bourse du gouvernement britannique pour un master d'un an entièrement financé ; deux ans d'expérience et un projet de retour clair sont exigés.",
                new GuideSections
                {
                    Conditions = "جنسية دولة مؤهَّلة (تشمل أغلب دول الوطن العربي) والعودة إليها سنتين بعد المنحة.\nشهادة بكالوريوس تؤهّل لماستر في بريطانيا.\nخبرة عمل لا تقل عن سنتين (2,800 ساعة) بما فيها التطوع والتدريب المدفوع.\nالتقديم على ثلاثة برامج ماستر مختلفة في جامعات بريطانية، والقبول غير المشروط يُطلب لاحقًا لا عند التقديم.\nلا يُقبل من يحمل جنسية بريطانية أو درس بتمويل حكومي بريطاني سابق.",
                    Timeline = "أغسطس–نوفمبر: التقديم الإلكتروني (أربعة مقالات 500 كلمة).\nفبراير–أبريل: المقابلات في السفارة البريطانية.\nيونيو: النتائج، ثم تأمين القبول الجامعي غير المشروط وشهادة اللغة قبل يوليو.\nسبتمبر–أكتوبر: السفر وبدء الدراسة.",
                    Letter = "المقالات الأربعة هي المنحة كلها: القيادة والتأثير، التواصل والشبكات، خطة الدراسة في بريطانيا، والخطة المهنية.\nافتح كل مقال بموقف حقيقي من حياتك (ماذا فعلت، ما النتيجة بالأرقام)، ثم اربطه بما ستفعله بعد العودة.\nاجعل خطة العودة ملموسة: مؤسسة أو مشروع محدد في بلدك وليس «خدمة المجتمع» عمومًا.",
                    Mistakes = "اختيار جامعات لأجل الاسم لا لأجل البرنامج المناسب لخطتك.\nمقالات عامة بلا أرقام ولا نتائج.\nالحديث عن الهجرة أو البقاء في بريطانيا.\nنسخ القصة نفسها في المقالات الأربعة.\nإهمال تأمين شهادة اللغة قبل الموعد (إن طُلبت).",
                    Tips = "ابحث عن خريجي تشيفنينغ من بلدك على لينكدإن واطلب قراءة مقالاتك.\nجهّز الإجابة عن «لماذا بريطانيا؟» بأمثلة محددة عن مؤسسات وخبراء.\nاحتفظ بنسخة منظمة من ساعات العمل وشهادات الخبرة.",
                    Checklist = new() { "جواز سفر ساري", "شهادة البكالوريوس وكشف الدرجات", "إثبات سنتَي خبرة", "اختيار ثلاثة برامج ماستر", "مسوّدة المقالات الأربعة", "مرجعان مهنيان", "شهادة لغة إنجليزية (إن طُلبت)" }
                },
                new GuideSections
                {
                    Conditions = "Citizen of an eligible country (most of the Arab world) and committed to returning for two years.\nA bachelor's degree that qualifies for a UK master's.\nAt least two years (2,800 hours) of work experience, including volunteering and paid internships.\nApply to three different UK master's courses; an unconditional offer is needed later, not at application.\nNot eligible if you hold British citizenship or had UK government funding before.",
                    Timeline = "August–November: online application (four 500-word essays).\nFebruary–April: interviews at the British embassy.\nJune: results, then secure an unconditional offer and English test before July.\nSeptember–October: travel and start.",
                    Letter = "The four essays are the whole scholarship: leadership and influence, networking, study plan in the UK, career plan.\nOpen each with a real situation (what you did, the result in numbers), then link it to what you will do after returning.\nMake the return plan concrete: a named institution or project at home, not “serving the community” in general.",
                    Mistakes = "Choosing universities for the name rather than the course that fits your plan.\nGeneric essays with no numbers or outcomes.\nTalking about migration or staying in the UK.\nReusing the same story in all four essays.\nLeaving the English test until it is too late.",
                    Tips = "Find Chevening alumni from your country on LinkedIn and ask them to read your essays.\nPrepare “Why the UK?” with specific institutions and experts.\nKeep an organised record of work hours and experience letters.",
                    Checklist = new() { "Valid passport", "Bachelor's certificate and transcript", "Proof of two years' experience", "Three chosen master's courses", "Drafts of the four essays", "Two professional references", "English test (if required)" }
                },
                new GuideSections
                {
                    Conditions = "Citoyen d'un pays éligible (la plupart du monde arabe) et engagement à rentrer deux ans.\nUne licence donnant accès à un master au Royaume-Uni.\nAu moins deux ans d'expérience (2 800 heures), bénévolat et stages rémunérés compris.\nCandidature à trois masters britanniques différents ; l'admission inconditionnelle est demandée plus tard.\nNon éligible avec la nationalité britannique ou un financement public britannique antérieur.",
                    Timeline = "Août–novembre : candidature en ligne (quatre essais de 500 mots).\nFévrier–avril : entretiens à l'ambassade.\nJuin : résultats, puis admission inconditionnelle et test d'anglais avant juillet.\nSeptembre–octobre : départ et rentrée.",
                    Letter = "Les quatre essais font toute la bourse : leadership, réseau, projet d'études, projet professionnel.\nOuvrez chacun par une situation réelle (action, résultat chiffré), puis reliez-la à votre action au retour.\nRendez le projet de retour concret : une institution ou un projet nommé, pas « servir la communauté » en général.",
                    Mistakes = "Choisir les universités pour le nom et non pour le programme.\nEssais génériques sans chiffres ni résultats.\nParler d'émigration ou de rester au Royaume-Uni.\nRéutiliser la même histoire dans les quatre essais.\nRepousser le test d'anglais.",
                    Tips = "Trouvez des anciens Chevening de votre pays sur LinkedIn et faites relire vos essais.\nPréparez « Pourquoi le Royaume-Uni ? » avec des institutions et experts précis.\nGardez un relevé organisé de vos heures de travail et attestations.",
                    Checklist = new() { "Passeport valide", "Diplôme de licence et relevés", "Preuve de deux ans d'expérience", "Trois masters choisis", "Brouillons des quatre essais", "Deux références professionnelles", "Test d'anglais (si exigé)" }
                }));

            list.Add(Make("daad-epos", "daad-epos", order++, 8, 10, "https://www.daad.de/en/study-and-research-in-germany/scholarships/",
                "دليل التقديم على منح DAAD للتنمية (EPOS، ألمانيا)", "How to apply for DAAD EPOS development scholarships (Germany)", "Candidater aux bourses DAAD EPOS (Allemagne)",
                "منح الماستر والدكتوراه الألمانية لخبراء من الدول النامية في مجالات التنمية والبيئة والطاقة؛ التقديم يكون للجامعة لا لـDAAD مباشرة.",
                "German master's and PhD scholarships for professionals from developing countries in development, environment and energy; you apply to the university, not to DAAD directly.",
                "Bourses allemandes de master et doctorat pour professionnels de pays en développement (développement, environnement, énergie) ; on candidate auprès de l'université, pas du DAAD.",
                new GuideSections
                {
                    Conditions = "بكالوريوس (أربع سنوات عادةً) بتقدير جيد جدًا، وتخرّج قبل أقل من ست سنوات.\nسنتا خبرة مهنية على الأقل بعد التخرج في مجال ذي صلة.\nجنسية دولة نامية (معظم الوطن العربي مؤهَّل).\nشهادة لغة: IELTS 6.0 أو TOEFL 80 تقريبًا للبرامج الإنجليزية، أو ألمانية للبرامج الألمانية.\nالتقديم لبرنامج من قائمة EPOS المحددة فقط.",
                    Timeline = "أغسطس–أكتوبر (وبعض البرامج حتى ديسمبر): التقديم مباشرة إلى الجامعة بملف DAAD.\nيناير–مارس: ترشيح الجامعات.\nأبريل–يونيو: قرارات DAAD.\nأغسطس–أكتوبر: دورة لغة ثم بدء الدراسة.",
                    Letter = "خطاب الدوافع في EPOS تنموي: ما المشكلة في بلدك، كيف يحلّها هذا البرنامج تحديدًا، وماذا ستفعل بعد العودة.\nاربط خبرتك المهنية بمحتوى البرنامج مقررًا بمقرر.\nصفحة إلى صفحتان بلغة بسيطة ومحددة.",
                    Mistakes = "التقديم لبرنامج خارج قائمة EPOS.\nخبرة أقل من سنتين أو غير موثقة بشهادات عمل.\nإرسال الملف إلى DAAD بدل الجامعة.\nخطاب دوافع عام لا يذكر البرنامج ومقرراته.\nتجاوز مدة الست سنوات منذ التخرج.",
                    Tips = "اختر برنامجًا واحدًا أو اثنين ودرس مقرراتهما جيدًا؛ الملف المركّز يفوز.\nاطلب من مديرك خطاب توصية يذكر مهامك ونتائجك.\nجهّز استمارة DAAD وشهادات العمل مبكرًا؛ بعض الجامعات تطلب الترجمة الموثقة.",
                    Checklist = new() { "استمارة DAAD موقَّعة", "سيرة ذاتية بصيغة Europass", "خطاب دوافع", "شهادة البكالوريوس وكشف الدرجات", "شهادات خبرة عمل (سنتان)", "خطاب توصية أكاديمي أو مهني", "شهادة لغة", "جواز سفر" }
                },
                new GuideSections
                {
                    Conditions = "Bachelor's (usually four years) with very good grades, completed less than six years ago.\nAt least two years of relevant professional experience after graduation.\nCitizen of a developing country (most of the Arab world qualifies).\nLanguage: roughly IELTS 6.0 / TOEFL 80 for English programmes, or German for German ones.\nApply only to a course on the EPOS list.",
                    Timeline = "August–October (some courses until December): apply directly to the university with the DAAD form.\nJanuary–March: university nominations.\nApril–June: DAAD decisions.\nAugust–October: language course, then studies.",
                    Letter = "The EPOS motivation letter is developmental: the problem at home, how this exact course solves it, what you do after returning.\nMap your experience to the course modules one by one.\nOne to two pages, simple and specific.",
                    Mistakes = "Applying to a course outside the EPOS list.\nLess than two years of experience, or experience without employer letters.\nSending the file to DAAD instead of the university.\nA generic letter that never names the course.\nMore than six years since graduation.",
                    Tips = "Pick one or two courses and study their modules; focused files win.\nAsk your manager for a reference that names your tasks and results.\nPrepare the DAAD form and work certificates early; some universities require certified translations.",
                    Checklist = new() { "Signed DAAD form", "Europass CV", "Motivation letter", "Bachelor's certificate and transcript", "Work certificates (two years)", "Academic or professional reference", "Language certificate", "Passport" }
                },
                new GuideSections
                {
                    Conditions = "Licence (quatre ans en général) avec très bonnes notes, obtenue depuis moins de six ans.\nAu moins deux ans d'expérience professionnelle pertinente après le diplôme.\nNationalité d'un pays en développement (la plupart du monde arabe).\nLangue : environ IELTS 6.0 / TOEFL 80 pour les programmes en anglais, ou allemand.\nCandidater uniquement à un programme de la liste EPOS.",
                    Timeline = "Août–octobre (certains jusqu'en décembre) : candidature directe à l'université avec le formulaire DAAD.\nJanvier–mars : nominations des universités.\nAvril–juin : décisions du DAAD.\nAoût–octobre : cours de langue puis études.",
                    Letter = "La lettre EPOS est orientée développement : le problème chez vous, comment ce programme précis le résout, ce que vous ferez au retour.\nReliez votre expérience aux modules du programme un par un.\nUne à deux pages, simples et précises.",
                    Mistakes = "Candidater hors liste EPOS.\nMoins de deux ans d'expérience ou sans attestations.\nEnvoyer le dossier au DAAD au lieu de l'université.\nLettre générique qui ne nomme pas le programme.\nPlus de six ans depuis le diplôme.",
                    Tips = "Choisissez un ou deux programmes et étudiez leurs modules ; les dossiers ciblés gagnent.\nDemandez à votre responsable une recommandation citant vos missions et résultats.\nPréparez tôt le formulaire DAAD et les attestations ; certaines universités exigent des traductions certifiées.",
                    Checklist = new() { "Formulaire DAAD signé", "CV Europass", "Lettre de motivation", "Diplôme de licence et relevés", "Attestations de travail (deux ans)", "Recommandation académique ou professionnelle", "Certificat de langue", "Passeport" }
                }));

            list.Add(Make("erasmus-mundus", "erasmus-mundus", order++, 10, 1, "https://www.eacea.ec.europa.eu/scholarships/erasmus-mundus-catalogue_en",
                "دليل التقديم على ماستر إيراسموس موندوس المشترك", "How to apply for an Erasmus Mundus Joint Master", "Candidater à un master conjoint Erasmus Mundus",
                "ماستر أوروبي مشترك بين جامعتين أو أكثر، ممول بالكامل من الاتحاد الأوروبي، وبلا شرط خبرة في أغلب البرامج.",
                "A European master's taught by two or more universities, fully funded by the EU, with no work-experience requirement in most programmes.",
                "Un master européen entre deux universités ou plus, entièrement financé par l'UE, sans exigence d'expérience dans la plupart des programmes.",
                new GuideSections
                {
                    Conditions = "بكالوريوس (أو في السنة الأخيرة بشرط التخرج قبل بدء البرنامج).\nكل برنامج يضع شروطه الأكاديمية واللغوية بنفسه؛ IELTS 6.5 شائع.\nلا يُقبل من حصل على منحة إيراسموس موندوس سابقًا، ومن أقام في أوروبا أكثر من 12 شهرًا في آخر 5 سنوات يُحسب «شريكًا» بتمويل أقل.\nيمكن التقديم على ثلاثة برامج في العام كحد أقصى.",
                    Timeline = "أكتوبر–يناير (وبعضها حتى مارس): التقديم على موقع كل برنامج مباشرة.\nمارس–أبريل: نتائج المنح والقوائم الاحتياطية.\nيونيو–أغسطس: التأشيرات والسكن.\nسبتمبر: البدء في الجامعة الأولى، ثم الانتقال بين الجامعات حسب المسار.",
                    Letter = "خطاب الدوافع يجيب عن: لماذا هذا الاتحاد الجامعي تحديدًا، أي مسار من مساراته، وكيف تخدم خلفيتك المشروع المشترك بين الجامعات.\nاذكر أساتذة أو مخرجات بحثية في البرنامج.\nصفحة واحدة في الغالب؛ احترم الحد الذي يضعه البرنامج.",
                    Mistakes = "التقديم على أكثر من ثلاثة برامج (يُستبعد الملف).\nنسخ خطاب واحد لبرامج مختلفة.\nإهمال شروط التخصص الدقيقة (مواد مسبقة معيّنة).\nترك خطابات التوصية لآخر يوم؛ بعض البرامج تطلب تحميلها من المُوصي.",
                    Tips = "ابدأ من كتالوج EMJM الرسمي وفلتر بالمجال، ثم اقرأ صفحة «Admission» لكل برنامج.\nراسل منسق البرنامج بسؤال محدد؛ الرد يفيدك في الخطاب.\nرتّب أوراقك في ملف واحد بالأسماء المطلوبة؛ منصات البرامج صارمة في الصيغ.",
                    Checklist = new() { "اختيار حتى ثلاثة برامج من الكتالوج", "شهادة البكالوريوس وكشف الدرجات", "شهادة لغة", "خطاب دوافع لكل برنامج", "خطابا توصية", "سيرة ذاتية", "إثبات مكان الإقامة في آخر 5 سنوات", "جواز سفر" }
                },
                new GuideSections
                {
                    Conditions = "A bachelor's degree (or final year, graduating before the programme starts).\nEach programme sets its own academic and language conditions; IELTS 6.5 is common.\nNot eligible if you already held an Erasmus Mundus scholarship; more than 12 months in Europe in the last 5 years counts you as a “partner-country” resident with lower funding.\nApply to at most three programmes per year.",
                    Timeline = "October–January (some until March): apply on each programme's own website.\nMarch–April: scholarship results and waiting lists.\nJune–August: visas and housing.\nSeptember: start at the first university, then move between partners along the track.",
                    Letter = "Answer: why this specific consortium, which of its tracks, and how your background serves the joint project.\nName professors or research outputs of the programme.\nUsually one page; respect the programme's limit.",
                    Mistakes = "Applying to more than three programmes (automatic exclusion).\nOne letter copied across programmes.\nIgnoring precise prerequisites (specific prior courses).\nLeaving references to the last day; some programmes require the referee to upload.",
                    Tips = "Start from the official EMJM catalogue, filter by field, then read each programme's Admission page.\nEmail the coordinator a specific question; the answer feeds your letter.\nOrganise documents with the required names; programme platforms are strict about formats.",
                    Checklist = new() { "Up to three programmes from the catalogue", "Bachelor's certificate and transcript", "Language certificate", "Motivation letter per programme", "Two reference letters", "CV", "Proof of residence for the last 5 years", "Passport" }
                },
                new GuideSections
                {
                    Conditions = "Une licence (ou dernière année, diplôme avant la rentrée).\nChaque programme fixe ses conditions académiques et linguistiques ; IELTS 6.5 est courant.\nNon éligible si déjà boursier Erasmus Mundus ; plus de 12 mois en Europe sur les 5 dernières années vous classe « pays partenaire » avec un financement réduit.\nTrois programmes au maximum par an.",
                    Timeline = "Octobre–janvier (certains jusqu'en mars) : candidature sur le site de chaque programme.\nMars–avril : résultats et listes d'attente.\nJuin–août : visas et logement.\nSeptembre : rentrée dans la première université, puis mobilité entre partenaires.",
                    Letter = "Répondez : pourquoi ce consortium précis, quel parcours, et comment votre profil sert le projet conjoint.\nCitez des enseignants ou travaux du programme.\nUne page en général ; respectez la limite imposée.",
                    Mistakes = "Plus de trois candidatures (exclusion automatique).\nUne même lettre pour plusieurs programmes.\nIgnorer les prérequis précis.\nRecommandations envoyées au dernier moment ; certains programmes exigent le dépôt par le référent.",
                    Tips = "Partez du catalogue officiel EMJM, filtrez par domaine, puis lisez la page Admission de chaque programme.\nÉcrivez au coordinateur une question précise ; la réponse nourrit votre lettre.\nNommez vos fichiers comme demandé ; les plateformes sont strictes.",
                    Checklist = new() { "Jusqu'à trois programmes du catalogue", "Diplôme de licence et relevés", "Certificat de langue", "Lettre de motivation par programme", "Deux recommandations", "CV", "Justificatif de résidence des 5 dernières années", "Passeport" }
                }));

            list.Add(Make("turkiye-burslari", "turkiye-scholarships", order++, 1, 2, "https://www.turkiyeburslari.gov.tr/",
                "دليل التقديم على المنح التركية (Türkiye Bursları)", "How to apply for Türkiye Scholarships", "Candidater aux bourses turques (Türkiye Bursları)",
                "منحة الحكومة التركية الممولة بالكامل للبكالوريوس والماستر والدكتوراه مع سنة لغة تركية، ومن أكثر المنح قبولًا للطلاب العرب.",
                "Türkiye's fully funded government scholarship for bachelor's, master's and PhD with a Turkish language year; one of the most accessible for Arab students.",
                "La bourse du gouvernement turc, entièrement financée, pour licence, master et doctorat avec une année de turc ; très accessible aux étudiants arabes.",
                new GuideSections
                {
                    Conditions = "السن: أقل من 21 للبكالوريوس، 30 للماستر، 35 للدكتوراه.\nمعدل: 70% للبكالوريوس (90% للطب)، 75% للماستر والدكتوراه.\nألا تكون مواطنًا تركيًا أو مسجلًا حاليًا في جامعة تركية.\nلا تُطلب شهادة لغة عند التقديم؛ سنة اللغة التركية مشمولة.",
                    Timeline = "يناير–فبراير: التقديم الإلكتروني على tbbs.turkiyeburslari.gov.tr.\nمارس–مايو: التقييم ودعوات المقابلات (في بلدك أو عبر الإنترنت).\nيوليو–أغسطس: النتائج.\nسبتمبر: السفر؛ سنة اللغة ثم التخصص.",
                    Letter = "خطاب الدوافع والأهداف يُقرأ بعناية: لماذا تركيا، لماذا هذا التخصص، وكيف ستفيد مجتمعك بعد التخرج.\nللماستر والدكتوراه أضف مقترحًا بحثيًا مختصرًا (صفحة) بعنوان وسؤال ومنهج.\nالتزم بحدود الكلمات في النموذج؛ لا تلصق نصوصًا مطولة.",
                    Mistakes = "ترتيب 12 برنامجًا بلا تدرّج (ضع برامج قوية ومتوسطة).\nرفع وثائق غير مقروءة أو غير مترجمة إلى التركية/الإنجليزية.\nتجاهل المقابلة: إنها الفيصل في الماستر والدكتوراه.\nكتابة أهداف عامة لا تذكر الجامعات التركية.",
                    Tips = "تعرّف على الأقسام والأساتذة في الجامعات التركية التي تختارها واذكرهم.\nجهّز إجابات مقابلة قصيرة عن مشروعك البحثي وخطتك بعد التخرج.\nالأنشطة التطوعية والشهادات تُحتسب؛ وثّقها.",
                    Checklist = new() { "صورة شخصية حديثة", "جواز سفر أو هوية", "شهادة الثانوية/البكالوريوس وكشف الدرجات", "خطاب دوافع وأهداف", "مقترح بحثي (ماستر/دكتوراه)", "خطابا توصية", "شهادات لغة ونشاطات (إن وجدت)" }
                },
                new GuideSections
                {
                    Conditions = "Age: under 21 for bachelor's, 30 for master's, 35 for PhD.\nGrades: 70% for bachelor's (90% for medicine), 75% for master's and PhD.\nNot a Turkish citizen and not currently enrolled in a Turkish university.\nNo language certificate at application; the Turkish language year is included.",
                    Timeline = "January–February: online application at tbbs.turkiyeburslari.gov.tr.\nMarch–May: evaluation and interview invitations (in your country or online).\nJuly–August: results.\nSeptember: travel; language year, then the degree.",
                    Letter = "The letter of intent is read carefully: why Türkiye, why this field, how you will serve your community after graduating.\nFor master's/PhD add a one-page research proposal: title, question, method.\nRespect the word limits in the form; do not paste long texts.",
                    Mistakes = "Ranking 12 programmes without a mix of strong and realistic choices.\nUploading unreadable documents or ones not translated into Turkish/English.\nUnderestimating the interview; it decides master's and PhD outcomes.\nGeneric goals that never mention Turkish universities.",
                    Tips = "Learn the departments and professors at the universities you choose and name them.\nPrepare short interview answers on your research project and post-graduation plan.\nVolunteering and certificates count; document them.",
                    Checklist = new() { "Recent photo", "Passport or ID", "High school/bachelor's certificate and transcript", "Letter of intent", "Research proposal (master's/PhD)", "Two reference letters", "Language and activity certificates (if any)" }
                },
                new GuideSections
                {
                    Conditions = "Âge : moins de 21 ans (licence), 30 (master), 35 (doctorat).\nNotes : 70 % (licence, 90 % en médecine), 75 % (master et doctorat).\nNi citoyen turc ni inscrit dans une université turque.\nAucun certificat de langue à la candidature ; l'année de turc est incluse.",
                    Timeline = "Janvier–février : candidature en ligne sur tbbs.turkiyeburslari.gov.tr.\nMars–mai : évaluation et entretiens (dans votre pays ou en ligne).\nJuillet–août : résultats.\nSeptembre : départ ; année de langue puis diplôme.",
                    Letter = "La lettre d'intention est lue attentivement : pourquoi la Turquie, pourquoi ce domaine, comment vous servirez votre communauté.\nPour master/doctorat, ajoutez un projet de recherche d'une page : titre, question, méthode.\nRespectez les limites de mots du formulaire.",
                    Mistakes = "Classer 12 programmes sans mélange de choix ambitieux et réalistes.\nDocuments illisibles ou non traduits.\nSous-estimer l'entretien, décisif en master et doctorat.\nObjectifs génériques sans universités turques nommées.",
                    Tips = "Renseignez-vous sur les départements et enseignants des universités choisies et nommez-les.\nPréparez des réponses courtes sur votre projet et votre plan après le diplôme.\nBénévolat et certificats comptent ; documentez-les.",
                    Checklist = new() { "Photo récente", "Passeport ou pièce d'identité", "Diplôme et relevés", "Lettre d'intention", "Projet de recherche (master/doctorat)", "Deux recommandations", "Certificats de langue et d'activités (le cas échéant)" }
                }));

            list.Add(Make("mext", "mext-japan", order++, 4, 5, "https://www.studyinjapan.go.jp/en/smap-stopj-applications-research.html",
                "دليل التقديم على منحة MEXT (اليابان) بترشيح السفارة", "How to apply for the MEXT scholarship (Japan) via embassy recommendation", "Candidater à la bourse MEXT (Japon) par recommandation de l'ambassade",
                "منحة وزارة التعليم اليابانية الممولة بالكامل للبحث (ماستر/دكتوراه) والبكالوريوس؛ المسار الأشهر عبر السفارة اليابانية في بلدك.",
                "Japan's fully funded Ministry of Education scholarship for research students (master's/PhD) and undergraduates; the common route is through the Japanese embassy in your country.",
                "La bourse du ministère japonais de l'Éducation, entièrement financée, pour chercheurs (master/doctorat) et étudiants de licence ; la voie classique passe par l'ambassade du Japon.",
                new GuideSections
                {
                    Conditions = "لطلاب البحث: بكالوريوس ومولود بعد تاريخ محدد (نحو 35 سنة).\nخطة بحث واضحة ومشرف محتمل في جامعة يابانية (موافقته تُطلب بعد الفحص الأولي).\nلغة: اليابانية أو الإنجليزية حسب الجامعة؛ تُجرى اختبارات لغة في السفارة.\nصحة جيدة وشهادة طبية على نموذج MEXT.",
                    Timeline = "أبريل–مايو: إعلان السفارة والتقديم (يختلف بلدًا عن بلد).\nيونيو–يوليو: الفحص الأولي، اختبارات اللغة، والمقابلة.\nأغسطس–سبتمبر: البحث عن مشرف وخطاب قبول مبدئي من الجامعة.\nيناير–فبراير: القرار النهائي من MEXT؛ السفر في أبريل أو أكتوبر.",
                    Letter = "خطة البحث هي قلب الملف: الخلفية، سؤال البحث، المنهج، والجدول الزمني في صفحتين.\nاربط الخطة بمجموعة بحثية يابانية بعينها واذكر أوراقها.\nرسالة التواصل مع المشرف: قصيرة، مع ملخص الخطة وسيرتك، وتذكر أنك مرشّح سفارة.",
                    Mistakes = "خطة بحث عامة بلا منهج ولا صلة بمختبر ياباني.\nمراسلة عشرات الأساتذة برسالة واحدة منسوخة.\nإهمال النموذج الطبي أو التوقيعات المطلوبة.\nالاعتقاد أن اليابانية شرط دائم؛ كثير من برامج الدراسات العليا بالإنجليزية.",
                    Tips = "ابحث عن المشرفين في قواعد الجامعات اليابانية وراسل من تطابق أبحاثه خطتك.\nتابع صفحة السفارة اليابانية في بلدك من مارس؛ المواعيد تختلف.\nحضّر لاختبار اللغة اليابانية حتى لو كان اختياريًا؛ يرفع ترتيبك.",
                    Checklist = new() { "استمارة MEXT", "خطة البحث (صفحتان)", "شهادات الدراسة وكشوف الدرجات", "خطاب توصية من المشرف الأكاديمي", "الشهادة الطبية على النموذج", "ملخص الرسالة/الأبحاث السابقة", "جواز سفر", "رسالة قبول مبدئي من مشرف ياباني (لاحقًا)" }
                },
                new GuideSections
                {
                    Conditions = "Research students: a bachelor's degree and born after a set date (about 35 years old).\nA clear research plan and a potential supervisor at a Japanese university (acceptance requested after the first screening).\nLanguage: Japanese or English depending on the university; the embassy runs language tests.\nGood health, with the MEXT medical form.",
                    Timeline = "April–May: embassy announcement and application (varies by country).\nJune–July: first screening, language tests, interview.\nAugust–September: find a supervisor and obtain a provisional acceptance letter.\nJanuary–February: final MEXT decision; departure in April or October.",
                    Letter = "The research plan is the heart of the file: background, question, method and timeline in two pages.\nTie it to a specific Japanese research group and cite its papers.\nThe email to the supervisor: short, with the plan summary and your CV, stating you are an embassy candidate.",
                    Mistakes = "A generic plan without a method or a link to a Japanese lab.\nMass-emailing dozens of professors with the same text.\nMissing the medical form or required signatures.\nAssuming Japanese is always required; many graduate programmes are in English.",
                    Tips = "Search Japanese university researcher databases and write to those whose work matches your plan.\nWatch your country's Japanese embassy page from March; dates differ.\nPrepare for the Japanese test even if optional; it raises your ranking.",
                    Checklist = new() { "MEXT application form", "Research plan (two pages)", "Degree certificates and transcripts", "Reference from your academic supervisor", "Medical certificate on the MEXT form", "Abstract of thesis/previous research", "Passport", "Provisional acceptance from a Japanese supervisor (later)" }
                },
                new GuideSections
                {
                    Conditions = "Étudiants-chercheurs : licence et date de naissance limite (environ 35 ans).\nUn projet de recherche clair et un directeur potentiel dans une université japonaise (accord demandé après la présélection).\nLangue : japonais ou anglais selon l'université ; tests à l'ambassade.\nBonne santé, formulaire médical MEXT.",
                    Timeline = "Avril–mai : annonce de l'ambassade et candidature (selon le pays).\nJuin–juillet : présélection, tests de langue, entretien.\nAoût–septembre : recherche d'un directeur et lettre d'acceptation provisoire.\nJanvier–février : décision finale du MEXT ; départ en avril ou octobre.",
                    Letter = "Le projet de recherche est le cœur du dossier : contexte, question, méthode, calendrier en deux pages.\nReliez-le à un groupe de recherche japonais précis et citez ses publications.\nE-mail au directeur : court, avec résumé du projet et CV, en précisant que vous êtes candidat ambassade.",
                    Mistakes = "Projet générique sans méthode ni lien avec un laboratoire japonais.\nEnvoi massif du même e-mail à des dizaines de professeurs.\nOublier le formulaire médical ou les signatures.\nCroire que le japonais est toujours exigé ; beaucoup de programmes sont en anglais.",
                    Tips = "Cherchez les directeurs dans les bases des universités japonaises et écrivez à ceux dont les travaux correspondent.\nSuivez la page de l'ambassade du Japon dès mars ; les dates varient.\nPréparez le test de japonais même s'il est facultatif ; il améliore le classement.",
                    Checklist = new() { "Formulaire MEXT", "Projet de recherche (deux pages)", "Diplômes et relevés", "Recommandation du directeur académique", "Certificat médical sur formulaire", "Résumé du mémoire/recherches", "Passeport", "Acceptation provisoire d'un directeur japonais (plus tard)" }
                }));

            list.Add(Make("fulbright", "fulbright", order++, 2, 5, "https://foreign.fulbrightonline.org/",
                "دليل التقديم على منحة فولبرايت (الولايات المتحدة)", "How to apply for the Fulbright Foreign Student Program (USA)", "Candidater au programme Fulbright pour étudiants étrangers (États-Unis)",
                "منحة الحكومة الأمريكية للماستر (وأحيانًا الدكتوراه) ممولة بالكامل، تُدار في كل دولة عربية عبر السفارة أو لجنة فولبرايت المحلية.",
                "The US government's fully funded master's (sometimes PhD) scholarship, run in each Arab country through the embassy or the local Fulbright commission.",
                "La bourse du gouvernement américain pour un master (parfois doctorat), entièrement financée, gérée dans chaque pays arabe par l'ambassade ou la commission Fulbright.",
                new GuideSections
                {
                    Conditions = "جنسية الدولة والإقامة فيها عند التقديم (لا يُقبل المقيمون في أمريكا).\nبكالوريوس بتقدير جيد وخبرة عمل غالبًا (سنتان في كثير من الدول).\nإنجليزية قوية: TOEFL iBT 80–90 أو IELTS 6.5 تقريبًا، وGRE/GMAT لبعض التخصصات.\nالتزام بالعودة سنتين بعد المنحة (تأشيرة J-1).\nالتخصصات المقبولة تختلف بلدًا عن بلد (الطب السريري مستثنى عادةً).",
                    Timeline = "فبراير–مايو: الإعلان والتقديم عبر سفارة أو لجنة بلدك (يختلف).\nيونيو–أغسطس: المقابلات واختبارات اللغة.\nسبتمبر–يناير: ترشيح مؤسسة IIE وطلبات الجامعات الأمريكية باسمك.\nمارس–مايو من العام التالي: القبول الجامعي؛ السفر في أغسطس.",
                    Letter = "بيانان: بيان الأهداف الدراسية (ماذا ستدرس ولماذا أمريكا) والبيان الشخصي (من أنت وما شكّلك).\nاذكر أثرك المتوقع في بلدك بعد العودة بمؤشرات محددة.\nلا تذكر جامعات بعينها إلا بحجة أكاديمية؛ IIE هي من تضع طلباتك.",
                    Mistakes = "خلط البيانَين أو تكرار المحتوى بينهما.\nإنجليزية غير جاهزة: نتيجة TOEFL/IELTS متأخرة تُسقط الملف.\nاختيار تخصص خارج قائمة بلدك.\nخطابات توصية من أشخاص لا يعرفون عملك.",
                    Tips = "ابدأ التحضير لـTOEFL/GRE ستة أشهر قبل الإعلان.\nراسل خريجي فولبرايت في بلدك؛ لجان كثيرة تعقد جلسات تعريفية.\nجهّز قصة واضحة عن «الأثر بعد العودة» تخدم البيانَين والمقابلة.",
                    Checklist = new() { "جواز سفر", "شهادة البكالوريوس وكشف الدرجات", "بيان الأهداف الدراسية", "البيان الشخصي", "ثلاثة خطابات توصية", "نتيجة TOEFL/IELTS", "GRE/GMAT (إن طُلب)", "سيرة ذاتية" }
                },
                new GuideSections
                {
                    Conditions = "Citizenship of the country and residence there at application (US residents are not eligible).\nA bachelor's with good grades, and often work experience (two years in many countries).\nStrong English: roughly TOEFL iBT 80–90 or IELTS 6.5, plus GRE/GMAT for some fields.\nCommitment to return for two years after the grant (J-1 visa).\nEligible fields differ by country (clinical medicine is usually excluded).",
                    Timeline = "February–May: announcement and application through your embassy or commission (varies).\nJune–August: interviews and language tests.\nSeptember–January: IIE nomination and US university applications filed for you.\nMarch–May of the next year: admissions; travel in August.",
                    Letter = "Two statements: study objectives (what you will study and why the US) and the personal statement (who you are and what shaped you).\nState your expected impact at home with concrete indicators.\nName universities only with an academic reason; IIE places your applications.",
                    Mistakes = "Mixing the two statements or repeating content between them.\nEnglish not ready: a late TOEFL/IELTS score sinks the file.\nChoosing a field outside your country's list.\nReferences from people who do not know your work.",
                    Tips = "Start TOEFL/GRE preparation six months before the announcement.\nContact Fulbright alumni in your country; many commissions hold info sessions.\nPrepare a clear “impact after return” story that serves both statements and the interview.",
                    Checklist = new() { "Passport", "Bachelor's certificate and transcript", "Study objectives statement", "Personal statement", "Three reference letters", "TOEFL/IELTS score", "GRE/GMAT (if required)", "CV" }
                },
                new GuideSections
                {
                    Conditions = "Nationalité du pays et résidence sur place à la candidature (résidents aux États-Unis non éligibles).\nLicence avec bonnes notes, souvent de l'expérience (deux ans dans beaucoup de pays).\nAnglais solide : environ TOEFL iBT 80–90 ou IELTS 6.5, GRE/GMAT pour certains domaines.\nEngagement à rentrer deux ans après la bourse (visa J-1).\nDomaines éligibles variables selon le pays (médecine clinique généralement exclue).",
                    Timeline = "Février–mai : annonce et candidature via ambassade ou commission (variable).\nJuin–août : entretiens et tests de langue.\nSeptembre–janvier : nomination par l'IIE et dépôt des candidatures universitaires.\nMars–mai de l'année suivante : admissions ; départ en août.",
                    Letter = "Deux textes : objectifs d'études (quoi et pourquoi les États-Unis) et déclaration personnelle (qui vous êtes).\nIndiquez l'impact attendu chez vous avec des indicateurs concrets.\nNe citez des universités qu'avec une raison académique ; l'IIE dépose vos candidatures.",
                    Mistakes = "Mélanger les deux textes ou répéter le contenu.\nAnglais non prêt : un score tardif fait tomber le dossier.\nChoisir un domaine hors liste du pays.\nRecommandations de personnes ne connaissant pas votre travail.",
                    Tips = "Préparez TOEFL/GRE six mois avant l'annonce.\nContactez les anciens Fulbright de votre pays ; beaucoup de commissions font des séances d'information.\nPréparez une histoire claire de « l'impact au retour » pour les textes et l'entretien.",
                    Checklist = new() { "Passeport", "Diplôme de licence et relevés", "Objectifs d'études", "Déclaration personnelle", "Trois recommandations", "Score TOEFL/IELTS", "GRE/GMAT (si exigé)", "CV" }
                }));

            list.Add(Make("csc", "china-csc", order++, 12, 4, "https://www.campuschina.org/",
                "دليل التقديم على منحة الحكومة الصينية (CSC)", "How to apply for the Chinese Government Scholarship (CSC)", "Candidater à la bourse du gouvernement chinois (CSC)",
                "منح الحكومة الصينية للبكالوريوس والماستر والدكتوراه، ممولة بالكامل، عبر السفارة الصينية (النوع A) أو الجامعة مباشرة (النوع B).",
                "Chinese government scholarships for bachelor's, master's and PhD, fully funded, through the Chinese embassy (Type A) or directly through a university (Type B).",
                "Bourses du gouvernement chinois pour licence, master et doctorat, entièrement financées, via l'ambassade (type A) ou directement l'université (type B).",
                new GuideSections
                {
                    Conditions = "السن: أقل من 25 للبكالوريوس، 35 للماستر، 40 للدكتوراه.\nشهادة المرحلة السابقة بتقدير جيد، وصحة جيدة على نموذج الفحص الطبي الصيني.\nالإنجليزية للبرامج الإنجليزية (IELTS 6.0 شائع) أو HSK للبرامج الصينية؛ سنة لغة ممكنة.\nلا يُقبل المسجلون حاليًا في جامعة صينية.",
                    Timeline = "ديسمبر–يناير: فتح النظام الإلكتروني (campuschina.org).\nيناير–أبريل: التقديم عبر السفارة (A) أو الجامعة (B) حسب مواعيد كل منهما.\nمايو–يوليو: نتائج الجامعات وCSC.\nسبتمبر: بدء الدراسة.",
                    Letter = "خطة الدراسة/البحث: 800 كلمة على الأقل للماستر والدكتوراه؛ اشرح الموضوع والمنهج والنتائج المتوقعة.\nخطاب قبول مبدئي من أستاذ صيني يرفع فرصك كثيرًا في النوع B؛ راسل الأساتذة بملخص خطتك.\nاذكر أثر الدراسة في الصين على مجالك في بلدك.",
                    Mistakes = "الخطأ في رقم وكالة التقديم (Agency Number) للجامعة أو السفارة.\nتجاوز مواعيد الجامعة التي تسبق موعد CSC العام.\nفحص طبي قديم أو على نموذج غير النموذج الصيني.\nخطة دراسة قصيرة جدًا.",
                    Tips = "راسل 5–10 أساتذة بتخصصك قبل التقديم بشهرين؛ قبول مبدئي واحد يكفي.\nجهّز الترجمة الموثقة للشهادات إلى الإنجليزية أو الصينية.\nاحتفظ برقم التسجيل في نظام CSC؛ تحتاجه في كل مراسلة.",
                    Checklist = new() { "استمارة CSC الإلكترونية (رقم الوكالة الصحيح)", "جواز سفر", "الشهادات وكشوف الدرجات مترجمة", "خطة الدراسة أو البحث (800 كلمة)", "خطابا توصية", "نموذج الفحص الطبي الصيني", "شهادة لغة", "خطاب قبول مبدئي (النوع B)" }
                },
                new GuideSections
                {
                    Conditions = "Age: under 25 for bachelor's, 35 for master's, 40 for PhD.\nGood grades in the previous degree and good health on the Chinese medical form.\nEnglish for English programmes (IELTS 6.0 is common) or HSK for Chinese ones; a language year is possible.\nNot currently enrolled in a Chinese university.",
                    Timeline = "December–January: the online system opens (campuschina.org).\nJanuary–April: apply through the embassy (A) or the university (B) by their own deadlines.\nMay–July: university and CSC results.\nSeptember: start.",
                    Letter = "Study/research plan: at least 800 words for master's and PhD; explain topic, method and expected results.\nA pre-admission letter from a Chinese professor greatly improves Type B chances; email professors with your plan summary.\nState how studying in China will affect your field at home.",
                    Mistakes = "Wrong Agency Number for the university or embassy.\nMissing the university deadline that precedes the general CSC one.\nAn old medical check or a non-Chinese form.\nA very short study plan.",
                    Tips = "Email 5–10 professors in your field two months before applying; one pre-admission is enough.\nPrepare certified translations into English or Chinese.\nKeep your CSC registration number; you need it in every message.",
                    Checklist = new() { "CSC online form (correct agency number)", "Passport", "Translated certificates and transcripts", "Study or research plan (800 words)", "Two reference letters", "Chinese medical examination form", "Language certificate", "Pre-admission letter (Type B)" }
                },
                new GuideSections
                {
                    Conditions = "Âge : moins de 25 ans (licence), 35 (master), 40 (doctorat).\nBonnes notes au diplôme précédent et bonne santé sur le formulaire médical chinois.\nAnglais pour les programmes en anglais (IELTS 6.0 courant) ou HSK pour les programmes en chinois ; année de langue possible.\nNon inscrit dans une université chinoise.",
                    Timeline = "Décembre–janvier : ouverture du système (campuschina.org).\nJanvier–avril : candidature via l'ambassade (A) ou l'université (B) selon leurs dates.\nMai–juillet : résultats.\nSeptembre : rentrée.",
                    Letter = "Projet d'études/recherche : au moins 800 mots en master et doctorat ; sujet, méthode, résultats attendus.\nUne pré-admission d'un professeur chinois augmente beaucoup les chances en type B ; écrivez avec un résumé du projet.\nIndiquez l'impact de vos études en Chine sur votre domaine chez vous.",
                    Mistakes = "Mauvais numéro d'agence (université ou ambassade).\nRater la date de l'université, antérieure à celle du CSC.\nExamen médical ancien ou sur un autre formulaire.\nProjet d'études trop court.",
                    Tips = "Écrivez à 5–10 professeurs deux mois avant ; une pré-admission suffit.\nPréparez des traductions certifiées en anglais ou chinois.\nConservez votre numéro d'inscription CSC pour toute correspondance.",
                    Checklist = new() { "Formulaire CSC en ligne (bon numéro d'agence)", "Passeport", "Diplômes et relevés traduits", "Projet d'études ou de recherche (800 mots)", "Deux recommandations", "Formulaire médical chinois", "Certificat de langue", "Pré-admission (type B)" }
                }));

            list.Add(Make("mastercard-foundation", "mastercard-foundation-scholars", order++, 9, 1, "https://mastercardfdn.org/all/scholars/",
                "دليل التقديم على برنامج Mastercard Foundation Scholars", "How to apply for the Mastercard Foundation Scholars Program", "Candidater au programme Mastercard Foundation Scholars",
                "منح كاملة للشباب الأفارقة الموهوبين من خلفيات محدودة الدخل في جامعات شريكة بأفريقيا والعالم؛ تناسب شباب الدول العربية الأفريقية خاصة.",
                "Full scholarships for talented young Africans from low-income backgrounds at partner universities in Africa and worldwide; relevant for youth from African Arab countries.",
                "Bourses complètes pour jeunes Africains talentueux de milieux modestes dans des universités partenaires en Afrique et dans le monde ; pertinent pour les pays arabes d'Afrique.",
                new GuideSections
                {
                    Conditions = "التقديم عبر جامعة شريكة لا عبر المؤسسة مباشرة؛ كل جامعة تحدد الدول والمراحل المقبولة.\nحاجة مالية موثقة وتفوق أكاديمي والتزام بخدمة المجتمع.\nالسن: غالبًا دون 29 للبكالوريوس ودون 35 للماستر (يختلف).\nكثير من الجامعات الشريكة تقبل طلاب شمال أفريقيا (مصر، السودان، المغرب، تونس، الجزائر، ليبيا).",
                    Timeline = "سبتمبر–يناير: فتح طلبات الجامعات الشريكة لدفعة العام التالي (يختلف بين الجامعات).\nفبراير–أبريل: المقابلات والتحقق من الحاجة المالية.\nمايو–يوليو: القرارات.\nأغسطس–سبتمبر: الوصول وبرنامج التهيئة.",
                    Letter = "البيان الشخصي يربط ثلاثة خيوط: ظروفك المالية بصراحة وكرامة، إنجازك الأكاديمي رغمها، وما فعلته لمجتمعك.\nاشرح كيف ستعود لتقود تغييرًا في مجتمعك («العودة والعطاء» قيمة أساسية).\nكن محددًا: أسماء، أرقام، نتائج.",
                    Mistakes = "التقديم للمؤسسة بدل الجامعة الشريكة.\nإخفاء الحاجة المالية أو المبالغة فيها بلا وثائق.\nبيان شخصي يركز على الطموح الفردي دون المجتمع.\nتجاوز حد السن للجامعة المختارة.",
                    Tips = "راجع قائمة الجامعات الشريكة واختر ما يقبل بلدك ومرحلتك، ثم اقرأ شروطها بدقة.\nجهّز وثائق الحاجة المالية (دخل الأسرة، شهادات) مبكرًا.\nوثّق عملك المجتمعي بصور وشهادات من المنظمات.",
                    Checklist = new() { "اختيار الجامعة الشريكة المناسبة", "شهادات الدراسة وكشوف الدرجات", "البيان الشخصي", "وثائق الحاجة المالية", "خطابا توصية (أكاديمي ومجتمعي)", "إثبات العمل المجتمعي", "جواز سفر أو هوية", "شهادة لغة (إن طُلبت)" }
                },
                new GuideSections
                {
                    Conditions = "Apply through a partner university, not the Foundation; each university sets eligible countries and levels.\nDocumented financial need, academic excellence and a commitment to community service.\nAge: often under 29 for bachelor's and under 35 for master's (varies).\nMany partners accept students from North Africa (Egypt, Sudan, Morocco, Tunisia, Algeria, Libya).",
                    Timeline = "September–January: partner universities open applications for the next intake (varies).\nFebruary–April: interviews and financial-need verification.\nMay–July: decisions.\nAugust–September: arrival and orientation.",
                    Letter = "The personal statement ties three threads: your financial circumstances with honesty and dignity, your academic achievement despite them, and what you did for your community.\nExplain how you will return to lead change (“give back” is a core value).\nBe specific: names, numbers, results.",
                    Mistakes = "Applying to the Foundation instead of a partner university.\nHiding or exaggerating financial need without documents.\nA statement focused on personal ambition without community.\nExceeding the chosen university's age limit.",
                    Tips = "Review the partner list, pick those accepting your country and level, then read their conditions closely.\nPrepare financial-need documents (family income, certificates) early.\nDocument community work with photos and letters from organizations.",
                    Checklist = new() { "Chosen partner university", "Certificates and transcripts", "Personal statement", "Financial-need documents", "Two references (academic and community)", "Proof of community work", "Passport or ID", "Language certificate (if required)" }
                },
                new GuideSections
                {
                    Conditions = "Candidature via une université partenaire, pas la Fondation ; chaque université fixe pays et niveaux éligibles.\nBesoin financier documenté, excellence académique et engagement communautaire.\nÂge : souvent moins de 29 ans (licence) et 35 (master), variable.\nBeaucoup de partenaires acceptent l'Afrique du Nord (Égypte, Soudan, Maroc, Tunisie, Algérie, Libye).",
                    Timeline = "Septembre–janvier : ouverture des candidatures des universités partenaires (variable).\nFévrier–avril : entretiens et vérification du besoin financier.\nMai–juillet : décisions.\nAoût–septembre : arrivée et intégration.",
                    Letter = "La déclaration personnelle relie trois fils : votre situation financière avec honnêteté et dignité, votre réussite académique malgré elle, et votre action pour la communauté.\nExpliquez comment vous reviendrez mener le changement (« rendre » est une valeur centrale).\nSoyez précis : noms, chiffres, résultats.",
                    Mistakes = "Candidater à la Fondation au lieu d'une université partenaire.\nCacher ou exagérer le besoin financier sans documents.\nDéclaration centrée sur l'ambition individuelle sans la communauté.\nDépasser la limite d'âge de l'université.",
                    Tips = "Consultez la liste des partenaires, choisissez ceux qui acceptent votre pays et niveau, lisez leurs conditions.\nPréparez tôt les justificatifs financiers.\nDocumentez votre engagement communautaire par des photos et attestations.",
                    Checklist = new() { "Université partenaire choisie", "Diplômes et relevés", "Déclaration personnelle", "Justificatifs de besoin financier", "Deux recommandations (académique et communautaire)", "Preuve d'engagement communautaire", "Passeport ou pièce d'identité", "Certificat de langue (si exigé)" }
                }));

            return list;
        }

        private static ApplicationGuide Make(string key, string slug, int order, int opens, int closes, string url,
            string titleAr, string titleEn, string titleFr, string summaryAr, string summaryEn, string summaryFr,
            GuideSections ar, GuideSections en, GuideSections fr)
        {
            var g = new ApplicationGuide
            {
                ProgramKey = key, Slug = slug, SortOrder = order, OpensMonth = opens, ClosesMonth = closes, OfficialUrl = url, IsPublished = true,
                TitleAr = titleAr, TitleEn = titleEn, TitleFr = titleFr, SummaryAr = summaryAr, SummaryEn = summaryEn, SummaryFr = summaryFr
            };
            g.SetSections(ar, en, fr);
            return g;
        }
    }
}
