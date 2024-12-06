using Impinj.OctaneSdk;
using Impinj_Reader.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Impinj_Reader.Services
{
    public class ReaderService
    {
        private readonly ReaderSettings _readerSettingsService;
        private readonly ImpinjReader _reader;
        private readonly HashSet<string> _seenEpcs; // Para evitar duplicados
        private Dictionary<string, List<string>> _activeTarimas = new Dictionary<string, List<string>>();
        private readonly List<(string Tarima, string Operador)> _associations; // Log de asociaciones
        private readonly IHubContext<MessageHub> _hubContext; // SignalR
        private const int LogLevel = 2; // 0 = Nada, 1 = Básico, 2 = Detallado
        private const string ReaderHostname = "172.16.100.197"; // Ip del lector
        private Dictionary<string, DateTime> _pendingAssociations = new Dictionary<string, DateTime>();
        private readonly int _timeoutSeconds = 5; // Tiempo límite para completar la asociación


        public ReaderService(ReaderSettings readerSettingsService, IHubContext<MessageHub> hubContext)
        {
            _readerSettingsService = readerSettingsService ?? throw new ArgumentNullException(nameof(readerSettingsService));
            _reader = new ImpinjReader();
            _seenEpcs = new HashSet<string>();
            _associations = new List<(string, string)>();
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        public void StartReader()
        {
            try
            {
                Console.WriteLine($"Connecting to reader at {ReaderHostname}...");
                _reader.Connect(ReaderHostname);

                // Configurar ajustes del lector usando ReaderSettings
                _readerSettingsService.ConfigureReaderSettings(_reader);

                // Asignar eventos específicos
                _reader.TagsReported += OnTagsReported;
                _reader.KeepaliveReceived += OnKeepaliveReceived;
                _reader.ConnectionLost += OnConnectionLost;

                Console.WriteLine("Reader started and ready to read tags.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting reader: {ex.Message}");
                throw;
            }
        }

        public void StopReader()
        {
            try
            {
                _reader.Stop();
                _reader.Disconnect();
                Console.WriteLine("Reader stopped and disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping reader: {ex.Message}");
                throw;
            }
        }

        private async void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            foreach (Tag tag in report.Tags)
            {
                string epc = tag.Epc.ToString().Replace(" ", "");
                Log($"Procesando EPC: {epc}", 2);

                if (epc.Length == 16) // Tarima
                {
                    if (_seenEpcs.Add(epc))
                    {
                        // Si no existe una entrada para la tarima, crea una nueva
                        if (!_activeTarimas.ContainsKey(epc))
                        {
                            _activeTarimas[epc] = new List<string>();
                        }

                        // Agrega el EPC al grupo de tarimas activas
                        _activeTarimas[epc].Add(epc);
                        Log($"Tarima detectada y registrada: {epc}", 1);

                        _pendingAssociations[epc] = DateTime.UtcNow;
                        _ = CheckTimeout(epc); // Inicia el timeout
                    }
                    else
                    {
                        Log($"EPC duplicado ignorado: {epc}", 2);
                    }
                }
                else if (epc.Length == 12) // Pulsera
                {
                    Log($"Pulsera detectada: {epc}", 1);

                    // Asociar la pulsera a todas las tarimas activas
                    foreach (var tarima in _activeTarimas.Keys.ToList())
                    {
                        // Envía un mensaje por SignalR para cada EPC de la tarima
                        foreach (var tarimaEpc in _activeTarimas[tarima])
                        {
                            Log($"Asociación completada: Tarima {tarimaEpc} con Operador {epc}", 1);
                            await _hubContext.Clients.All.SendAsync("sendMessage", new
                            {
                                Type = "Asociación",
                                Tarima = tarimaEpc,
                                Operador = epc,
                                Timestamp = DateTime.UtcNow
                            });
                            Console.WriteLine($"Mensaje enviado a SignalR: {{ Type: Asociación, Tarima: {tarimaEpc}, Operador: {epc}, Timestamp: {DateTime.UtcNow} }}");
                        }

                        // Limpia las asociaciones pendientes
                        _activeTarimas.Remove(tarima);
                        _pendingAssociations.Remove(tarima);
                        Log($"Estado de Tarima {tarima} reiniciado.", 2);
                    }
                }
                else
                {
                    Log($"EPC desconocido detectado: {epc}", 1);
                }
            }
        }


        private async Task CheckTimeout(string epc)
        {
            await Task.Delay(_timeoutSeconds * 1000); // Espera el tiempo definido

            // Verifica si el EPC sigue pendiente después del timeout
            if (_pendingAssociations.ContainsKey(epc) && DateTime.UtcNow.Subtract(_pendingAssociations[epc]).TotalSeconds >= _timeoutSeconds)
            {
                Log($"Timeout alcanzado para EPC: {epc}. Eliminando de asociaciones pendientes.", 2);

                // Si es una tarima, notificar por SignalR como incompleta
                if (_activeTarimas.ContainsKey(epc))
                {
                    Log($"Timeout: Tarima {epc} no se asoció con un operador.", 1);

                    // Enviar mensaje por SignalR
                    await _hubContext.Clients.All.SendAsync("sendMessage", new
                    {
                        Type = "Asociación Incompleta",
                        Tarima = epc,
                        Operador = "Indefinido",
                        Timestamp = DateTime.UtcNow
                    });

                    // Limpiar el EPC de las asociaciones pendientes
                    _activeTarimas.Remove(epc);
                }

                // Eliminar del diccionario de asociaciones pendientes
                _pendingAssociations.Remove(epc);
            }
        }






        private void Log(string message, int level = 1)
        {
            if (level <= LogLevel)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }
        }



        //private void AssociateTarimaWithOperator(string tarima, string operador)
        //{
        //    // Guardar la asociación
        //    _associations.Add((tarima, operador));
        //    Console.WriteLine($"Asociación completada: Tarima {tarima} con Operador {operador}");

        //    // Aquí podrías emitir un evento o llamar a un servicio para notificar al frontend
        //}

        private void OnKeepaliveReceived(ImpinjReader reader)
        {
            Console.WriteLine($"Keepalive received from reader at {reader.Address}");
        }

        private void OnConnectionLost(ImpinjReader reader)
        {
            Console.WriteLine($"Connection lost to reader at {reader.Address}. Reconnecting...");
            StartReader();
        }
    }
}