namespace TransfAPI.Models
{
    public class FichaCobroDto
    {
        public  string idCobrador{ get; set; } ="0000000001";
        public string codigoCobrador { get; set; } = "";
        public string nombreCobrador { get; set; } = "DIRECTO";
        public string idUsuario { get; set; } = "0000000001";
        public string nombreUsuario { get; set; } = "ADM";
        public string idVendedor { get; set; } = "0000000001";
        public string idAgencia { get; set; }= "0000000001";
        public string SucPrefijo { get; set; } = "01";
        public List<AsignarData> Asignaciones {get;set;} = new List<AsignarData>();
        public int idGCobroAnticipo {get;set;}
    }

    public class AsignarData
    {
        public decimal factorCambio { get; set; }=0m;
        public decimal comision { get; set; }=0m;
        public ReciboData Recibo { get; set; } = new ReciboData();
        public List<MetodoPagoData> MetodosPago { get; set; } = new List<MetodoPagoData>();
    }

    public class ReciboData
    {
        public string idCliente { get; set; } = "";  
        public string nombreCliente { get; set; } = "";
        public string ciRifCliente { get; set; } = "";
        public string codigoCliente { get; set; } = "";
        public string direccionCliente { get; set; } = "";
        public string telefonoCliente { get; set; } = "";
        public decimal montoRecibidoMonDiv { get; set; }=0;
        public decimal montoAnticipoCargarMonDiv { get; set; }=0;
        public string Nota { get; set; } = "";      
    }

    public class MetodoPagoData
    {
        public string AutoMedioPago { get; set; } = "";
        public string Medio { get; set; } = "";
        public string Codigo { get; set; } = "";
        public decimal MontoRecibido { get; set; } = 0;
        public string Referencia { get; set; } = "";
        public DateTime OpFecha { get; set; } = DateTime.Now;
        public decimal OpMonto { get; set; } = 0;
        public decimal OpTasa { get; set; } = 0;
        public string OpAplicaConversion { get; set; } = "";
    }
}