namespace SiloManager.Application.Enums
{
    public enum StatusUmidade
    {
        Seco = 0,  // abaixo do mínimo   → 🟡 Amarelo
        Ideal = 1,  // dentro do range    → 🟢 Verde
        Atencao = 2,  // acima do ideal     → 🟡 Amarelo
        Critico = 3   // acima do máximo    → 🔴 Vermelho + Retrabalho
    }
}