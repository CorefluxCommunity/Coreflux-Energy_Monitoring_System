using App.WorkerService;

public class Program
{
	public static void Main(string[] args)
	{
		// Create a builder for the host application
		HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
		
		// Register the Worker service with the dependency injection container
		builder.Services.AddHostedService<Worker>();

		// Build the host
		IHost host = builder.Build();
		
		Console.WriteLine("Starting Agregator");
		// Run the host application
		host.Run();
	}
}
