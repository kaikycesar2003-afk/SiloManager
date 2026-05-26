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

                // Processa linha completa (terminada em \n)
                while (_buffer.Contains('\n'))
                {
                    var idx = _buffer.IndexOf('\n');
                    var linha = _buffer[..idx].Trim();
                    _buffer = _buffer[(idx + 1)..];

                    if (!string.IsNullOrWhiteSpace(linha))
                        ProcessarLinha(linha);
                }
            }
            catch (Exception ex)
            {
                ErroRecebido?.Invoke(ex.Message);
            }
        }

        private void ProcessarLinha(string linha)
        {
            // Formato Gehaka: "11773 ; 11.9 ; ... ; Soja ; ... ; G2000 ; ... ; 23012786001080 ; 17:14:07 ; 14/05/26 ; hash ; hash"
            var campos = linha.Split(" ; ");
            if (campos.Length < 16) return;

            try
            {
                var cultura = CultureInfo.InvariantCulture;

                var dto = new LeituraSerialDto
                {
                    Umidade = double.Parse(campos[1].Trim(), cultura),
                    NomeProduto = campos[7].Trim(),
                    ModeloEquipamento = campos[10].Trim(),
                    NumeroSerieEquipamento = campos[13].Trim(),
                    DataHoraEquipamento = ParseDataHora(campos[15].Trim(), campos[14].Trim()),
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