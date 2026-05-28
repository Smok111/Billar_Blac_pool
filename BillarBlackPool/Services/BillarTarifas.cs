namespace BillarBlackPool.Services
{
    public sealed record CalculoTiempoConsumo(int Minutos, decimal Importe, decimal PrecioHora, decimal PrecioMediaHora, decimal PrecioLibrePorMinuto);

    public static class BillarTarifas
    {
        public const decimal PrecioHora = 6m;
        public const decimal PrecioMediaHora = 3m;
        public const decimal PrecioLibrePorMinuto = 0.10m;

        public static CalculoTiempoConsumo Calcular(DateTime inicio, DateTime? fin)
        {
            if (!fin.HasValue || fin.Value <= inicio)
            {
                return new CalculoTiempoConsumo(0, 0m, PrecioHora, PrecioMediaHora, PrecioLibrePorMinuto);
            }

            var minutos = (int)Math.Ceiling((fin.Value - inicio).TotalMinutes);
            var importe = Math.Round(minutos * PrecioLibrePorMinuto, 2, MidpointRounding.AwayFromZero);

            return new CalculoTiempoConsumo(minutos, importe, PrecioHora, PrecioMediaHora, PrecioLibrePorMinuto);
        }
    }
}
