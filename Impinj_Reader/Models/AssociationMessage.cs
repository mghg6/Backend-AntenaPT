namespace Impinj_Reader.Models
{
    
        public class AssociationMessage
        {
            public string Tarima { get; set; } // EPC de la tarima
            public string Operador { get; set; } // EPC del operador
            public DateTime Timestamp { get; set; } // Fecha y hora del mensaje
        }
    
}
