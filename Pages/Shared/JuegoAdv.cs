namespace Love.Pages.Shared;

/// <summary>Datos para pintar la tarjeta de un juego con frase. Diario: modo zona Juegos (una partida al día).</summary>
public record JuegoAdv(Love.Models.TipoCasilla Tipo, bool Resuelta, string Frase, bool Diario = false);
