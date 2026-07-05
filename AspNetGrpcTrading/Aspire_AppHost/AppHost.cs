var builder = DistributedApplication.CreateBuilder(args);

// "trading" is logical resource name 

var trading = builder.AddProject<Projects.Grpc_Server_Trading>("trading", launchProfileName: "http");


builder.AddProject<Projects.Grpc_Client_Portfolio>("portfolio")
    .WithReference(trading) // Says that "portfolio" depends on "trading", and PASSES ITS COORDINATES THERE
    .WaitFor(trading); // Holds "Portfolio" start until "Trading" is up and running

builder.Build().Run();

// 1. Where is this launchProfileName "http" defined?

// In the Trading project, in "Properties/launchSettings.json".
// It defines the URL and other settings for running the Trading project.


