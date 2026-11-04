using Foras_Khadra.Helpers;
using Foras_Khadra.Models;

namespace Foras_Khadra.Data
{
    /// <summary>
    /// الدفعة الأولى من صفحات النية (المرحلة 2.3): 22 دولة، الوطن العربي، ممولة بالكامل، بدون آيلتس، زمالات مناخية،
    /// منح الماستر والدكتوراه، تدريب عن بُعد، فرص للنساء، ومنح في ست دول مضيفة. تُستورد من /Admin/LandingPages (الموجود بالـslug لا يُمسّ)
    /// والمدير يحرّر النصوص بعدها.
    /// </summary>
    public static class LandingPageSeed
    {
        private static readonly string[] HostScholarshipIso = { "DE", "TR", "GB", "CA", "JP", "FR" };

        public static List<LandingPage> All(IReadOnlyCollection<Country> countries)
        {
            var pages = new List<LandingPage>();
            var order = 0;

            foreach (var c in countries.Where(c => c.IsoCode != null && CompassMatcher.ArabIso.Contains(c.IsoCode)).OrderBy(c => c.Id))
                pages.Add(Country(c, order++));

            pages.Add(Page("arab-world", order++, new OpportunityQuery { Country = FacetParser.ArabWorldCode },
                "فرص لشباب الوطن العربي: منح ووظائف وزمالات مفتوحة الآن", "Opportunities for youth in the Arab world, open now", "Opportunités pour les jeunes du monde arabe, ouvertes maintenant",
                "كل الفرص المفتوحة الآن لشباب الوطن العربي في مكان واحد: منح دراسية ممولة بالكامل، وظائف خضراء، زمالات، تدريب، تطوع، مؤتمرات ومسابقات. تُعرض هنا الفرص التي تقبل المتقدمين من أي دولة عربية (أو من العالم كله)، لا تلك المحصورة بدولة واحدة. كل فرصة مكتوبة بالعربية والإنجليزية والفرنسية مع شروط الأهلية والموعد النهائي ورابط التقديم الرسمي، ونحدّث القائمة يوميًا ونزيل ما انتهى. رشّح بالنوع أو المرحلة أو المجال أو التمويل، أو جرّب «بوصلة» لتعرف في دقيقتين ما تؤهَّل له أنت تحديدًا.",
                "Every opportunity open right now to youth across the Arab world, in one place: fully funded scholarships, green jobs, fellowships, internships, volunteering, conferences and competitions. This page lists opportunities that accept applicants from any Arab country (or from anywhere in the world), not those limited to a single country. Each one is written in Arabic, English and French with its eligibility criteria, deadline and official application link; we update the list daily and remove what has closed. Filter by type, level, field or funding, or try the Compass to see in two minutes what you personally qualify for.",
                "Toutes les opportunités ouvertes maintenant aux jeunes du monde arabe, au même endroit : bourses entièrement financées, emplois verts, fellowships, stages, bénévolat, conférences et concours. Cette page présente les opportunités qui acceptent les candidats de n'importe quel pays arabe (ou du monde entier), et non celles réservées à un seul pays. Chacune est rédigée en arabe, anglais et français avec ses critères, sa date limite et le lien officiel ; la liste est mise à jour chaque jour. Filtrez par type, niveau, domaine ou financement, ou essayez la Boussole.",
                DefaultFaqAr(), DefaultFaqEn(), DefaultFaqFr()));

            pages.Add(Page("fully-funded", order++, new OpportunityQuery { Funding = "FullyFunded" },
                "فرص ممولة بالكامل لشباب الوطن العربي", "Fully funded opportunities for youth in the Arab world", "Opportunités entièrement financées pour les jeunes du monde arabe",
                "منح وزمالات ومؤتمرات وبرامج تدريب تغطي كل التكاليف: الرسوم الدراسية أو رسوم المشاركة، السفر، الإقامة، وغالبًا راتبًا شهريًا أو مصروفًا. نصنّف الفرصة «ممولة بالكامل» فقط حين يذكر المصدر ذلك صراحةً، فما تراه هنا لا يحتاج منك سوى التقديم. كل فرصة بثلاث لغات مع الدول المقبولة والمرحلة المطلوبة وشهادة اللغة إن طُلبت والموعد النهائي ورابط التقديم الرسمي. المنافسة على الممولة بالكامل أشد، لذلك ابدأ مبكرًا، وجهّز أوراقك من قائمة «جاهزيتك» في بوصلتك.",
                "Scholarships, fellowships, conferences and training programmes that cover everything: tuition or participation fees, travel, accommodation and often a monthly stipend. We label an opportunity “fully funded” only when the source says so explicitly, so what you see here needs nothing from you but an application. Each one comes in three languages with accepted countries, required level, language test if any, deadline and the official link. Competition for fully funded opportunities is tougher, so start early and prepare your documents from the readiness list in your Compass.",
                "Bourses, fellowships, conférences et formations qui couvrent tout : frais de scolarité ou de participation, voyage, hébergement et souvent une allocation mensuelle. Nous n'étiquetons une opportunité « entièrement financée » que lorsque la source le dit explicitement. Chacune est en trois langues avec les pays acceptés, le niveau requis, le test de langue éventuel, la date limite et le lien officiel. La concurrence est plus forte : commencez tôt et préparez vos documents depuis la liste de préparation de votre Boussole.",
                new[] { ("ماذا يعني «ممولة بالكامل» هنا؟", "أن المصدر ذكر صراحةً تغطية الرسوم والسفر والإقامة (وغالبًا راتبًا). الفرص التي لم يذكر مصدرها نوع التمويل لا تظهر في هذه الصفحة."), ("هل أحتاج شهادة لغة؟", "ليس دائمًا. استعمل فلتر «بدون آيلتس» لترى الممولة بالكامل التي لا تطلب شهادة، أو جرّب البوصلة لتعرف أين تنقصك شهادة."), ("كيف أزيد فرصتي؟", "قدّم مبكرًا، واقرأ شروط الأهلية حرفيًا، وجهّز خطاب الدوافع ورسائل التوصية قبل الموعد بأسابيع. قائمة «جاهزيتك» في البوصلة تجمع أوراقك لكل التقديمات.") },
                new[] { ("What does “fully funded” mean here?", "The source explicitly covers fees, travel and accommodation (and usually a stipend). Opportunities whose source does not state the funding type are not shown on this page."), ("Do I need a language certificate?", "Not always. Use the “No IELTS” filter to see fully funded opportunities that require none, or try the Compass to see where a certificate is the only thing missing."), ("How do I improve my chances?", "Apply early, read the eligibility criteria literally, and prepare your motivation letter and recommendation letters weeks before the deadline. The readiness list in your Compass keeps your documents in one place.") },
                new[] { ("Que signifie « entièrement financée » ici ?", "La source couvre explicitement les frais, le voyage et l'hébergement (et souvent une allocation). Les opportunités sans type de financement précisé n'apparaissent pas ici."), ("Faut-il un certificat de langue ?", "Pas toujours. Utilisez le filtre « Sans IELTS », ou essayez la Boussole pour voir où un certificat est la seule chose qui manque."), ("Comment augmenter mes chances ?", "Postulez tôt, lisez les critères à la lettre et préparez lettre de motivation et recommandations des semaines à l'avance. La liste de préparation de la Boussole centralise vos documents.") }));

            pages.Add(Page("no-ielts", order++, new OpportunityQuery { NoIelts = true },
                "منح وفرص بدون آيلتس أو توفل", "Scholarships and opportunities without IELTS or TOEFL", "Bourses et opportunités sans IELTS ni TOEFL",
                "فرص مفتوحة الآن لا تطلب شهادة IELTS ولا TOEFL: منح تقبل خطاب إثبات اللغة من الجامعة أو مقابلة بدل الاختبار، وزمالات وتدريب وتطوع ومؤتمرات لا تشترط شهادة أصلًا. لا نضع الفرصة هنا إلا إذا قال مصدرها صراحةً إن الشهادة غير مطلوبة؛ الفرص التي لم تذكر اللغة لا تظهر، حتى لا تفاجأ لاحقًا. كل فرصة بثلاث لغات مع الدول المقبولة والمرحلة والموعد ورابط التقديم الرسمي. إن كانت شهادة اللغة هي العائق الوحيد أمامك، فهذه الصفحة هي نقطة البداية.",
                "Opportunities open now that require neither IELTS nor TOEFL: scholarships that accept a university proof-of-language letter or an interview instead of the test, plus fellowships, internships, volunteering and conferences that never ask for a certificate. An opportunity appears here only when its source explicitly says the test is not required; those that do not mention language at all are left out so you are not surprised later. Each one comes in three languages with accepted countries, level, deadline and the official link. If a language certificate is the only thing standing in your way, start here.",
                "Opportunités ouvertes maintenant qui n'exigent ni IELTS ni TOEFL : bourses acceptant une attestation de langue de l'université ou un entretien, et fellowships, stages, bénévolat et conférences sans certificat. Une opportunité n'apparaît ici que si sa source dit explicitement que le test n'est pas requis ; celles qui ne mentionnent pas la langue sont exclues. Chacune est en trois langues avec pays acceptés, niveau, date limite et lien officiel. Si le certificat de langue est votre seul obstacle, commencez ici.",
                new[] { ("هل يعني هذا أن اللغة غير مهمة؟", "لا. الشهادة غير مطلوبة، لكن أغلب هذه الفرص تدرَّس أو تُدار بالإنجليزية أو الفرنسية، وقد تطلب خطاب إثبات لغة من جامعتك أو مقابلة."), ("لماذا لا تظهر فرص كثيرة أعرف أنها بلا شهادة؟", "لأننا لا ندرج الفرصة هنا إلا إذا ذكر مصدرها صراحةً أن الشهادة غير مطلوبة. الفرص التي سكت مصدرها عن اللغة لا تُدرج حتى لا تكون المعلومة خاطئة."), ("هل يمكنني التقديم ثم إحضار الشهادة لاحقًا؟", "في بعض المنح نعم (الشهادة شرط للقبول النهائي لا للتقديم). اقرأ شروط الأهلية في صفحة كل فرصة.") },
                new[] { ("Does this mean language doesn't matter?", "No. The certificate is not required, but most of these are taught or run in English or French and may ask for a proof-of-language letter from your university or an interview."), ("Why don't I see opportunities I know need no test?", "Because we only list an opportunity here when its source explicitly says the test is not required. Sources that say nothing about language are left out so the information is never wrong."), ("Can I apply now and bring the certificate later?", "For some scholarships, yes — the certificate is a condition for final admission, not for applying. Read the eligibility section of each opportunity.") },
                new[] { ("La langue n'a-t-elle donc pas d'importance ?", "Si. Le certificat n'est pas requis, mais la plupart de ces opportunités se déroulent en anglais ou en français et peuvent demander une attestation de votre université ou un entretien."), ("Pourquoi certaines opportunités sans test n'apparaissent-elles pas ?", "Nous ne listons ici que celles dont la source dit explicitement que le test n'est pas requis. Les sources muettes sur la langue sont exclues."), ("Puis-je postuler puis fournir le certificat plus tard ?", "Pour certaines bourses, oui : le certificat conditionne l'admission finale, pas la candidature. Lisez les critères de chaque opportunité.") }));

            pages.Add(Page("climate-fellowships", order++, new OpportunityQuery { Type = new() { "Fellowships" }, Theme = "Climate" },
                "زمالات مناخية للشباب من الوطن العربي", "Climate fellowships for youth from the Arab world", "Fellowships climat pour les jeunes du monde arabe",
                "زمالات في العمل المناخي والسياسات المناخية والانتقال العادل والتكيّف: برامج ممولة لعدة أشهر أو سنة لدى المنظمات الدولية ومراكز الأبحاث والجامعات، للباحثين الشباب والناشطين والمهنيين من الوطن العربي. نعرض لكل زمالة من تقبل (الدول، المرحلة، الخبرة)، وما تقدّمه (راتب، سفر، إرشاد)، وموعدها ورابطها الرسمي، ونخبرك حين تفتح دورتها القادمة إن كانت مغلقة الآن. الزمالات المناخية هي أسرع طريق إلى مؤتمرات الأطراف وشبكات القيادة المناخية الدولية.",
                "Fellowships in climate action, climate policy, just transition and adaptation: funded programmes of a few months to a year with international organisations, think tanks and universities, for young researchers, activists and professionals from the Arab world. For each fellowship we show who can apply (countries, level, experience), what it offers (stipend, travel, mentoring), its deadline and official link — and we alert you when the next cycle opens if it is closed now. Climate fellowships are the fastest route into COP delegations and international climate leadership networks.",
                "Fellowships en action climatique, politiques climatiques, transition juste et adaptation : programmes financés de quelques mois à un an auprès d'organisations internationales, de centres de recherche et d'universités, pour jeunes chercheurs, militants et professionnels du monde arabe. Pour chaque fellowship : qui peut postuler, ce qu'il offre (allocation, voyage, mentorat), la date limite et le lien officiel, avec une alerte à l'ouverture du prochain cycle. Les fellowships climat ouvrent la voie aux délégations COP et aux réseaux internationaux.",
                DefaultFaqAr(), DefaultFaqEn(), DefaultFaqFr()));

            pages.Add(Page("masters-scholarships", order++, new OpportunityQuery { Type = new() { "Scholarships" }, Degree = "Master" },
                "منح الماستر لشباب الوطن العربي", "Master's scholarships for youth in the Arab world", "Bourses de master pour les jeunes du monde arabe",
                "منح الماجستير المفتوحة الآن لحَمَلة البكالوريوس من الوطن العربي: ممولة بالكامل أو جزئيًا، في ألمانيا وتركيا وبريطانيا وكندا واليابان وفرنسا وهولندا والسويد وغيرها، في البيئة والطاقة والمياه والهندسة والعلوم والإدارة والسياسات. نعرض لكل منحة الدول المقبولة وشهادة اللغة المطلوبة والموعد النهائي ورابط التقديم الرسمي. أغلب منح الماستر تفتح مرة في السنة بين أكتوبر ومارس، فابدأ التجهيز قبل الموعد بشهور: كشف الدرجات، خطاب الدوافع، رسالتا توصية، وشهادة اللغة إن طُلبت.",
                "Master's scholarships open now to bachelor's graduates from the Arab world: fully or partially funded, in Germany, Türkiye, the UK, Canada, Japan, France, the Netherlands, Sweden and beyond, in environment, energy, water, engineering, science, management and policy. For each scholarship we show accepted countries, the language test required, the deadline and the official link. Most master's scholarships open once a year between October and March, so start preparing months ahead: transcript, motivation letter, two recommendation letters and a language certificate if required.",
                "Bourses de master ouvertes maintenant aux titulaires d'une licence du monde arabe : entièrement ou partiellement financées, en Allemagne, Turquie, Royaume-Uni, Canada, Japon, France, Pays-Bas, Suède et ailleurs, en environnement, énergie, eau, ingénierie, sciences, gestion et politiques publiques. Pour chaque bourse : pays acceptés, test de langue requis, date limite et lien officiel. La plupart ouvrent une fois par an entre octobre et mars : préparez relevé de notes, lettre de motivation, deux recommandations et certificat de langue.",
                DefaultFaqAr(), DefaultFaqEn(), DefaultFaqFr()));

            pages.Add(Page("phd-scholarships", order++, new OpportunityQuery { Type = new() { "Scholarships" }, Degree = "PhD" },
                "منح الدكتوراه لشباب الوطن العربي", "PhD scholarships for youth in the Arab world", "Bourses de doctorat pour les jeunes du monde arabe",
                "منح الدكتوراه المفتوحة الآن لحَمَلة الماجستير من الوطن العربي: ممولة بالكامل مع راتب شهري في الغالب، في الجامعات ومراكز الأبحاث في أوروبا وآسيا وأمريكا الشمالية، في المناخ والطاقة والمياه والبيئة والهندسة والعلوم. نعرض لكل منحة الدول المقبولة والمجالات والموعد ورابط التقديم الرسمي. الدكتوراه تحتاج في الغالب مقترح بحث ومراسلة مشرف محتمل قبل التقديم، فابدأ بذلك مبكرًا، وتابع البرامج المتكررة لنخبرك حين تفتح دورتها.",
                "PhD scholarships open now to master's graduates from the Arab world: usually fully funded with a monthly stipend, at universities and research centres in Europe, Asia and North America, in climate, energy, water, environment, engineering and science. For each scholarship we show accepted countries, fields, deadline and the official link. A PhD usually needs a research proposal and contact with a potential supervisor before applying, so start that early — and follow recurring programmes so we can tell you when their next cycle opens.",
                "Bourses de doctorat ouvertes maintenant aux titulaires d'un master du monde arabe : généralement entièrement financées avec allocation mensuelle, dans des universités et centres de recherche d'Europe, d'Asie et d'Amérique du Nord, en climat, énergie, eau, environnement, ingénierie et sciences. Pour chaque bourse : pays acceptés, domaines, date limite et lien officiel. Un doctorat exige souvent un projet de recherche et un contact préalable avec un directeur : commencez tôt et suivez les programmes récurrents.",
                DefaultFaqAr(), DefaultFaqEn(), DefaultFaqFr()));

            pages.Add(Page("remote-internships", order++, new OpportunityQuery { Type = new() { "Internships" }, Remote = true },
                "تدريب عن بُعد للطلاب والخريجين", "Remote internships for students and graduates", "Stages à distance pour étudiants et diplômés",
                "فرص تدريب عن بُعد مفتوحة الآن للطلاب والخريجين من الوطن العربي: مع المنظمات الدولية والشركات الناشئة والجامعات، من بلدك وبساعات مرنة، بأجر أو بشهادة معتمدة. التدريب عن بُعد يفتح لك خبرة دولية بلا تأشيرة ولا تكاليف سفر، ويُحتسب في طلبات المنح والزمالات لاحقًا. نعرض لكل فرصة الجهة ومدتها وشروطها والموعد ورابط التقديم المباشر، ويمكن التقديم بلا حساب. رشّح بالمجال أو ببلد الجهة، واحفظ ما يعنيك لنذكّرك قبل موعده بأسبوع.",
                "Remote internships open now to students and graduates from the Arab world: with international organisations, start-ups and universities, from your own country and on flexible hours, paid or with a recognised certificate. A remote internship gives you international experience without a visa or travel costs, and it counts in scholarship and fellowship applications later. For each one we show the organisation, duration, conditions, deadline and a direct application link — no account needed.",
                "Stages à distance ouverts maintenant aux étudiants et diplômés du monde arabe : organisations internationales, start-ups et universités, depuis votre pays et à horaires flexibles, rémunérés ou avec attestation. Un stage à distance apporte une expérience internationale sans visa ni frais de voyage, valorisable ensuite pour les bourses et fellowships. Pour chaque offre : organisation, durée, conditions, date limite et lien direct, sans compte.",
                DefaultFaqAr(), DefaultFaqEn(), DefaultFaqFr()));

            pages.Add(Page("women", order++, new OpportunityQuery { Women = true },
                "فرص للنساء والفتيات في الوطن العربي", "Opportunities for women and girls in the Arab world", "Opportunités pour les femmes et les filles du monde arabe",
                "منح وزمالات وبرامج قيادة وتدريب مخصصة للنساء والفتيات من الوطن العربي: في العلوم والتكنولوجيا والهندسة والمناخ وريادة الأعمال والقيادة، من منظمات دولية وجامعات ومؤسسات تدعم مشاركة المرأة. نعرض هنا فقط الفرص التي ينص مصدرها على أنها للنساء، مع الدول المقبولة والمرحلة والتمويل والموعد ورابط التقديم الرسمي. إن كنتِ تبحثين عن فرص عامة أيضًا، فكل الفرص الأخرى مفتوحة لكِ كذلك؛ هذه الصفحة تجمع المخصص فقط.",
                "Scholarships, fellowships, leadership and training programmes reserved for women and girls from the Arab world: in science, technology, engineering, climate, entrepreneurship and leadership, from international organisations, universities and foundations that support women's participation. Only opportunities whose source states they are for women appear here, with accepted countries, level, funding, deadline and the official link. All other opportunities on the site are open to you too; this page gathers the dedicated ones only.",
                "Bourses, fellowships, programmes de leadership et de formation réservés aux femmes et aux filles du monde arabe : sciences, technologie, ingénierie, climat, entrepreneuriat et leadership, auprès d'organisations internationales, d'universités et de fondations. Seules les opportunités dont la source précise qu'elles sont destinées aux femmes apparaissent ici, avec pays acceptés, niveau, financement, date limite et lien officiel. Toutes les autres opportunités du site vous sont ouvertes aussi.",
                DefaultFaqAr(), DefaultFaqEn(), DefaultFaqFr()));

            foreach (var iso in HostScholarshipIso)
                pages.Add(HostScholarships(iso, order++));

            return pages;
        }

