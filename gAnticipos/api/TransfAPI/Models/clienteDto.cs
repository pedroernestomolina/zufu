using System.ComponentModel.DataAnnotations;

namespace TransfAPI.Models
{
    public class clienteDto
    {
        public string idCliente { get; set; }="";
        public string codigoCliente { get; set; }="";
        public string ciRifCliente { get; set; }="";
        public string nombreRazonSocialCliente  { get; set; }="";
        public string dirFiscalCliente  { get; set; }="";
        public string estatusCredito { get; set; }="";
        public string estatusActivo { get; set; } ="";
    }
}