var builder = DistributedApplication.CreateBuilder(args);

// Add RabbitMQ for message broker
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("rabbitmq-data")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

// Add Redis for distributed caching
var redis = builder.AddRedis("redis")
    .WithDataVolume("redis-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Add MongoDB container with volume for data persistence
var mongodb = builder.AddMongoDB("mongodb")
    .WithDataVolume("mongodb-data")
    .WithLifetime(ContainerLifetime.Persistent);

var hardwareStoreDb = mongodb.AddDatabase("HardwareStoreDb");

// Add SQL Server container (optional, for demo purposes)
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume("sqlserver-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Add existing microservice (WebUI becomes a microservice)
var webApi = builder.AddProject<Projects.HardwareStore_WebUI>("webapi")
    .WithReference(hardwareStoreDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(mongodb)
    .WaitFor(redis)
    .WaitFor(rabbitmq);

// Add Aggregator Service
var aggregator = builder.AddProject<Projects.HardwareStore_AggregatorService>("aggregator")
    .WithReference(webApi)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(webApi);

// Add API Gateway (entry point for the system)
var gateway = builder.AddProject<Projects.HardwareStore_ApiGateway>("gateway")
    .WithReference(webApi)
    .WithReference(aggregator)
    .WaitFor(aggregator);

builder.Build().Run();