        private static LandingPage Country(Country c, int order)
        {
            var iso = c.IsoCode!.ToUpperInvariant();
            var slug = AsciiSlug(c.NameEn.Replace("The ", ""));
            return Page(slug, order, new OpportunityQuery { Country = iso },
                $"فرص للشباب في {c.NameAr}: منح ووظائف وزمالات", $"Opportunities for youth in {c.NameEn}: scholarships, jobs and fellowships", $"Opportunités pour les jeunes – {c.NameFr} : bourses, emplois et fellowships",
                $"كل الفرص المفتوحة الآن التي تقبل المتقدمين من {c.NameAr}: منح دراسية ممولة بالكامل، وظائف خضراء، زمالات، تدريب، تطوع، مؤتمرات ومسابقات. تُعرض هنا الفرص التي تذكر {c.NameAr} صراحةً في شروطها، إضافةً إلى المفتوحة لكل دول الوطن العربي أو للعالم كله، فلا تضيع عليك فرصة لأنها لم تذكر بلدك بالاسم. كل فرصة مكتوبة بثلاث لغات مع شروط الأهلية والموعد النهائي ورابط التقديم الرسمي، ونحدّث القائمة يوميًا ونزيل المنتهي. رشّح بالنوع أو المرحلة أو المجال، أو جرّب «بوصلة» لترى في دقيقتين ما تؤهَّل له أنت تحديدًا.",
                $"Every opportunity open right now that accepts applicants from {c.NameEn}: fully funded scholarships, green jobs, fellowships, internships, volunteering, conferences and competitions. This page lists opportunities that name {c.NameEn} explicitly in their criteria, plus those open to the whole Arab world or to everyone, so you never miss one just because it did not mention your country by name. Each one is written in three languages with eligibility criteria, deadline and the official application link; we update the list daily and remove what has closed. Filter by type, level or field, or try the Compass to see in two minutes what you personally qualify for.",
                $"Toutes les opportunités ouvertes maintenant qui acceptent les candidats de {c.NameFr} : bourses entièrement financées, emplois verts, fellowships, stages, bénévolat, conférences et concours. Cette page présente celles qui citent {c.NameFr} explicitement dans leurs critères, ainsi que celles ouvertes à tout le monde arabe ou au monde entier. Chacune est rédigée en trois langues avec ses critères, sa date limite et le lien officiel ; la liste est mise à jour chaque jour. Filtrez par type, niveau ou domaine, ou essayez la Boussole.",
                DefaultFaqAr(), DefaultFaqEn(), DefaultFaqFr());
        }

