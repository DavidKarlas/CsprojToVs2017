using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Project2015To2017Tests")]

namespace Project2015To2017
{
	public sealed class ProjectConverter
	{
		private static readonly IReadOnlyDictionary<string, string> ProjectFileMappings = new Dictionary<string, string>
		{
			{ ".csproj", "cs" },
			{ ".vbproj", "vb" },
			{ ".fsproj", "fs" }
		};

		private readonly ILogger logger;
		private readonly ConversionOptions conversionOptions;
		private readonly ProjectReader projectReader;

		private static IReadOnlyCollection<ITransformation> TransformationsToApply(ConversionOptions conversionOptions)
		{
			return new ITransformation[]
			{
				new TargetFrameworkTransformation(
					conversionOptions.TargetFrameworks,
					conversionOptions.AppendTargetFrameworkToOutputPath),
				new PropertySimplificationTransformation(),
				new PropertyDeduplicationTransformation(),
				new TestProjectPackageReferenceTransformation(),
				new AssemblyReferenceTransformation(),
				new RemovePackageAssemblyReferencesTransformation(),
				new DefaultAssemblyReferenceRemovalTransformation(),
				new RemovePackageImportsTransformation(),
				new FileTransformation(),
				new NugetPackageTransformation(),
				new AssemblyAttributeTransformation(conversionOptions.KeepAssemblyInfo),
				new XamlPagesTransformation(),
				new PrimaryUnconditionalPropertyTransformation(),
			};
		}

		public ProjectConverter(ILogger logger, ConversionOptions conversionOptions = null)
		{
			this.logger = logger;
			this.conversionOptions = conversionOptions ?? new ConversionOptions();
			this.projectReader = new ProjectReader(logger, this.conversionOptions);
		}

		public IEnumerable<Project> Convert(string target)
		{
			var extension = Path.GetExtension(target) ?? throw new ArgumentNullException(nameof(target));
			if (extension.Length > 0)
			{
				if (extension == ".sln")
				{
					foreach (var project in ConvertSolution(target))
					{
						yield return project;
					}
				}
				else if (ProjectFileMappings.TryGetValue(extension, out var fileExtension))
				{
					var file = new FileInfo(target);
					yield return this.ProcessFile(file, null);
				}
				else
				{ 
						this.logger.LogCritical("Please specify a project or solution file.");
				}

				yield break;
			}

			// Process the only solution in given directory
			var solutionFiles = Directory.EnumerateFiles(target, "*.sln", SearchOption.TopDirectoryOnly).ToArray();
			if (solutionFiles.Length == 1)
			{
				foreach (var project in this.ConvertSolution(solutionFiles[0]))
				{
					yield return project;
				}

				yield break;
			}

			var projectsProcessed = 0;
			// Process all csprojs found in given directory
			foreach (var mapping in ProjectFileMappings)
			{
				var projectFiles = Directory.EnumerateFiles(target, "*" + mapping.Key, SearchOption.AllDirectories).ToArray();
				if (projectFiles.Length == 0)
				{
					continue;
				}

				if (projectFiles.Length > 1)
				{
					this.logger.LogInformation($"Multiple project files found under directory {target}:");
				}

				this.logger.LogInformation(string.Join(Environment.NewLine, projectFiles));

				foreach (var projectFile in projectFiles)
				{
					// todo: rewrite both directory enumerations to use FileInfo instead of raw strings
					yield return this.ProcessFile(new FileInfo(projectFile), null);
					projectsProcessed++;
				}
			}

			if (projectsProcessed == 0)
			{
				this.logger.LogCritical("Please specify a project file.");
			}
		}

		private IEnumerable<Project> ConvertSolution(string target)
		{
			this.logger.LogDebug("Solution parsing started.");
			var solution = SolutionReader.Instance.Read(target, this.logger);

			if (solution.ProjectPaths == null)
			{
				yield break;
			}

			foreach (var projectPath in solution.ProjectPaths)
			{
				this.logger.LogInformation("Project found: " + projectPath.Include);
				if (!projectPath.ProjectFile.Exists)
				{
					this.logger.LogError("Project file not found at: " + projectPath.ProjectFile.FullName);
				}
				else
				{
					yield return this.ProcessFile(projectPath.ProjectFile, solution);
				}
			}
		}

		private Project ProcessFile(FileInfo file, Solution solution)
		{
			if (!Validate(file, this.logger))
			{
				return null;
			}

			var project = this.projectReader.Read(file);
			if (project == null)
			{
				return null;
			}

			project.CodeFileExtension = ProjectFileMappings[file.Extension];
			project.Solution = solution;

			foreach (var transform in this.conversionOptions.PreDefaultTransforms)
			{
				transform.Transform(project, this.logger);
			}

			foreach (var transform in TransformationsToApply(this.conversionOptions))
			{
				transform.Transform(project, this.logger);
			}

			foreach (var transform in this.conversionOptions.PostDefaultTransforms)
			{
				transform.Transform(project, this.logger);
			}

			return project;
		}

		internal static bool Validate(FileInfo file, ILogger logger)
		{
			if (file.Exists)
			{
				return true;
			}

			logger.LogError($"File {file.FullName} could not be found.");
			return false;
		}
	}
}
