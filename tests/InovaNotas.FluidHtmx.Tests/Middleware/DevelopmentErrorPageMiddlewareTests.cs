using FluentAssertions;
using InovaNotas.FluidHtmx.Exceptions;
using InovaNotas.FluidHtmx.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace InovaNotas.FluidHtmx.Tests.Middleware;

public class DevelopmentErrorPageMiddlewareTests
{
    private static DevelopmentErrorPageMiddleware CreateMiddleware(
        RequestDelegate next,
        bool isDevelopment = true)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);

        return new DevelopmentErrorPageMiddleware(
            next,
            env,
            NullLogger<DevelopmentErrorPageMiddleware>.Instance);
    }

    [Fact]
    public async Task RendersErrorPage_OnTemplateParseException()
    {
        var middleware = CreateMiddleware(_ => throw new TemplateParseException("pages/home/index", "Unexpected tag at (3:10)"));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("text/html; charset=utf-8");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        body.Should().Contain("Template Parse Error");
        body.Should().Contain("pages/home/index");
        body.Should().Contain("Unexpected tag");
    }

    [Fact]
    public async Task RendersErrorPage_OnTemplateNotFoundException()
    {
        var middleware = CreateMiddleware(_ => throw new TemplateNotFoundException("pages/missing"));

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("text/html; charset=utf-8");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        body.Should().Contain("Template Not Found");
        body.Should().Contain("pages/missing");
    }

    [Fact]
    public async Task PassesThrough_WhenNoException()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task PassesThrough_InProduction()
    {
        var middleware = CreateMiddleware(
            _ => throw new TemplateParseException("pages/home/index", "Unexpected tag"),
            isDevelopment: false);

        var context = new DefaultHttpContext();

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<TemplateParseException>();
    }
}