        private static LandingPage HostScholarships(string iso, int order)
        {
            var ar = CountryHelper.NameForIso(iso, "ar") ?? iso;
            var en = CountryHelper.NameForIso(iso, "en") ?? iso;
            var fr = CountryHelper.NameForIso(iso, "fr") ?? iso;
            var slug = "scholarships-in-" + AsciiSlug(en);
            return Page(slug, order, new OpportunityQuery { Type = new() { "Scholarships" }, Host = iso },
                $"منح دراسية في {ar} لشباب الوطن العربي", $"Scholarships in {en} for youth in the Arab world", $"Bourses d'études – {fr} – pour les jeunes du monde arabe",
                $"المنح الدراسية المفتوحة الآن للدراسة في {ar}، للطلاب من الوطن العربي: بكالوريوس وماستر ودكتوراه، ممولة بالكامل أو جزئيًا، من الحكومة والجامعات والمؤسسات. نعرض لكل منحة الدول المقبولة والمرحلة وشهادة اللغة المطلوبة وما يغطيه التمويل والموعد النهائي ورابط التقديم الرسمي، ونخبرك حين تفتح دورة البرامج المتكررة. اقرأ شروط كل منحة بدقة؛ أغلب منح {ar} تفتح مرة واحدة في السنة وتغلق قبل بداية العام الدراسي بشهور.",
                $"Scholarships open right now to study in {en}, for students from the Arab world: bachelor's, master's and PhD, fully or partially funded, from the government, universities and foundations. For each scholarship we show accepted countries, level, the language test required, what the funding covers, the deadline and the official link — and we tell you when recurring programmes open their next cycle. Read each scholarship's conditions carefully; most scholarships in {en} open once a year and close months before the academic year starts.",
                $"Bourses ouvertes maintenant pour étudier en {fr}, pour les étudiants du monde arabe : licence, master et doctorat, entièrement ou partiellement financées, par l'État, les universités et des fondations. Pour chaque bourse : pays acceptés, niveau, test de langue requis, couverture du financement, date limite et lien officiel, avec une alerte à l'ouverture des programmes récurrents. Lisez bien les conditions : la plupart ouvrent une fois par an et se clôturent des mois avant la rentrée.",
                DefaultFaqAr(), DefaultFaqEn(), DefaultFaqFr());
        }

