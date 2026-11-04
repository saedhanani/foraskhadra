using Foras_Khadra.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace Foras_Khadra.Models
{
    public class AllOpportunitiesViewModel
    {
        public List<Opportunity> Opportunities { get; set; } = new();

        public string SelectedCountry { get; set; }
        public List<OpportunityType> SelectedTypes { get; set; } = new();
        // ⚡ إضافة هذه الخاصية للقائمة المنسدلة
        public List<string> Countries { get; set; } = new List<string>();

        // ترقيم الصفحات من الخادم: Opportunities تحمل صفحة واحدة فقط
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        // المرحلة 2.1/2.2: الاستعلام كما جاء، والقيم المفهومة منه، وصفحة النوع إن كانت هذه صفحة نوع، وخيارات الفلاتر
        public OpportunityQuery Query { get; set; } = new();
        public ParsedFilter Filter { get; set; } = new();
        public TypePage? TypePage { get; set; }
        public List<Field> Fields { get; set; } = new();
        /// <summary>"دولتي": الدول العربية برموز ISO بلغة الواجهة.</summary>
        public List<CountryOption> CountryOptions { get; set; } = new();
        /// <summary>بلدان الاستضافة الموجودة فعلًا في الفرص المنشورة.</summary>
        public List<CountryOption> HostOptions { get; set; } = new();
        /// <summary>صفحة نية (المرحلة 2.3) إن كانت هذه صفحة نية؛ Filter حينها هو الدمج (فلترها + الطلب) وExtraFilter هو الطلب وحده.</summary>
        public LandingPage? Landing { get; set; }
        public ParsedFilter ExtraFilter { get; set; } = new();
        public bool LandingIndexable { get; set; }

        // لتوليد قائمة الأنواع للـ dropdown
        public List<SelectListItem> TypeSelectList
        {
            get
            {
                var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                var list = new List<SelectListItem>();
                foreach (var t in Enum.GetValues(typeof(OpportunityType)))
                {
                    var type = (OpportunityType)t;
                    list.Add(new SelectListItem
                    {
                        Text = type.GetDisplayName(lang), // <-- تمرير اللغة هنا
                        Value = type.ToString(),
                        Selected = SelectedTypes.Contains(type)
                    });
                }
                return list;
            }
        }
    }
}
