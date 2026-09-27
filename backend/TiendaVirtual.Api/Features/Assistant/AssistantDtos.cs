using System.ComponentModel.DataAnnotations;
using TiendaVirtual.Api.Features.Products;

namespace TiendaVirtual.Api.Features.Assistant;

public record ChatMessage(
    [Required, RegularExpression("user|assistant")] string Role,
    [Required, StringLength(2000)] string Content);

public record AssistantRequest([Required, MinLength(1), MaxLength(20)] List<ChatMessage> Messages);

public record AssistantReply(string Answer, List<ProductDto> Products);
