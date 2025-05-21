namespace TransfAPI.Models
{
        public class FichaCobroConMultiplesMovDto
    {
        public string idCobrador { get; set; } = "0000000001";
        public string codigoCobrador { get; set; } = "";
        public string nombreCobrador { get; set; } = "DIRECTO";
        public string idUsuario { get; set; } = "0000000001";
        public string nombreUsuario { get; set; } = "ADM";
        public string idVendedor { get; set; } = "0000000001";
        public string idAgencia { get; set; } = "0000000001";
        public string SucPrefijo { get; set; } = "01";
        public List<AsignarData> Asignaciones { get; set; } = new List<AsignarData>();
    }
}