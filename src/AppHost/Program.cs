var builder = DistributedApplication.CreateBuilder(args);

var mysql = builder.AddMySql("mysql");
var database = mysql.AddDatabase("cwm-db");

var redis = builder.AddRedis("cwm-cache")
    .WithRedisInsight()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.ZARI_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithExternalHttpEndpoints();

builder.Build().Run();
