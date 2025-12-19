using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class Feedback
    {
        public int Id { get; set; }
        public string? Name { get; set; } = "";
        public string? Email { get; set; } = "";
        public int Rating { get; set; } = 0; // Assuming rating is an integer value, e.g., 1 to 5
        public int CompanyId { get; set; }
        public Company Company { get; set; }

        // Link to the RequestInformation (optional)
        public int? RequestId { get; set; }
        public RequestInformation? RequestInformation { get; set; }

        public string? Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property for related entities, if any
        // public ICollection<RelatedEntity> RelatedEntities { get; set; } = new List<RelatedEntity>();
    }
}
