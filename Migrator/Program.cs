using Project2015To2017.Analysis;
using Project2015To2017.Writing;
using Project2015To2017;
using Project2015To2017.Migrate2017;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Transforms;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging;
using Project2015To2017.Migrate2019.Library;

var loggerFactory = LoggerFactory.Create(builder =>
{
	builder.AddFilter("Microsoft", LogLevel.Warning)
		   .AddFilter("System", LogLevel.Warning)
		   .AddFilter("SampleApp.Program", LogLevel.Debug)
		   .AddConsole();
});
var facility = new MigrationFacility(loggerFactory.CreateLogger(""));

var folder = @"C:\GIT\OMDP2\source\ADF.Support\ConfigProvider";


facility.ExecuteMigrate(
	new[] { Path.Combine(folder, "ConfigProvider.sln") },
	Vs16TransformationSet.Instance, // the default set of project file transformations

	// The rest are optional, will use sane defaults if not specified

	new ConversionOptions()
	{
		KeepAssemblyInfo = true
	}, // control over things like target framework and AssemblyInfo treatment
	new ProjectWriteOptions()
	{

	}, // control over backup creation and custom source control logic
	new AnalysisOptions()
	{
	}// control over diagnostics which will be run after migration
);

foreach (var ff in new[] { folder, @"C:\GIT\OMDP2\source\ADF.Support\ConfigProvider\Data", @"C:\GIT\OMDP2\source\Buildings\" })
{
	Directory.GetFiles(ff, "AssemblyInfo.cs", SearchOption.AllDirectories).ToList().ForEach(f =>
	{
		File.Delete(f);
		if (Directory.GetFileSystemEntries(Path.GetDirectoryName(f)).Length == 0)
		{
			Directory.Delete(Path.GetDirectoryName(f), true);
		}
	});
}