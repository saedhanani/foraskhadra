-- توحيد المصطلح: "شباب الوطن العربي" بدل "الشباب العربي" في اسم برنامج بناء القدرات المخزَّن في قاعدة الإنتاج.
-- آمن للتكرار، بلا EXEC. لا هجرة EF (بيانات فقط). يمكن تنفيذه قبل نشر الكود أو بعده.
UPDATE FlagshipPrograms SET MenuTitleAr = N'برنامج فرص خضراء لبناء قدرات شباب الوطن العربي' WHERE Slug = 'youth-capacity-building' AND MenuTitleAr = N'برنامج فرص خضراء لبناء قدرات الشباب العربي';
UPDATE FlagshipPrograms SET TitleAr = N'برنامج فرص خضراء لبناء قدرات شباب الوطن العربي' WHERE Slug = 'youth-capacity-building' AND TitleAr = N'برنامج فرص خضراء لبناء قدرات الشباب العربي';
UPDATE FlagshipPrograms SET MenuTitleEn = N'Arab World Youth Capacity-Building Programme' WHERE Slug = 'youth-capacity-building' AND MenuTitleEn = N'Arab Youth Capacity-Building Programme';
UPDATE FlagshipPrograms SET TitleEn = N'Foras Khadra Capacity-Building Programme for Youth in the Arab World' WHERE Slug = 'youth-capacity-building' AND TitleEn = N'Foras Khadra Arab Youth Capacity-Building Programme';
UPDATE FlagshipPrograms SET TitleFr = N'Programme Foras Khadra de renforcement des capacités des jeunes du monde arabe' WHERE Slug = 'youth-capacity-building' AND TitleFr = N'Programme Foras Khadra de renforcement des capacités des jeunes arabes';
SELECT Slug, MenuTitleAr, TitleAr, TitleEn, TitleFr FROM FlagshipPrograms WHERE Slug = 'youth-capacity-building';
