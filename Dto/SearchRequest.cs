using System.ComponentModel.DataAnnotations;

namespace Flappr.Dto
{
    // Arama isteği DTO'su (formdan gelen veriler)
    public class SearchRequest
    {
        [Required]
        public string SearchTerm { get; set; }
    }

    // Arama sonucu DTO'su (view veya API için döndürülecek veriler)
    public class SearchResult
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
    }

    // Opsiyonel: birden fazla sonucu paketlemek için
    public class SearchResponse
    {
        public List<SearchResult> Sonuc { get; set; } = new List<SearchResult>();
    }
}