        /// <summary>slug لاتيني فقط: يزيل علامات التشكيل اللاتينية (Türkiye → turkiye) ويحذف ما ليس ASCII.</summary>
        private static string AsciiSlug(string text)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var ch in text.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                if (ch < 128) sb.Append(ch);
            }
            return SlugHelper.Make(sb.ToString(), 40);
        }

        private static LandingPage Page(string slug, int order, OpportunityQuery filter, string titleAr, string titleEn, string titleFr, string introAr, string introEn, string introFr,
            (string Q, string A)[] faqAr, (string Q, string A)[] faqEn, (string Q, string A)[] faqFr)
        {
            var page = new LandingPage
            {
                Slug = slug, SortOrder = order, IsPublished = true,
                TitleAr = titleAr, TitleEn = titleEn, TitleFr = titleFr,
                IntroAr = introAr, IntroEn = introEn, IntroFr = introFr
            };
            page.SetQuery(filter);
            page.SetFaq(faqAr.Select(f => new FaqItem(f.Q, f.A)).ToList(), faqEn.Select(f => new FaqItem(f.Q, f.A)).ToList(), faqFr.Select(f => new FaqItem(f.Q, f.A)).ToList());
            return page;
        }

        private static (string, string)[] DefaultFaqAr() => new[]
        {
            ("كيف أعرف إن كنت مؤهَّلًا لفرصة هنا؟", "افتح الفرصة واقرأ قسم «شروط الأهلية»: الدول المقبولة والمرحلة والعمر وشهادة اللغة. أو أجب عن 8 أسئلة في «بوصلة» فنعرض لك فقط ما تؤهَّل له، مع سبب لكل فرصة."),
            ("هل أحتاج حسابًا للتقديم؟", "للوظائف والتدريب والتطوع لا؛ رابط التقديم ظاهر مباشرة. للمنح والزمالات والمؤتمرات والمسابقات نطلب حسابًا مجانيًا وتأكيد بريدك قبل زر «قدّم الآن»، لنذكّرك بالموعد ونتابع معك."),
            ("كم مرة تُحدَّث هذه الصفحة؟", "يوميًا. تُضاف الفرص الجديدة فور نشرها، وتظهر المنتهية في آخر القائمة بعلامة «منتهية» ثم تُزال. احفظ الفرصة لنذكّرك قبل موعدها بأسبوع.")
        };

        private static (string, string)[] DefaultFaqEn() => new[]
        {
            ("How do I know whether I qualify for an opportunity here?", "Open it and read the eligibility section: accepted countries, level, age and language test. Or answer 8 questions in the Compass and we show only what you qualify for, with a reason for each one."),
            ("Do I need an account to apply?", "Not for jobs, internships and volunteering — the apply link is right there. For scholarships, fellowships, conferences and competitions we ask for a free account and a confirmed email before “Apply now”, so we can remind you of the deadline and track your application with you."),
            ("How often is this page updated?", "Daily. New opportunities appear as soon as they are published; closed ones move to the end with an “expired” badge and are then removed. Save an opportunity to be reminded a week before its deadline.")
        };

        private static (string, string)[] DefaultFaqFr() => new[]
        {
            ("Comment savoir si je suis éligible à une opportunité ici ?", "Ouvrez-la et lisez la section « critères d'éligibilité » : pays acceptés, niveau, âge et test de langue. Ou répondez à 8 questions dans la Boussole : nous n'affichons que ce à quoi vous êtes éligible, avec une raison pour chaque opportunité."),
            ("Faut-il un compte pour postuler ?", "Pas pour les emplois, stages et bénévolat : le lien est direct. Pour les bourses, fellowships, conférences et concours, un compte gratuit et un e-mail confirmé sont demandés avant « Postuler », pour vous rappeler la date limite et suivre votre candidature."),
            ("À quelle fréquence cette page est-elle mise à jour ?", "Chaque jour. Les nouvelles opportunités apparaissent dès leur publication ; les clôturées passent en fin de liste avec la mention « expirée » puis sont retirées. Enregistrez une opportunité pour un rappel une semaine avant la date limite.")
        };
    }
}
