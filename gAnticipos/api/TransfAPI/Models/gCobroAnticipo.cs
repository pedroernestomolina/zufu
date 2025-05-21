using System.ComponentModel.DataAnnotations;

namespace TransfAPI.Models
{
    public class gCobroAnticipo
    {
        [Key]
        public int id {get;set;}
        public string id_medio_cobro  { get; set; }="";
        public string codigo_medio_cobro  { get; set; }="";
        public string desc_medio_cobro  { get; set; }="";
        public decimal monto_recibido  { get; set; }
        public DateTime fecha_gestion  { get; set; }
        public string numero_referencia { get; set; }="";
        public DateTime fecha_registro  { get; set; }
        public string estatus_comprometido { get; set; }="";
        public string estatus_anulado { get; set; }="";
    }
}