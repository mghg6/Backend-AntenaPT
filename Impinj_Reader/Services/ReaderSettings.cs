using Impinj.OctaneSdk;

namespace Impinj_Reader.Services
{
    public class ReaderSettings
    {
        public void ConfigureReaderSettings(ImpinjReader reader)
        {
            try
            {
                // Obtener la configuración predeterminada
                Settings settings = reader.QueryDefaultSettings();

                // Configuración de inicio y parada
                settings.AutoStart.Mode = AutoStartMode.Immediate;
                settings.AutoStop.Mode = AutoStopMode.None;

                // Incluir datos adicionales en los reportes
                settings.Report.IncludeFirstSeenTime = true;
                settings.Report.IncludeLastSeenTime = true;
                settings.Report.IncludeSeenCount = true;

                // Configuración de Keepalives
                settings.Keepalives.Enabled = true;
                settings.Keepalives.PeriodInMs = 5000;
                settings.Keepalives.EnableLinkMonitorMode = true;
                settings.Keepalives.LinkDownThreshold = 5;

                // Configurar cada antena (del 1 al 13)
                for (ushort i = 1; i <= 13; i++)
                {
                    AntennaConfig antennaConfig = settings.Antennas.GetAntenna(i);
                    antennaConfig.TxPowerInDbm = 28; // Potencia de transmisión en dBm
                    antennaConfig.RxSensitivityInDbm = -58; // Sensibilidad del receptor
                }

                // Aplicar y guardar configuración
                reader.ApplySettings(settings);
                reader.SaveSettings();

                Console.WriteLine("Configuración aplicada correctamente.");
            }
            catch (OctaneSdkException ex)
            {
                Console.WriteLine($"Error al aplicar configuración al lector: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado al configurar el lector: {ex.Message}");
                throw;
            }
        }
    }
}
