namespace Foras_Khadra.Models
{
 
    //To save the mail setting from appSettting.json
    public class MailSettings
    {
        public int Id { get; set; }
        public string Mail { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

        /// <summary>صندوق استلام رسائل «تواصل معنا» (MailSettings:ContactTo)؛ فارغ = info@foraskhadra.com.</summary>
        public string? ContactTo { get; set; }

        public const string DefaultContactTo = "info@foraskhadra.com";

        public string ContactInbox => string.IsNullOrWhiteSpace(ContactTo) ? DefaultContactTo : ContactTo.Trim();
    }
}
