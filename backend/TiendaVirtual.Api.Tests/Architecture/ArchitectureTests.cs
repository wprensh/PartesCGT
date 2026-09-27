using NetArchTest.Rules;
using TiendaVirtual.Api.Common.Web;

namespace TiendaVirtual.Api.Tests.Architecture;

/// <summary>
/// Reglas de dependencias (ver backend/ARCHITECTURE.md). Si una falla, el mensaje dice qué tipo la rompe.
/// Cada regla verifica además que su selección no esté vacía, para que no pase "en vacío" tras un renombre.
/// </summary>
public class ArchitectureTests
{
    private const string Root = "TiendaVirtual.Api";

    private static PredicateList Select(Func<Predicates, PredicateList> filter)
    {
        var selection = filter(Types.InAssembly(typeof(ApiControllerBase).Assembly).That());
        Assert.NotEmpty(selection.GetTypes());
        return selection;
    }

    private static void AssertRule(ConditionList rule, string description)
    {
        var result = rule.GetResult();
        Assert.True(result.IsSuccessful, $"{description}. Lo rompen: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void El_dominio_no_depende_de_la_web_ni_de_la_base_de_datos() => AssertRule(
        Select(t => t.ResideInNamespace($"{Root}.Domain")).ShouldNot().HaveDependencyOnAny(
            $"{Root}.Features", $"{Root}.Infrastructure", "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore"),
        "Domain solo puede usar Common y .NET base");

    [Fact]
    public void Common_no_depende_del_negocio() => AssertRule(
        Select(t => t.ResideInNamespace($"{Root}.Common")).ShouldNot()
            .HaveDependencyOnAny($"{Root}.Features", $"{Root}.Infrastructure", $"{Root}.Domain"),
        "Common es transversal y no conoce el negocio");

    [Fact]
    public void Los_controladores_son_delgados_y_no_tocan_la_base_de_datos() => AssertRule(
        Select(t => t.Inherit(typeof(ApiControllerBase))).ShouldNot()
            .HaveDependencyOnAny($"{Root}.Infrastructure", "Microsoft.EntityFrameworkCore"),
        "Los controladores delegan en servicios; no usan AppDbContext");

    [Fact]
    public void Todos_los_controladores_heredan_de_ApiControllerBase() => AssertRule(
        Select(t => t.HaveNameEndingWith("Controller").And().AreNotAbstract()).Should().Inherit(typeof(ApiControllerBase)),
        "Así todos traducen los errores de negocio igual");

    [Fact]
    public void Los_servicios_no_dependen_de_MVC() => AssertRule(
        Select(t => t.ResideInNamespace($"{Root}.Features").And().HaveNameEndingWith("Service"))
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore.Mvc"),
        "Los casos de uso devuelven Result<T>, no respuestas HTTP");

    [Fact]
    public void Las_consultas_no_dependen_de_MVC() => AssertRule(
        Select(t => t.ResideInNamespace($"{Root}.Features").And().HaveNameEndingWith("Queries"))
            .ShouldNot().HaveDependencyOn("Microsoft.AspNetCore.Mvc"),
        "Las lecturas devuelven DTOs, no respuestas HTTP");
}
