using System.ComponentModel.DataAnnotations;

namespace TransfAPI.Models
{
    public class empresaMedios
    {
        [Key]
        public string auto { get; set; }="";
        public string codigo { get; set; } = string.Empty;
        public string nombre { get; set; } = string.Empty;
        public string estatus_cobro_gAnticipo { get; set; } = string.Empty;
    }
}