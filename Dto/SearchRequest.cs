using System.ComponentModel.DataAnnotations;

namespace Flappr.Dto
{
    // Arama isteği DTO'su (formdan gelen veriler)
    public class SearchRequest
    {
        [Required]
        public string SearchTerm { get; set; }
        public List<SearchResponse> Sonuc { get; set; } = new List<SearchResponse>();

    }

    // Arama sonucu DTO'su (view veya API için döndürülecek veriler)
    public class SearchResponse
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
    }
}
