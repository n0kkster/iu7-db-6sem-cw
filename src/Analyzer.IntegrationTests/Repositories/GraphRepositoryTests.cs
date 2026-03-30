using Analyzer.Domain.Entities;
using Analyzer.Domain.Enums;
using Analyzer.IntegrationTests.Fixtures;
using Analyzer.Infrastructure.Persistence;
using FluentAssertions;

namespace Analyzer.IntegrationTests.Repositories;

[Collection("Neo4j collection")]
public class GraphRepositoryTests(SharedNeo4jFixture fixture)
{
    [Fact]
    public async Task AddComponentAsync_ShouldSaveNodeToGraph()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);

        var systemId = Guid.NewGuid();
        var component = new Component
        {
            SystemId = systemId,
            Name = "Auth Microservice",
            Description = "Handles user authentication",
            Type = ComponentType.Microservice
        };

        // Act
        await repository.AddComponentAsync(component);

        // Assert
        var result = await repository.GetComponentAsync(component.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Auth Microservice");
        result.Type.Should().Be(ComponentType.Microservice);
        result.SystemId.Should().Be(systemId);
    }

    [Fact]
    public async Task AddLinkAsync_ShouldCreateRelationshipBetweenComponents()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);
        var systemId = Guid.NewGuid();

        var source = new Component 
        { 
            SystemId = systemId, 
            Name = "Frontend", 
            Description = "UI", 
            Type = ComponentType.Unknown 
        };
        
        var target = new Component 
        { 
            SystemId = systemId, 
            Name = "Backend API", 
            Description = "API", 
            Type = ComponentType.Microservice 
        };

        await repository.AddComponentAsync(source);
        await repository.AddComponentAsync(target);

        var link = new Link
        {
            SourceId = source.Id,
            TargetId = target.Id,
            Severity = LinkSeverity.High,
            Protocol = ProtocolType.REST
        };

        // Act
        await repository.AddLinkAsync(link);

        // Assert
        var links = await repository.GetLinksBySystemIdAsync(systemId);

        links.Should().HaveCount(1);
        links.First().SourceId.Should().Be(source.Id);
        links.First().TargetId.Should().Be(target.Id);
        links.First().Protocol.Should().Be(ProtocolType.REST);
        links.First().Severity.Should().Be(LinkSeverity.High);
    }
    [Fact]
    public async Task GetComponentsBySystemIdAsync_ShouldReturnOnlySystemsComponents()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);

        var targetSystemId = Guid.NewGuid();
        var otherSystemId = Guid.NewGuid();

        // Целевая система
        await repository.AddComponentAsync(
            new Component 
            { 
                SystemId = targetSystemId, 
                Name = "C1", 
                Description = "D1", 
                Type = ComponentType.Database 
            }
        );

        await repository.AddComponentAsync(
            new Component 
            { 
                SystemId = targetSystemId, 
                Name = "C2", Description = "D2", 
                Type = ComponentType.Database 
            }
        );

        // Другая система
        await repository.AddComponentAsync(
            new Component 
            { 
                SystemId = otherSystemId, 
                Name = "Noise", 
                Description = "Noise", 
                Type = ComponentType.Unknown 
            }
        );

        // Act
        var components = await repository.GetComponentsBySystemIdAsync(targetSystemId);

        // Assert
        components.Should().HaveCount(2);
        components.All(c => c.SystemId == targetSystemId).Should().BeTrue();
    }

    [Fact]
    public async Task GetCascadingFailureImpactAsync_ShouldReturnDependentNodes()
    {
        // Arrange
        var driver = fixture.CreateDriver();
        var repository = new Neo4jGraphRepository(driver);
        var systemId = Guid.NewGuid();

        var compA = new Component 
        { 
            SystemId = systemId, 
            Name = "Service A", 
            Description = "A", 
            Type = ComponentType.Microservice 
        };
        
        var compB = new Component 
        { 
            SystemId = systemId, 
            Name = "Service B", 
            Description = "B", 
            Type = ComponentType.Microservice 
        };
        
        var compC = new Component 
        { 
            SystemId = systemId, 
            Name = "Database C",
            Description = "C",
            Type = ComponentType.Database 
        };

        await repository.AddComponentAsync(compA);
        await repository.AddComponentAsync(compB);
        await repository.AddComponentAsync(compC);

        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compA.Id, 
                TargetId = compB.Id, 
                Protocol = ProtocolType.REST, 
                Severity = LinkSeverity.High 
            }
        );
    
        await repository.AddLinkAsync(
            new Link 
            { 
                SourceId = compB.Id, 
                TargetId = compC.Id, 
                Protocol = ProtocolType.TCP, 
                Severity = LinkSeverity.High 
            }
        );

        // Act
        var impactedIds = await repository.GetCascadingFailureImpactAsync(compC.Id);

        // Assert
        // compA и compB должны пострадать
        impactedIds.Should().Contain(compB.Id);
    }
}