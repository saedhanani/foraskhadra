using Foras_Khadra.Models;

namespace Foras_Khadra.Helpers
{
    /// <summary>صفحة نوع واحد (/opportunities/{slug}): عنوان ومقدمة ووصف لكل لغة — المرحلة 2.1.</summary>
    public sealed record TypePage(OpportunityType Type, string Slug,
        string TitleAr, string TitleEn, string TitleFr,
        string IntroAr, string IntroEn, string IntroFr,
        string MetaAr, string MetaEn, string MetaFr)
    {
        public string Title(string lang) => lang == "en" ? TitleEn : lang == "fr" ? TitleFr : TitleAr;
        public string Intro(string lang) => lang == "en" ? IntroEn : lang == "fr" ? IntroFr : IntroAr;
        public string Meta(string lang) => lang == "en" ? MetaEn : lang == "fr" ? MetaFr : MetaAr;
    }

    public static class OpportunityTypePages
    {
        public static readonly IReadOnlyList<TypePage> All = new[]
        {
            new TypePage(OpportunityType.Scholarships, "scholarships",
                "منح دراسية لشباب الوطن العربي", "Scholarships for youth in the Arab world", "Bourses d'études pour les jeunes du monde arabe",
                "منح البكالوريوس والماستر والدكتوراه المفتوحة الآن لشباب الوطن العربي: ممولة بالكامل أو جزئيًا، في ألمانيا وتركيا وبريطانيا وكندا واليابان وفرنسا وغيرها. كل منحة مكتوبة بثلاث لغات مع شروط الأهلية (الدول المقبولة، المرحلة، شهادة اللغة) والموعد النهائي ورابط التقديم الرسمي. رشّح بدولتك ومرحلتك ومجالك، أو جرّب «بوصلة» لترى المنح المؤهَّل لها أنت تحديدًا.",
                "Bachelor's, master's and PhD scholarships open right now to youth in the Arab world: fully or partially funded, in Germany, Türkiye, the UK, Canada, Japan, France and beyond. Every scholarship is written in three languages with its eligibility criteria (accepted countries, level, language test), deadline and official application link. Filter by your country, level and field — or try the Compass to see the ones you personally qualify for.",
                "Bourses de licence, master et doctorat ouvertes maintenant aux jeunes du monde arabe : entièrement ou partiellement financées, en Allemagne, Turquie, Royaume-Uni, Canada, Japon, France et ailleurs. Chaque bourse est rédigée en trois langues avec ses critères d'éligibilité (pays acceptés, niveau, test de langue), sa date limite et le lien officiel. Filtrez par pays, niveau et domaine, ou essayez la Boussole.",
                "منح دراسية مفتوحة الآن لشباب الوطن العربي: بكالوريوس وماستر ودكتوراه، ممولة بالكامل، بشروط الأهلية والموعد ورابط التقديم.",
                "Open scholarships for youth in the Arab world: bachelor's, master's and PhD, fully funded, with eligibility, deadline and official link.",
                "Bourses ouvertes aux jeunes du monde arabe : licence, master, doctorat, entièrement financées, avec éligibilité, date limite et lien officiel."),
            new TypePage(OpportunityType.Fellowships, "fellowships",
                "زمالات للشباب في البيئة والمناخ والتنمية", "Fellowships in environment, climate and development", "Fellowships en environnement, climat et développement",
                "زمالات بحثية ومهنية وقيادية للشباب من الوطن العربي: برامج ممولة لعدة أشهر أو سنة في المناخ والطاقة والمياه والسياسات والتنمية، لدى الجامعات والمنظمات الدولية ومراكز الأبحاث. نعرض لكل زمالة من تقبل (الدول، المرحلة، سنوات الخبرة) وما تقدّمه وموعدها، ونخبرك حين تفتح دورتها القادمة إن كانت مغلقة الآن.",
                "Research, professional and leadership fellowships for youth from the Arab world: funded programs of a few months to a year in climate, energy, water, policy and development, hosted by universities, international organisations and think tanks. For each fellowship we show who can apply (countries, level, years of experience), what it offers and its deadline — and we tell you when the next cycle opens if it is closed now.",
                "Fellowships de recherche, professionnels et de leadership pour les jeunes du monde arabe : programmes financés de quelques mois à un an en climat, énergie, eau, politiques publiques et développement, auprès d'universités, d'organisations internationales et de centres de recherche. Pour chaque fellowship : qui peut postuler, ce qu'il offre, sa date limite — et une alerte à l'ouverture du prochain cycle.",
                "زمالات ممولة للشباب من الوطن العربي في المناخ والبيئة والتنمية: الشروط والمواعيد ورابط التقديم، وتنبيه حين تفتح الدورة القادمة.",
                "Funded fellowships for youth from the Arab world in climate, environment and development: criteria, deadlines, official link, next-cycle alerts.",
                "Fellowships financés pour les jeunes du monde arabe en climat, environnement et développement : critères, dates limites, lien officiel."),
            new TypePage(OpportunityType.Jobs, "jobs",
                "وظائف خضراء للشباب في الوطن العربي", "Green jobs for youth in the Arab world", "Emplois verts pour les jeunes du monde arabe",
                "وظائف في البيئة والطاقة المتجددة والمياه والاستدامة والعمل المناخي، في الوطن العربي وعن بُعد ومع المنظمات الدولية: وظائف للخريجين الجدد وللمهنيين، بدوام كامل أو جزئي أو عقود. كل وظيفة تحمل الجهة الموظِّفة وبلد العمل ونوع الدوام والموعد النهائي ورابط التقديم المباشر، ويمكن التقديم عليها بلا حساب. رشّح ببلدك أو بمجالك أو اختر «عن بُعد فقط».",
                "Jobs in environment, renewable energy, water, sustainability and climate action — across the Arab world, remotely and with international organisations: roles for fresh graduates and for professionals, full-time, part-time or contract. Every job shows the hiring organisation, country, employment type, deadline and a direct application link, and you can apply without an account. Filter by your country or field, or pick remote only.",
                "Emplois dans l'environnement, les énergies renouvelables, l'eau, la durabilité et l'action climatique — dans le monde arabe, à distance et auprès d'organisations internationales : postes pour jeunes diplômés et professionnels, à temps plein, partiel ou en contrat. Chaque offre indique l'employeur, le pays, le type de contrat, la date limite et un lien de candidature direct, sans compte. Filtrez par pays, domaine ou à distance.",
                "وظائف خضراء مفتوحة الآن في الوطن العربي وعن بُعد: البيئة والطاقة والمياه والمناخ، بالجهة الموظِّفة والموعد ورابط التقديم المباشر.",
                "Open green jobs in the Arab world and remote: environment, energy, water and climate, with employer, deadline and a direct apply link.",
                "Emplois verts ouverts dans le monde arabe et à distance : environnement, énergie, eau et climat, avec employeur, date limite et lien direct."),
            new TypePage(OpportunityType.Internships, "internships",
                "فرص تدريب للطلاب والخريجين", "Internships for students and graduates", "Stages pour étudiants et jeunes diplômés",
                "فرص تدريب عملي وبرامج تدريبية للطلاب والخريجين الجدد من الوطن العربي: في المنظمات الدولية والشركات والجامعات، حضوريًا أو عن بُعد، بأجر أو بلا أجر مع شهادة. نوضّح لكل فرصة من تقبل (الدول والمرحلة الدراسية) ومدتها ومكانها وموعدها ورابط التقديم المباشر، ويمكن التقديم عليها بلا حساب.",
                "Internships and training programmes for students and fresh graduates from the Arab world: with international organisations, companies and universities, on site or remote, paid or unpaid with a certificate. For each one we show who can apply (countries and level), duration, location, deadline and a direct application link — no account needed to apply.",
                "Stages et programmes de formation pour étudiants et jeunes diplômés du monde arabe : organisations internationales, entreprises et universités, sur place ou à distance, rémunérés ou non avec attestation. Pour chaque offre : qui peut postuler (pays et niveau), durée, lieu, date limite et lien direct — sans compte.",
                "فرص تدريب مفتوحة للطلاب والخريجين من الوطن العربي: حضوري وعن بُعد، بأجر أو بشهادة، بالشروط والموعد ورابط التقديم المباشر.",
                "Open internships for students and graduates from the Arab world: on site and remote, paid or with a certificate, with criteria, deadline and direct link.",
                "Stages ouverts aux étudiants et diplômés du monde arabe : sur place et à distance, rémunérés ou avec attestation, critères, date limite, lien direct."),
            new TypePage(OpportunityType.Volunteering, "volunteering",
                "فرص تطوع في البيئة والمناخ", "Volunteering in environment and climate", "Bénévolat en environnement et climat",
                "فرص تطوع محلية ودولية وعن بُعد للشباب من الوطن العربي: حملات بيئية، مؤتمرات مناخية، مشاريع مجتمعية، وبرامج تطوع ممولة تغطي السفر والإقامة. نعرض لكل فرصة الجهة والمكان والمدة وما تغطيه وموعدها ورابط التسجيل المباشر، ويمكن التقديم عليها بلا حساب. التطوع أسرع طريق لخبرة تُكتب في سيرتك وتفتح منحًا وزمالات لاحقًا.",
                "Local, international and remote volunteering for youth from the Arab world: environmental campaigns, climate conferences, community projects and funded volunteering programmes that cover travel and accommodation. Each listing shows the organisation, place, duration, what is covered, the deadline and a direct sign-up link — no account needed. Volunteering is the fastest way to experience you can put on your CV, and it opens scholarships and fellowships later.",
                "Bénévolat local, international et à distance pour les jeunes du monde arabe : campagnes environnementales, conférences climat, projets communautaires et programmes financés couvrant voyage et hébergement. Chaque offre indique l'organisation, le lieu, la durée, ce qui est pris en charge, la date limite et un lien direct — sans compte. Le bénévolat est le chemin le plus rapide vers une expérience valorisable.",
                "فرص تطوع مفتوحة للشباب من الوطن العربي: محلية ودولية وعن بُعد، ممولة أو بشهادة، بالجهة والمدة والموعد ورابط التسجيل.",
                "Open volunteering for youth from the Arab world: local, international and remote, funded or with a certificate, with organisation, duration, deadline and link.",
                "Bénévolat ouvert aux jeunes du monde arabe : local, international et à distance, financé ou avec attestation, organisation, durée, date limite, lien."),
            new TypePage(OpportunityType.Conferences, "conferences",
                "مؤتمرات وورش ودورات للشباب", "Conferences, workshops and courses for youth", "Conférences, ateliers et formations pour les jeunes",
                "مؤتمرات وقمم شبابية وورش تدريبية ودورات في المناخ والبيئة والقيادة والتنمية: ممولة بالكامل (سفر وإقامة) أو إلكترونية مجانية. نعرض لكل حدث تاريخه ومكانه ومن يُقبل وما يغطيه التمويل والموعد النهائي للتسجيل ورابط التقديم الرسمي. اختر «عن بُعد» للدورات والمؤتمرات الإلكترونية، أو رشّح ببلد الاستضافة.",
                "Youth conferences and summits, training workshops and courses in climate, environment, leadership and development: fully funded (travel and accommodation) or free online. For each event we show its date and venue, who is accepted, what the funding covers, the registration deadline and the official link. Choose remote for online courses and conferences, or filter by host country.",
                "Conférences et sommets jeunesse, ateliers de formation et cours en climat, environnement, leadership et développement : entièrement financés (voyage et hébergement) ou gratuits en ligne. Pour chaque événement : date et lieu, qui est accepté, ce que couvre le financement, la date limite d'inscription et le lien officiel. Choisissez « à distance » pour les événements en ligne.",
                "مؤتمرات وورش ودورات ممولة بالكامل أو إلكترونية للشباب من الوطن العربي في المناخ والبيئة والقيادة: التاريخ والمكان والتمويل ورابط التسجيل.",
                "Fully funded or online conferences, workshops and courses for youth from the Arab world in climate, environment and leadership.",
                "Conférences, ateliers et formations financés ou en ligne pour les jeunes du monde arabe en climat, environnement et leadership."),
            new TypePage(OpportunityType.Competitions, "competitions",
                "مسابقات وجوائز للشباب", "Competitions and awards for youth", "Concours et prix pour les jeunes",
                "مسابقات أفكار ومشاريع، جوائز، وبرامج مسرّعات للشباب من الوطن العربي في البيئة والمناخ والابتكار وريادة الأعمال: جوائز مالية، تمويل، وتدريب. نعرض لكل مسابقة شروط المشاركة (العمر، الدول، الفريق أو الفرد) وما تقدّمه وموعد التقديم ورابطه الرسمي، وقائمة الأوراق التي تجهّزها (ملخص الفكرة، العرض التقديمي، تشكيل الفريق).",
                "Idea and project competitions, awards and accelerator programmes for youth from the Arab world in environment, climate, innovation and entrepreneurship: cash prizes, funding and training. For each competition we show the entry conditions (age, countries, team or individual), what it offers, the deadline and the official link, plus the documents to prepare (idea summary, pitch deck, team).",
                "Concours d'idées et de projets, prix et programmes d'accélération pour les jeunes du monde arabe en environnement, climat, innovation et entrepreneuriat : prix en argent, financement et formation. Pour chaque concours : conditions (âge, pays, équipe ou individuel), ce qu'il offre, la date limite, le lien officiel et les documents à préparer.",
                "مسابقات وجوائز ومسرّعات مفتوحة للشباب من الوطن العربي في البيئة والمناخ والابتكار: الشروط والجوائز والموعد ورابط التقديم.",
                "Open competitions, awards and accelerators for youth from the Arab world in environment, climate and innovation: conditions, prizes, deadline, link.",
                "Concours, prix et accélérateurs ouverts aux jeunes du monde arabe en environnement, climat et innovation : conditions, prix, date limite, lien."),
        };

        private static readonly Dictionary<string, TypePage> BySlugMap = All.ToDictionary(p => p.Slug, StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<OpportunityType, TypePage> ByTypeMap = All.ToDictionary(p => p.Type);

        /// <summary>يقبل أيضًا الأسماء القديمة (trainings، jobs بأحرف كبيرة…).</summary>
        public static TypePage? BySlug(string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            var s = slug.Trim().ToLowerInvariant();
            if (s == "trainings" || s == "training") s = "internships";
            if (BySlugMap.TryGetValue(s, out var page)) return page;
            return Enum.TryParse<OpportunityType>(s, true, out var type) ? For(type) : null;
        }

        public static TypePage For(OpportunityType type) => ByTypeMap[type];
    }
}
