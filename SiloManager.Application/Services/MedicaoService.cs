using SiloManager.Application.DTOs;
using SiloManager.Application.Enums;
using SiloManager.Application.Session;
using SiloManager.Domain.Entities;
using SiloManager.Domain.Interfaces.Repositories;

namespace SiloManager.Application.Services
{
    public class MedicaoService
    {
        private readonly IMedicaoRepository _medicaoRepo;
        private readonly IProdutoRepository _produtoRepo;
        private readonly IEquipamentoRepository _equipRepo;
        private readonly IConfiguracaoRepository _configRepo;

        public MedicaoService(IMedicaoRepository medicaoRepo,
                              IProdutoRepository produtoRepo,
                              IEquipamentoRepository equipRepo,
                              IConfiguracaoRepository configRepo)
        {
            _medicaoRepo = medicaoRepo;
            _produtoRepo = produtoRepo;
            _equipRepo = equipRepo;
            _configRepo = configRepo;
        }

        // Verifica se o timer liberou uma nova medição
        // Retorna: null = liberado | TimeSpan = tempo restante
        public async Task<TimeSpan?> VerificarTimerAsync(int empresaId)
        {
            var config = await _configRepo.GetByEmpresaAsync(empresaId);
            var intervalo = config?.IntervaloMinimoSegundos ?? 900;

            var ultima = await _medicaoRepo.GetUltimaGeralAsync(empresaId);
            if (ultima is null) return null; // nunca mediu, libera

            var decorrido = DateTime.Now - ultima.DataHoraSistema;
            var restante = TimeSpan.FromSeconds(intervalo) - decorrido;

            return restante > TimeSpan.Zero ? restante : null;
        }

        // Calcula o status do semáforo baseado no produto cadastrado
        public StatusUmidade CalcularStatus(Produto produto, double umidade)
        {
            if (umidade < produto.UmidadeMinima) return StatusUmidade.Seco;
            if (umidade <= produto.UmidadeIdeal) return StatusUmidade.Ideal;
            if (umidade <= produto.UmidadeMaxima) return StatusUmidade.Atencao;
            return StatusUmidade.Critico;
        }

        // Enriquece o DTO da serial com dados do banco (produto e status)
        public async Task<LeituraSerialDto> EnriquecerLeituraAsync(LeituraSerialDto dto)
        {
            var produto = await _produtoRepo.GetByNomeAsync(dto.NomeProduto);
            if (produto is not null)
                dto.Status = CalcularStatus(produto, dto.Umidade);

            return dto;
        }

        // Salva a medição confirmada pelo operador
        public async Task<Medicao> SalvarMedicaoAsync(
            int empresaId,
            int produtoId,
            int equipamentoId,
            int siloDestinoId,
            double umidade,
            bool isRetrabalho,
            string dadosBrutos,
            DateTime dataHoraEquipamento,
            string? observacao = null)
        {
            var sessao = SessaoUsuario.Atual
                ?? throw new InvalidOperationException("Nenhum usuário logado.");

            // Calcula intervalo desde a última medição
            var ultima = await _medicaoRepo.GetUltimaGeralAsync(empresaId);
            int? intervalo = ultima is null
                ? null
                : (int)(DateTime.Now - ultima.DataHoraSistema).TotalSeconds;

            var medicao = new Medicao
            {
                EmpresaId = empresaId,
                UsuarioId = sessao.UsuarioId,
                ProdutoId = produtoId,
                EquipamentoId = equipamentoId,
                SiloDestinoId = siloDestinoId,
                Umidade = umidade,
                DataHoraSistema = DateTime.Now,
                DataHoraEquipamento = dataHoraEquipamento,
                IntervaloSegundos = intervalo,
                IsRetrabalho = isRetrabalho,
                Observacao = observacao,
                DadosBrutosSerial = dadosBrutos
            };

            await _medicaoRepo.AddAsync(medicao);
            await _medicaoRepo.SaveChangesAsync();

            return medicao;
        }
    }
}