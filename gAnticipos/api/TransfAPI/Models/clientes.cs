using System.ComponentModel.DataAnnotations;

namespace TransfAPI.Models
{
    public class clientes
    {
        [Key]
        public string auto { get; set; }="";
        public string codigo { get; set; }="";
        public string ci_rif { get; set; }="";
        public string razon_social { get; set; }="";
        public string dir_fiscal  { get; set; }="";
        public string estatus_credito { get; set; }="";
        public string estatus { get; set; } ="";
    }
}