using Foras_Khadra.Models;
using Microsoft.EntityFrameworkCore;

namespace Foras_Khadra.Data
{
    /// <summary>
    /// المشاريع الرائدة الثلاثة كما وردت في «ملف الشركاء والمستثمرين 2026». تُزرع مرة واحدة عندما يكون الجدول فارغًا
    /// (في الإنتاج أيضًا)، وبعدها يحرّرها الأدمن من لوحة التحكم. الصور في wwwroot/images/programs (مرفوعة مع الموقع).
    /// </summary>
    public static class FlagshipProgramSeeder
    {
        public static async Task<int> SeedAsync(ApplicationDbContext db)
        {
            if (await db.FlagshipPrograms.AnyAsync()) return 0;

            db.FlagshipPrograms.AddRange(Rcoy(), ClimateVoices(), CapacityBuilding());
            return await db.SaveChangesAsync();
        }

        private static ProgramStat Stat(string value, string ar, string en, string fr) => new() { Value = value, Ar = ar, En = en, Fr = fr };
        private static ProgramMilestone Step(string year, string ar, string en, string fr) => new() { Year = year, Ar = ar, En = en, Fr = fr };

        private static FlagshipProgram Rcoy() => new()
        {
            Slug = "rcoy-mena",
            SortOrder = 1,
            MenuTitleAr = "RCOY MENA",
            MenuTitleEn = "RCOY MENA",
            MenuTitleFr = "RCOY MENA",
            TitleAr = "RCOY MENA — المؤتمر الإقليمي للشباب والمناخ",
            TitleEn = "RCOY MENA — Regional Conference of Youth on Climate",
            TitleFr = "RCOY MENA — Conférence régionale des jeunes sur le climat",
            SubtitleAr = "نزوى، سلطنة عُمان · 16 – 19 سبتمبر 2026 · ضمن المسار الرسمي لشبكة YOUNGO التمهيدي لمؤتمر الأطراف",
            SubtitleEn = "Nizwa, Sultanate of Oman · 16 – 19 September 2026 · within YOUNGO's official pre-COP track",
            SubtitleFr = "Nizwa, Sultanat d'Oman · 16 – 19 septembre 2026 · dans le cadre officiel pré-COP de YOUNGO",
            HeadlineValue = "500+",
            HeadlineLabelAr = "مشارك ومشاركة من أكثر من 22 دولة",
            HeadlineLabelEn = "participants from more than 22 countries",
            HeadlineLabelFr = "participants venus de plus de 22 pays",
            IntroAr = "<p>أكبر تجمّع إقليمي للشباب والمناخ في المنطقة العربية ضمن المسار الرسمي لشبكة YOUNGO التمهيدي لمؤتمر الأطراف. استضافته فرص خضراء رسميًا بالتعاون مع منظمة «مدد للتنمية» بعد اختيارها ضمن دعوة الاستضافة السنوية.</p><p>انطلق المؤتمر من قلب أسبوع عُمان المناخي، وعُقدت جلساته في متحف عُمان عبر الزمان بمنح، واختُتم تحت رعاية والي نزوى ومحافظ الداخلية بالإنابة باعتماد البيان الإقليمي للشباب الذي يُرفع إلى مؤتمر الأطراف COP31 في أنطاليا.</p>",
            IntroEn = "<p>The largest regional youth climate gathering in the Arab region, within YOUNGO's official pre-COP track. Foras Khadra was the official host, in partnership with Madad for Development, after being selected through the annual hosting call.</p><p>The conference opened at the heart of Oman Climate Week, held its sessions at the Oman Across Ages Museum in Manah, and closed under the patronage of the Wali of Nizwa and Acting Governor of Al Dakhiliyah with the adoption of the Regional Youth Statement, submitted to COP31 in Antalya.</p>",
            IntroFr = "<p>Le plus grand rassemblement régional de jeunes pour le climat dans la région arabe, dans le cadre officiel pré-COP du réseau YOUNGO. Foras Khadra en a été l'hôte officiel, en partenariat avec Madad for Development, après avoir été sélectionnée lors de l'appel annuel à candidatures.</p><p>La conférence s'est ouverte au cœur de la Semaine du climat d'Oman, a tenu ses sessions au musée Oman Across Ages à Manah, et s'est clôturée sous le patronage du Wali de Nizwa et gouverneur par intérim d'Al Dakhiliyah par l'adoption de la Déclaration régionale des jeunes, soumise à la COP31 à Antalya.</p>",
            Stats = new List<ProgramStat>
            {
                Stat("22+", "دولة من المشرق والمغرب والخليج", "countries from the Mashreq, Maghreb and Gulf", "pays du Machrek, du Maghreb et du Golfe"),
                Stat("4 · 28", "مسارات موضوعية · جلسة وورشة متوازية", "thematic tracks · parallel sessions and workshops", "axes thématiques · sessions et ateliers parallèles"),
                Stat("180", "دقيقة من مختبرات السياسات التفاعلية بمشاركة الجميع", "minutes of fully interactive Policy Labs", "minutes de laboratoires de politiques entièrement interactifs"),
                Stat("24 → 9", "فريقًا في هاكاثون «آفاق» → 9 فرق في النهائي", "Afaq hackathon teams → 9 finalists", "équipes du hackathon « Afaq » → 9 finalistes"),
                Stat("RYS 2026", "البيان الإقليمي للشباب — يُرفع إلى COP31 في أنطاليا", "Regional Youth Statement — submitted to COP31 in Antalya", "Déclaration régionale des jeunes — soumise à la COP31 à Antalya"),
                Stat("TeckEvent", "تطبيق رسمي للمؤتمر: أجندة، تحديات، تشبيك", "official conference app: agenda, challenges, networking", "application officielle : agenda, défis, réseautage")
            },
            Timeline = new List<ProgramMilestone>
            {
                Step("2024", "شريك رسمي وبوث في RCOY MENA – الأردن، وشراكات مع عدد من مؤتمرات LCOY", "Official partner and booth at RCOY MENA – Jordan, with partnerships across several LCOY conferences", "Partenaire officiel et stand à RCOY MENA – Jordanie, avec des partenariats avec plusieurs conférences LCOY"),
                Step("2025", "شراكات رسمية مع 10 مؤتمرات LCOY وطنية في ليبيا وفلسطين وجيبوتي والصومال والسودان والمغرب والجزائر والأردن والعراق وسوريا", "Official partnerships with 10 national LCOY conferences in Libya, Palestine, Djibouti, Somalia, Sudan, Morocco, Algeria, Jordan, Iraq and Syria", "Partenariats officiels avec 10 conférences LCOY nationales en Libye, Palestine, Djibouti, Somalie, Soudan, Maroc, Algérie, Jordanie, Irak et Syrie"),
                Step("2026", "المستضيف الرسمي لمؤتمر RCOY MENA 2026 في نزوى بالتعاون مع «مدد للتنمية» — 500+ مشارك من 22 دولة", "Official host of RCOY MENA 2026 in Nizwa, with Madad for Development — 500+ participants from 22 countries", "Hôte officiel de RCOY MENA 2026 à Nizwa, avec Madad for Development — 500+ participants de 22 pays")
            },
            OutcomesHeadingAr = "من LCOY إلى COP",
            OutcomesHeadingEn = "From LCOY to COP",
            OutcomesHeadingFr = "De la LCOY à la COP",
            OutcomesAr = "<p>مؤتمر LCOY هو المؤتمر الوطني للشباب حول المناخ في كل دولة، بينما يجمع RCOY مخرجات هذه المؤتمرات على المستوى الإقليمي تمهيدًا لمؤتمرات الأطراف — وكلاهما تحت مظلة YOUNGO، الدائرة الرسمية للشباب في اتفاقية الأمم المتحدة الإطارية لتغير المناخ.</p><ul><li><strong>LCOY</strong> مؤتمر الشباب المحلي</li><li><strong>RCOY</strong> مؤتمر الشباب الإقليمي — استضفناه في 2026</li><li><strong>COY</strong> مؤتمر الشباب العالمي</li><li><strong>COP</strong> مؤتمر الأطراف</li></ul>",
            OutcomesEn = "<p>LCOY is each country's national youth conference on climate; RCOY brings their outcomes together at the regional level ahead of the COP — both under YOUNGO, the official youth constituency of the UN Framework Convention on Climate Change.</p><ul><li><strong>LCOY</strong> Local Conference of Youth</li><li><strong>RCOY</strong> Regional Conference of Youth — we hosted it in 2026</li><li><strong>COY</strong> Conference of Youth</li><li><strong>COP</strong> UN Climate Change Conference</li></ul>",
            OutcomesFr = "<p>La LCOY est la conférence nationale des jeunes sur le climat de chaque pays ; la RCOY rassemble leurs résultats au niveau régional avant la COP — toutes deux sous l'égide de YOUNGO, la circonscription officielle des jeunes de la CCNUCC.</p><ul><li><strong>LCOY</strong> Conférence locale des jeunes</li><li><strong>RCOY</strong> Conférence régionale des jeunes — que nous avons accueillie en 2026</li><li><strong>COY</strong> Conférence des jeunes</li><li><strong>COP</strong> Conférence des Parties</li></ul>",
            PartnersAr = "بالتعاون مع مدد للتنمية – عُمان · YOUNGO · أسبوع عُمان المناخي · تحت رعاية والي نزوى",
            PartnersEn = "With Madad for Development – Oman · YOUNGO · Oman Climate Week · under the patronage of the Wali of Nizwa",
            PartnersFr = "Avec Madad for Development – Oman · YOUNGO · Semaine du climat d'Oman · sous le patronage du Wali de Nizwa",
            HeroImagePath = "/images/programs/rcoy-mena-hero.jpg",
            Gallery = new List<string> { "/images/programs/rcoy-mena-2.jpg", "/images/programs/rcoy-mena-3.jpg" }
        };

