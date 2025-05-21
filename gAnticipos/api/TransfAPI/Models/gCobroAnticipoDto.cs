namespace TransfAPI.Models
{
    public class gCobroAnticipoDto
    {
        public  int id { get; set; } = 0;
        public string idMedio { get; set; } = string.Empty;
        public string codigoMedio { get; set; } = string.Empty;
        public string descMedio  { get; set; } = string.Empty;
        public decimal montoRecibido { get; set; } =0m;
        public DateTime fechaGestion  { get; set; }
        public string numeroReferencia  { get; set; } ="";
        public string estatusComprometida { get; set; }="";
    }
}