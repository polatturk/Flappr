using System.ComponentModel.DataAnnotations;

namespace Flappr.Dto
{
    public class SearchRequest
    {
        [Required]
        public string SearchTerm { get; set; }
        public List<SearchResponse> Sonuc { get; set; } = new List<SearchResponse>();

    }

    public class SearchResponse
    {
        public Guid Id { get; set; }
        public string UserUsername { get; set; }
        public string UserNickname { get; set; }
    }
}