        private static FlagshipProgram ClimateVoices() => new()
        {
            Slug = "climate-voices-summit",
            SortOrder = 2,
            MenuTitleAr = "قمة صنّاع المحتوى",
            MenuTitleEn = "Content Creators Summit",
            MenuTitleFr = "Sommet des créateurs de contenu",
            TitleAr = "قمة صنّاع المحتوى المناخي — برنامج «أصوات المناخ»",
            TitleEn = "Climate Content Creators Summit — the “Climate Voices” programme",
            TitleFr = "Sommet des créateurs de contenu climatique — programme « Voix du climat »",
            SubtitleAr = "القاهرة، 17 – 18 يوليو 2026 · تحت رعاية جامعة الدول العربية · بالشراكة مع Climatic Peace",
            SubtitleEn = "Cairo, 17 – 18 July 2026 · under the patronage of the League of Arab States · with Climatic Peace",
            SubtitleFr = "Le Caire, 17 – 18 juillet 2026 · sous le patronage de la Ligue des États arabes · avec Climatic Peace",
            HeadlineValue = "600K+",
            HeadlineLabelAr = "شخص وصلهم محتوى مناخي عربي صنعه شباب 9 دول — في 19 يومًا فقط",
            HeadlineLabelEn = "people reached by Arabic climate content made by youth from 9 countries — in just 19 days",
            HeadlineLabelFr = "personnes touchées par du contenu climatique en arabe créé par des jeunes de 9 pays — en 19 jours seulement",
            IntroAr = "<p>أول برنامج إقليمي للتواصل المناخي باللغة العربية في 9 دول جنوب متوسطية (الجزائر، مصر، الأردن، لبنان، ليبيا، المغرب، فلسطين، سوريا، تونس)، قادته فرص خضراء إقليميًا مع Climatic Peace (مؤسسة نفرتاري).</p><p>تدرّج البرنامج من تدريب أونلاين (14 – 24 أبريل)، إلى 9 معسكرات وطنية أنتج فيها كل مشارك أول محتوى مناخي عربي له، إلى حملة رقمية إقليمية على 5 منصات، وتُوّج بقمة القاهرة التي حضرها 42 مشاركًا من 9 دول، وانضم إليها EU Jeel وEGYouth4Climate شريكين جديدين.</p>",
            IntroEn = "<p>The first regional climate communication programme in Arabic across 9 Southern Mediterranean countries (Algeria, Egypt, Jordan, Lebanon, Libya, Morocco, Palestine, Syria and Tunisia), led regionally by Foras Khadra with Climatic Peace (Nefertari Foundation).</p><p>It moved from online training (14 – 24 April), to 9 national camps where every participant produced their first Arabic climate piece, to a regional campaign on 5 platforms, and culminated in the Cairo Summit with 42 participants from 9 countries, joined by EU Jeel and EGYouth4Climate as new partners.</p>",
            IntroFr = "<p>Le premier programme régional de communication climatique en arabe dans 9 pays du sud de la Méditerranée (Algérie, Égypte, Jordanie, Liban, Libye, Maroc, Palestine, Syrie et Tunisie), mené au niveau régional par Foras Khadra avec Climatic Peace (Fondation Nefertari).</p><p>Il est passé d'une formation en ligne (14 – 24 avril) à 9 camps nationaux où chaque participant a produit son premier contenu climatique en arabe, puis à une campagne régionale sur 5 plateformes, avant de culminer avec le Sommet du Caire réunissant 42 participants de 9 pays, rejoints par EU Jeel et EGYouth4Climate comme nouveaux partenaires.</p>",
            Stats = new List<ProgramStat>
            {
                Stat("914", "متقدمًا مسجّلًا", "registered applicants", "candidats inscrits"),
                Stat("200+", "متدربًا · 14 ساعة تدريب", "trainees · 14 hours of training", "stagiaires · 14 heures de formation"),
                Stat("9", "معسكرات وطنية · 203 مشاركين", "national camps · 203 participants", "camps nationaux · 203 participants"),
                Stat("42", "مشاركًا في قمة القاهرة من 9 دول", "participants at the Cairo Summit from 9 countries", "participants au Sommet du Caire, de 9 pays"),
                Stat("54.7%", "نساء — الهدف كان 50%", "women — against a 50% target", "de femmes — contre un objectif de 50 %"),
                Stat("70+", "ريلز وكاروسيل من أكثر من 45 مدينة", "reels and carousels from 45+ cities", "reels et carrousels de plus de 45 villes"),
                Stat("1,000+", "مدعوّون لصياغة الميثاق", "invited to draft the manifesto", "invités à rédiger le manifeste"),
                Stat("50+", "وسيلة إعلام وجامعة ومنظمة يصلها الميثاق", "media outlets, universities and NGOs receiving it", "médias, universités et ONG destinataires")
            },
            Timeline = new List<ProgramMilestone>
            {
                Step("14 – 24 أبريل", "تدريب أونلاين: 914 متقدمًا، 200+ متدرب، 14 ساعة تدريب", "Online training: 914 applicants, 200+ trainees, 14 hours", "Formation en ligne : 914 candidats, 200+ stagiaires, 14 heures"),
                Step("9 معسكرات", "معسكرات وطنية في 9 دول أنتج فيها 203 مشاركين أول محتوى مناخي عربي لهم", "National camps in 9 countries where 203 participants produced their first Arabic climate piece", "Camps nationaux dans 9 pays où 203 participants ont produit leur premier contenu climatique en arabe"),
                Step("19 يومًا", "حملة #ClimateVoicesMENA على 5 منصات: 70+ ريلز وكاروسيل من 45+ مدينة و600K+ وصول", "#ClimateVoicesMENA campaign on 5 platforms: 70+ reels and carousels from 45+ cities, 600K+ reach", "Campagne #ClimateVoicesMENA sur 5 plateformes : 70+ reels et carrousels de 45+ villes, 600K+ de portée"),
                Step("17 – 18 يوليو", "قمة القاهرة الإقليمية تحت رعاية جامعة الدول العربية: 42 مشاركًا من 9 دول", "Cairo Regional Summit under the patronage of the League of Arab States: 42 participants from 9 countries", "Sommet régional du Caire sous le patronage de la Ligue des États arabes : 42 participants de 9 pays")
            },
            OutcomesHeadingAr = "مخرجات القمة — تحققت جميعها",
            OutcomesHeadingEn = "Summit outputs — all delivered",
            OutcomesHeadingFr = "Résultats du sommet — tous atteints",
            OutcomesAr = "<ul><li>اعتماد «ميثاق التواصل المناخي الأخلاقي في الإعلام العربي» في 18 يوليو 2026 — أول ميثاق من نوعه، صاغه الشباب أنفسهم.</li><li>اعتماد الدليل الأخلاقي لمتحدثي الحملات، والإطلاق الرسمي لشبكة شباب جنوب المتوسط للإعلام المناخي بحوكمة وخطة عمل لاثني عشر شهرًا.</li><li>إصدار «أصوات من أجل المناخ: قصص من جنوب المتوسط»، وتوزيع الميثاق على أكثر من 50 وسيلة إعلام وجامعة ومنظمة.</li></ul>",
            OutcomesEn = "<ul><li>Adoption of the “Manifesto for Ethical Climate Communication in Arabic Media” on 18 July 2026 — the first of its kind, drafted by young people themselves.</li><li>Adoption of the Ethical Guidelines for Campaign Communicators, and the formal launch of the South Mediterranean Youth Climate Media Network with governance and a 12-month workplan.</li><li>Publication of “Voices for the Climate: Stories from the South Mediterranean”, and distribution of the manifesto to 50+ media outlets, universities and NGOs.</li></ul>",
            OutcomesFr = "<ul><li>Adoption du « Manifeste pour une communication climatique éthique dans les médias arabes » le 18 juillet 2026 — le premier du genre, rédigé par les jeunes eux-mêmes.</li><li>Adoption des lignes directrices éthiques pour les communicants de campagne, et lancement officiel du Réseau des jeunes du sud de la Méditerranée pour les médias climatiques, avec une gouvernance et un plan de travail sur 12 mois.</li><li>Publication de « Voix pour le climat : histoires du sud de la Méditerranée », et diffusion du manifeste à plus de 50 médias, universités et ONG.</li></ul>",
            PartnersAr = "بالشراكة مع Climatic Peace (مؤسسة نفرتاري) · تحت رعاية جامعة الدول العربية · EU Jeel Connect · EGYouth4Climate",
            PartnersEn = "With Climatic Peace (Nefertari Foundation) · under the patronage of the League of Arab States · EU Jeel Connect · EGYouth4Climate",
            PartnersFr = "Avec Climatic Peace (Fondation Nefertari) · sous le patronage de la Ligue des États arabes · EU Jeel Connect · EGYouth4Climate",
            HeroImagePath = "/images/programs/climate-voices-hero.jpg",
            Gallery = new List<string>()
        };

