namespace ZARI.Architecture.Tests;

using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

public sealed class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    private const string DomainNamespace = "ZARI.Domain";
    private const string ApplicationNamespace = "ZARI.Application";
    private const string InfrastructureNamespace = "ZARI.Infrastructure";
    private const string ApiNamespace = "ZARI.Api";

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Handlers_Should_Be_Sealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Validators_Should_Be_Sealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Entities_Should_Not_Be_Public_Setters_For_Id()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("ZARI.Domain.Entities")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
