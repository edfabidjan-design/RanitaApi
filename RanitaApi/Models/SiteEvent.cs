using System.ComponentModel.DataAnnotations.Schema;

namespace RanitaApi.Models
{
    public class SiteEvent
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#10b981";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string PromoText { get; set; } = "";
        public string SlideTitle { get; set; } = "";
        public string SlideSub { get; set; } = "";
        public string SlideCta { get; set; } = "Voir les offres →";
        public string SlideLink { get; set; } = "products.html";
        public string SlideDisc { get; set; } = "";
        public string SlideImg { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public bool IsCurrentlyActive =>
            IsActive &&
            (StartDate == null || StartDate.Value.Date <= DateTime.UtcNow.Date) &&
            (EndDate == null || EndDate.Value.Date >= DateTime.UtcNow.Date);
    }
}