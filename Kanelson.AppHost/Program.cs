var builder = DistributedApplication.CreateBuilder(args);


var postgres = builder.AddPostgres("kanelson-db")
    .WithDataVolume("kanelson-data")
    .AddDatabase("kanelson");

builder.AddProject<Projects.Kanelson>("kanelson-api")
    .WithReference(postgres, "KanelsonDb")
    .WaitFor(postgres);



builder.Build().Run();
