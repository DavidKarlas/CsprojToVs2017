using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class XamlPagesTransformation : ILegacyOnlyProjectTransformation
	{
		public XamlPagesTransformation(ILogger logger = null)
		{
		}

		/// <inheritdoc />
		public void Transform(Project definition)
		{
			if (definition.FilePath.Name == "Data.csproj")
			{
				SwitchToGlob(definition, "EmbeddedResource", "Resources");
			}

			if (definition.FilePath.Name == "OMDP.MapsDataPipeline.Utils.Tests.csproj")
			{
				SwitchToGlob(definition, "Content", "ResourceFiles");
			}


			if (definition.FilePath.Name == "OMDP.MapsDataPipeline.Data.Enrichment.Tests.csproj")
			{
				SwitchToGlob(definition, "None", "Resources");
			}



			if (definition.FilePath.Name == "DVT.Common.csproj")
			{
				SwitchToGlob(definition, "Content", "TestConfig");
			}
			if (definition.FilePath.Name == "OMDP.MapsDataPipeline.FMU.FastMapUpdate.Tests.csproj")
			{
				SwitchToGlob(definition, "None", "TestSamples");
			}
		}

		private static void SwitchToGlob(Project definition, string itemType, string path)
		{
			definition.ItemGroups
		   .SelectMany(x => x.Elements())
		   .Where(x => x.Name.LocalName == itemType)
		   .Remove();

			var itemGroup = new XElement("ItemGroup");
			XElement embeddedResource = new XElement(itemType);
			embeddedResource.Add(new XAttribute("Include", $"{path}\\**\\*.*"));
			embeddedResource.Add(new XElement("CopyToOutputDirectory", "Always"));
			itemGroup.Add(embeddedResource);
			definition.ItemGroups.Add(itemGroup);
		}
	}
}