using SiloManager.Application.DTOs;
using System.Globalization;
using System.IO.Ports;

namespace SiloManager.Application.Services
{
    public class SerialService : IDisposable
    {
        private SerialPort? _porta;
        private string _buffer = string.Empty;

        // Evento disparado quando uma leitura válida chega
        public event Action<LeituraSerialDto>? LeituraRecebida;
        public event Action<string>? ErroRecebido;

        public bool Conectado => _porta?.IsOpen ?? false;

        public void Conectar(string portaCom, int baudRate = 9600)
        {
            Desconectar();

            _porta = new SerialPort(portaCom, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                Encoding = System.Text.Encoding.UTF8
            };

            _porta.DataReceived += OnDataReceived;
            _porta.Open();
        }

        public void Desconectar()
        {
            if (_porta is { IsOpen: true })
                _porta.Close();
            _porta?.Dispose();
            _porta = null;
            _buffer = string.Empty;
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                _buffer += _porta!.ReadExisting();

                // Gehaka pode usar \r, \n ou \r\n como terminador
                var terminadores = new[] { "\r\n", "\r", "\n" };

                foreach (var term in terminadores)
                {
                    while (_buffer.Contains(term))
                    {
                        var idx = _buffer.IndexOf(term);
                        var linha = _buffer[..idx].Trim();
                        _buffer = _buffer[(idx + term.Length)..];

                        if (!string.IsNullOrWhiteSpace(linha))
                            ProcessarLinha(linha);
                    }
                }
            }
            catch (Exception ex)
            {
                ErroRecebido?.Invoke(ex.Message);
            }
        }

        private void ProcessarLinha(string linha)
        {
            // Normaliza espaços múltiplos: "1139 ;  12.97" → "1139 ; 12.97"
            while (linha.Contains("  ")) linha = linha.Replace("  ", " ");

            // Formato Gehaka: "1139 ; 12.97 ; ... ; Soja ; ... ; G810-I ; ... ; 15051903001012 ; 18:15:07 ; 26/05/26"
            var campos = linha.Split(" ; ");
            if (campos.Length < 15) return;

            try
            {
                var cultura = CultureInfo.InvariantCulture;

                // Encontra o campo do produto (texto não numérico após campo 6)
                string nomeProduto = campos[7].Trim();
                string modeloEquip = campos.Length > 9 ? campos[9].Trim() : string.Empty;
                string numeroSerie = campos.Length > 13 ? campos[13].Trim() : string.Empty;
                string horaStr = campos.Length > 14 ? campos[14].Trim() : string.Empty;
                string dataStr = campos.Length > 15 ? campos[15].Trim() : string.Empty;

                var dto = new LeituraSerialDto
                {
                    Umidade = double.Parse(campos[1].Trim(), cultura),
                    NomeProduto = nomeProduto,
                    ModeloEquipamento = modeloEquip,
                    NumeroSerieEquipamento = numeroSerie,
                    DataHoraEquipamento = ParseDataHora(dataStr, horaStr),
                    DadosBrutos = linha
                };

                LeituraRecebida?.Invoke(dto);
            }
            catch (Exception ex)
            {
                ErroRecebido?.Invoke($"Erro ao parsear leitura: {ex.Message}");
            }
        }

        private static DateTime ParseDataHora(string data, string hora)
        {
            // Formato recebido: data="14/05/26" hora="17:14:07"
            if (DateTime.TryParseExact(
                    $"{data} {hora}",
                    "dd/MM/yy HH:mm:ss",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dt))
                return dt;

            return DateTime.Now;
        }

        // Lista as portas COM disponíveis no sistema
        public static string[] ListarPortas() => SerialPort.GetPortNames();

        public void Dispose() => Desconectar();
    }
}