        private static FlagshipProgram CapacityBuilding() => new()
        {
            Slug = "youth-capacity-building",
            SortOrder = 3,
            MenuTitleAr = "برنامج فرص خضراء لبناء قدرات الشباب العربي",
            MenuTitleEn = "Arab Youth Capacity-Building Programme",
            MenuTitleFr = "Programme de renforcement des capacités",
            TitleAr = "برنامج فرص خضراء لبناء قدرات الشباب العربي",
            TitleEn = "Foras Khadra Arab Youth Capacity-Building Programme",
            TitleFr = "Programme Foras Khadra de renforcement des capacités des jeunes arabes",
            SubtitleAr = "بتمويل مؤسسة آنا ليند (Anna Lindh Foundation) · 13 تدريبًا في 12 مدينة و7 دول عربية",
            SubtitleEn = "Funded by the Anna Lindh Foundation · 13 trainings in 12 cities across 7 Arab countries",
            SubtitleFr = "Financé par la Fondation Anna Lindh · 13 formations dans 12 villes de 7 pays arabes",
            HeadlineValue = "505",
            HeadlineLabelAr = "متدربًا ومتدربة اختيروا من 3,165 متقدّم (نسبة قبول 16%)",
            HeadlineLabelEn = "trainees selected from 3,165 applicants (16% acceptance rate)",
            HeadlineLabelFr = "stagiaires sélectionnés parmi 3 165 candidats (taux d'admission de 16 %)",
            IntroAr = "<p>تدريب حضوري مكثّف ليوم كامل يحوّل وعي الشباب بالمناخ إلى مبادرات عملية، عبر رحلة من ثلاث مراحل: فهم التغيّر المناخي وتحدياته في المنطقة العربية، ثم إتقان البحث عن الفرص وتقييم ملاءمتها واستراتيجيات رفع نسب القبول، وصولًا إلى تصميم مبادرات بيئية محلية وعرضها.</p><p>يستهدف البرنامج بشكل خاص المدن خارج العواصم والمناطق الأقل وصولًا: 62% من التدريبات نُفّذت خارج العواصم، ونسبة النساء تجاوزت 50% في كل تدريب.</p><p>اعتُمدت عملية الاختيار على مرحلتين: فرز أهلية، ثم تقييم نوعي بمقيّمَين لكل طلب وفق معايير موزونة تشمل الدافعية، والقدرة على قيادة مبادرة محلية، وملاءمة الخلفية، والعدالة الجغرافية بأولوية للمدن غير العواصم، والتوازن الجندري.</p>",
            IntroEn = "<p>An intensive full-day, in-person training that turns young people's climate awareness into practical initiatives, through a three-stage journey: understanding climate change and its challenges in the Arab region; mastering the search for opportunities, assessing fit and strategies to raise acceptance rates; and finally designing and pitching local environmental initiatives.</p><p>It targets, in particular, cities outside the capitals and the least-reached areas: 62% of trainings were delivered outside capital cities, and women exceeded 50% in every training.</p><p>Selection ran in two stages: an eligibility screen, then a qualitative assessment by two reviewers per application against weighted criteria — motivation, ability to lead a local initiative, relevance of background, geographic fairness with priority for non-capital cities, and gender balance.</p>",
            IntroFr = "<p>Une formation intensive d'une journée complète, en présentiel, qui transforme la conscience climatique des jeunes en initiatives concrètes, à travers un parcours en trois étapes : comprendre le changement climatique et ses défis dans la région arabe ; maîtriser la recherche d'opportunités, évaluer l'adéquation et les stratégies pour augmenter les taux d'admission ; et enfin concevoir et présenter des initiatives environnementales locales.</p><p>Le programme cible en particulier les villes hors des capitales et les zones les moins desservies : 62 % des formations ont eu lieu hors des capitales, et les femmes ont dépassé 50 % dans chaque formation.</p><p>La sélection s'est déroulée en deux étapes : un filtrage d'éligibilité, puis une évaluation qualitative par deux examinateurs par candidature selon des critères pondérés — motivation, capacité à mener une initiative locale, pertinence du parcours, équité géographique avec priorité aux villes hors capitales, et équilibre de genre.</p>",
            Stats = new List<ProgramStat>
            {
                Stat("3,165", "متقدمًا للبرنامج من 9 دول", "applicants from 9 countries", "candidats de 9 pays"),
                Stat("13", "تدريبًا في 12 مدينة", "trainings in 12 cities", "formations dans 12 villes"),
                Stat("7", "دول عربية", "Arab countries", "pays arabes"),
                Stat("62%", "من التدريبات خارج العواصم", "of trainings outside capital cities", "des formations hors des capitales"),
                Stat("50%+", "نسبة النساء في جميع التدريبات", "women in every training", "de femmes dans chaque formation"),
                Stat("195", "متدربًا في سوريا عبر 5 تدريبات", "trainees in Syria across 5 trainings", "stagiaires en Syrie sur 5 formations"),
                Stat("60+", "شريكًا لربط المبادرات بالتمويل", "partners to connect initiatives with funding", "partenaires pour relier les initiatives au financement"),
                Stat("12", "مدينة: القاهرة، الإسكندرية، بيروت، نابلس، الخليل، دمشق، حمص، طرطوس، حلب، صفاقس، عمّان، الجزائر", "cities: Cairo, Alexandria, Beirut, Nablus, Hebron, Damascus, Homs, Tartous, Aleppo, Sfax, Amman, Algiers", "villes : Le Caire, Alexandrie, Beyrouth, Naplouse, Hébron, Damas, Homs, Tartous, Alep, Sfax, Amman, Alger")
            },
            Timeline = new List<ProgramMilestone>
            {
                Step("01", "فهم التغيّر المناخي وتحدياته في المنطقة العربية", "Understanding climate change and its challenges in the Arab region", "Comprendre le changement climatique et ses défis dans la région arabe"),
                Step("02", "إتقان البحث عن الفرص وتقييم ملاءمتها واستراتيجيات رفع نسب القبول", "Mastering the search for opportunities, assessing fit and strategies to raise acceptance rates", "Maîtriser la recherche d'opportunités, évaluer l'adéquation et augmenter les taux d'admission"),
                Step("03", "تصميم مبادرات بيئية محلية وعرضها أمام لجنة التقييم", "Designing and pitching local environmental initiatives to the panel", "Concevoir et présenter des initiatives environnementales locales devant le jury")
            },
            OutcomesHeadingAr = "ما بعد التدريب",
            OutcomesHeadingEn = "After the training",
            OutcomesHeadingFr = "Après la formation",
            OutcomesAr = "<ul><li>تصوير عروض الفرق ونشرها عبر منصاتنا.</li><li>اختيار أفضل 3 مبادرات في كل دولة ونشرها على الموقع.</li><li>ربط المبادرات المتميزة بشبكة أكثر من 60 شريكًا لبحث فرص التطوير والتمويل.</li></ul>",
            OutcomesEn = "<ul><li>Team pitches are filmed and published on our channels.</li><li>The best three initiatives in each country are selected and published on the website.</li><li>Outstanding initiatives are connected to a network of more than 60 partners to explore development and funding.</li></ul>",
            OutcomesFr = "<ul><li>Les présentations des équipes sont filmées et publiées sur nos canaux.</li><li>Les trois meilleures initiatives de chaque pays sont sélectionnées et publiées sur le site.</li><li>Les initiatives remarquables sont mises en relation avec un réseau de plus de 60 partenaires pour explorer le développement et le financement.</li></ul>",
            PartnersAr = "بتمويل مؤسسة آنا ليند · شركاء استضافة وتنفيذ محليون في 7 دول · شبكة 60+ شريكًا لتمويل مبادرات الخريجين",
            PartnersEn = "Funded by the Anna Lindh Foundation · local hosting and delivery partners in 7 countries · a 60+ partner network to fund alumni initiatives",
            PartnersFr = "Financé par la Fondation Anna Lindh · partenaires locaux d'accueil et de mise en œuvre dans 7 pays · un réseau de 60+ partenaires pour financer les initiatives des alumni",
            HeroImagePath = "/images/programs/youth-empowerment-hero.jpg",
            Gallery = new List<string> { "/images/programs/youth-empowerment-2.jpg", "/images/programs/youth-empowerment-3.jpg" }
        };
    }
}
