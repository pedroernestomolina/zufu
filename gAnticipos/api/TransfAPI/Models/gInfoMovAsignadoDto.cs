namespace TransfAPI.Models
{
    public class gInfoMovAsignadoDto
    {
        public DateTime fechaAsig { get; set; }
        public string ciRifEntidadAsig { get; set; }="";
        public string nombreEntidadAsig { get; set; }="";
        public string direccionEntidadAsig { get; set; }="";
        public decimal comisionAsig { get; set; }=0m;   
        public string docNumeroAsig { get; set; }="";
        public decimal montoConComisionAsig { get; set; }=0m;
        public decimal montoSinComisionAsig { get {return calcMonto(); } }
        //
        private decimal calcMonto()
        {
            decimal monto = 0;
            if (comisionAsig > 0)
            {
                monto = (montoConComisionAsig *100m) / (100m-comisionAsig);
            }
            else
            {
                monto = montoConComisionAsig;
            }
            return monto;
        }
    }
}