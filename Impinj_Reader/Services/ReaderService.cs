using System.Collections.Generic;
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
        private string _currentTarima; // EPC de la última tarima detectada
        private readonly List<(string Tarima, string Operador)> _associations; // Log de asociaciones
        private readonly IHubContext<MessageHub> _hubContext; // SignalR
        private const int LogLevel = 2; // 0 = Nada, 1 = Básico, 2 = Detallado
        private const string ReaderHostname = "172.16.100.198"; // Ip del lector

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
                        _currentTarima = epc;
                        Log($"Tarima detectada: {epc}", 1);
                        Log($"Estado actual de Tarima: {_currentTarima}", 2);
                    }
                    else
                    {
                        Log($"EPC duplicado ignorado: {epc}", 2);
                    }
                }
                else if (epc.Length == 12) // Pulsera
                {
                    Log($"Pulsera detectada: {epc}", 1);

                    if (_currentTarima != null)
                    {
                        // Realiza la asociación
                        Log($"Asociación completada: Tarima {_currentTarima} con Operador {epc}", 1);

                        Log($"Enviando mensaje a SignalR: Tarima {_currentTarima}, Operador {epc}", 1);
                        await _hubContext.Clients.All.SendAsync("sendMessage", new
                        {
                            Type = "Asociación",
                            Tarima = _currentTarima,
                            Operador = epc,
                            Timestamp = DateTime.UtcNow
                        });
                        Console.WriteLine($"Mensaje enviado a SignalR: {{ Type: Asociación, Tarima: {_currentTarima}, Operador: {epc}, Timestamp: {DateTime.UtcNow} }}");


                        // Reinicia el estado de tarima
                        _currentTarima = null;
                        Log("Estado de Tarima reiniciado.", 2);
                    }
                    else
                    {
                        Log($"Pulsera detectada sin tarima activa: {epc}", 1);
                    }
                }
                else
                {
                    Log($"EPC desconocido detectado: {epc}", 1);
                }
            }
        }




        private void Log(string message, int level = 1)
        {
            if (level <= LogLevel)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }
        }



        private void AssociateTarimaWithOperator(string tarima, string operador)
        {
            // Guardar la asociación
            _associations.Add((tarima, operador));
            Console.WriteLine($"Asociación completada: Tarima {tarima} con Operador {operador}");

            // Aquí podrías emitir un evento o llamar a un servicio para notificar al frontend
        }

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
