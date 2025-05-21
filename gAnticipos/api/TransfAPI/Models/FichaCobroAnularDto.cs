namespace TransfAPI.Models
{
    public class FichaCobroAnularDto
    {
        public int idAnticipo { get; set; }
        public List<DataAnular> Asignaciones {get;set;} = new List<DataAnular>();
    }

    public class DataAnular
    {
        public string idCxcRecibo { get; set; }="";
        public string idCxcPago  { get; set; }="";
        public string idCliente { get; set; }="";
        public decimal montoAnticipoRestarMonDiv { get; set; }=0m;
    }
}