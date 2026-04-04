namespace Analyzer.Infrastructure.Queries;

public static class CypherQueryFactory
{
    #region Компоненты (Components)

    public static string GetComponentsBySystemId() => @"
        MATCH (n) 
        WHERE n.system_id = $SystemId 
        RETURN n.name AS Name, 
               n.id AS Id, 
               n.desc AS Desc,
               n.system_id AS SystemId, 
               labels(n) AS Type";

    public static string GetComponentById() => @"
        MATCH (n) 
        WHERE n.id = $Id 
        RETURN n.name AS Name, 
               n.desc AS Desc, 
               n.system_id AS SystemId, 
               labels(n) AS Type";

    public static string AddComponent(string componentType) => $@"
        CREATE (:{componentType} {{
            name: $Name, 
            id: $Guid, 
            desc: $Description,
            system_id: $SystemId
        }})";

    public static string UpdateComponent() => @"
        MATCH (c {id: $Id})
        SET c.name = $Name, 
            c.desc = $Description";

    public static string DeleteComponent() => @"
        MATCH (c {id: $Id})
        DETACH DELETE c";

    #endregion

    #region Связи (Links)

    public static string AddLink() => @"
        MATCH (s { id: $SourceId })
        MATCH (t { id: $TargetId })
        CREATE (s)-[:DEPENDS_ON {id: $Id, severity: $Severity, protocol: $Protocol}]->(t)";

    public static string GetLinksBySystemId() => @"
        MATCH (source)-[r:DEPENDS_ON]->(target)
        WHERE source.system_id = $SystemId AND target.system_id = $SystemId
        RETURN source.id AS SourceId, target.id AS TargetId, 
               r.severity AS Severity, r.protocol AS Protocol, r.id AS Id";

    public static string DeleteLink() => @"
        MATCH ()-[r:DEPENDS_ON]->()
        WHERE r.id = $Id
        DELETE r";

    #endregion

    #region Аналитика отказоустойчивости (Resilience Analysis)

    public static string GetCascadingFailureImpact() => @"
        MATCH (failed {id: $FailedId})<-[:DEPENDS_ON*]-(affected)
        RETURN affected.id AS Id";

    public static string GetCyclicDependencies() => @"
        MATCH path = (c {system_id: $SystemId})-[:DEPENDS_ON*]->(c)
        RETURN [node in nodes(path) | node.id] AS CycleIds";

    public static string GetSinglePointsOfFailure() => @"
        MATCH (c {system_id: $SystemId})<-[:DEPENDS_ON*]-(dependent)
        WITH c, count(DISTINCT dependent) AS ImpactCount
        WHERE ImpactCount >= $Threshold
        RETURN c.id AS Id, ImpactCount";

    public static string GetDecommissioningImpact() => @"
        MATCH (target {id: $TargetId})<-[:DEPENDS_ON*]-(impacted)
        RETURN DISTINCT impacted.id AS Id";

    public static string GetDeploymentRiskPaths() => @"
        MATCH path = (dependent)-[:DEPENDS_ON*]->(target {id: $TargetId})
        RETURN[node in nodes(path) | node.id] AS PathIds";

    #endregion
}