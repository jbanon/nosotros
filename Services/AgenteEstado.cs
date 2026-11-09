using Love.Models;

namespace Love.Services;

public static class AgenteEstado
{
    public static string Texto(TareaAgente t) => t.Estado switch
    {
        EstadoTarea.Pendiente => "⏳ en cola",
        EstadoTarea.EnCurso => "✨ el agente está en ello",
        EstadoTarea.Error => "⚠️ error",
        _ => t.Aplicada != null ? "✓ guardada" : "📝 lista para revisar",
    };
}
