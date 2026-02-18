var builder = DistributedApplication.CreateBuilder(args);

var cicd = builder.AddContainer("cicd", "cicd")
	.WithHttpEndpoint(
		port: 8085,       
		targetPort: 8085, 
		name: "http")
	.WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8085");


builder.Build().Run();
