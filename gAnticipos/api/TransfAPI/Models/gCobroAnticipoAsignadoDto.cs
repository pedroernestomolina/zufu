namespace TransfAPI.Models
{
    public class gCobroAnticipoAsignadoDto
    {
        public int id { get; set; } = 0;
        public string idMedio { get; set; } = string.Empty;
        public string codigoMedio { get; set; } = string.Empty;
        public string descMedio { get; set; } = string.Empty;
        public decimal montoRecibido { get; set; } = 0m;
        public DateTime fechaGestion { get; set; }
        public string numeroReferencia { get; set; } = "";
        public string estatusComprometida { get; set; } = "";
        public string docNumeroAsig { get; set; } = "";
        public string entidadAsig { get; set; } = "";
        public string ciRifEntidadAsig { get; set; } = "";
        public string idCxcRecibo { get; set; } = "";
    }
}