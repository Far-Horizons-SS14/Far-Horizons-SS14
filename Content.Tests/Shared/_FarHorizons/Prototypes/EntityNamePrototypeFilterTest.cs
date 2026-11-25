using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Content.Shared._FarHorizons.Prototypes;
using Content.Shared.Entry;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.UnitTesting;

namespace Content.Tests.Shared._FarHorizons.Prototypes;

[TestFixture(UnitTestProject.Server)]
[TestFixture(UnitTestProject.Client)]
[TestOf(typeof(EntityNamePrototypeFilter))]
public sealed class EntityNamePrototypeFilterTest(UnitTestProject project) : RobustUnitTest
{
    public override UnitTestProject Project => project;

    protected override Assembly[] GetContentAssemblies() => [typeof(EntryPoint).Assembly];

    [TestCase("emergency oxygen tank", "Emergency Oxygen Tank")]
    [TestCase("Emergency Oxygen Tank", "Emergency Oxygen Tank")]
    [TestCase("advanced PDA", "Advanced PDA")]
    [TestCase("NT Mk-II laser", "NT Mk-II Laser")]
    [TestCase("captain's spare ID card", "Captain's Spare ID Card")]
    [TestCase("double-barreled shotgun", "Double-Barreled Shotgun")]
    [TestCase("  oxygen   tank  ", "  Oxygen   Tank  ")]
    [TestCase("", "")]
    public void TitleCasesEntityNames(string input, string expected)
    {
        var document = Parse($"- type: entity\n  id: Test\n  name: \"{input}\"\n");
        var prototype = ((SequenceDataNode) document.Root).Cast<MappingDataNode>(0);
        var name = prototype.Get<ValueDataNode>("name");
        var start = name.Start;
        var end = name.End;
        var culture = CultureInfo.CurrentCulture;
        try
        {
            // The rule of thumb when testing anything with casing... test it twice, in Turkish!
            // Note that this shouldn't affect anything, since we do the titlecasing in
            // InvariantCulture, but it's good practice.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            EntityNamePrototypeFilter.Apply(document);
            EntityNamePrototypeFilter.Apply(document);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        var result = prototype.Get<ValueDataNode>("name");
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(expected));
            Assert.That(result.Start, Is.EqualTo(start));
            Assert.That(result.End, Is.EqualTo(end));
            Assert.That(name.Value, Is.EqualTo(input));
        });
    }

    [Test]
    public void DoesNotChangeAliasedFieldsOutsideEntityNames()
    {
        var document = Parse("""
            - type: entity
              id: Test
              name: &originalName oxygen tank
              description: *originalName
            - type: reagent
              id: TestReagent
              name: *originalName
            - type: entity
              id: SecondEntity
              name: *originalName
            """);
        var sequence = (SequenceDataNode) document.Root;

        EntityNamePrototypeFilter.Apply(document);

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Cast<MappingDataNode>(0).Get<ValueDataNode>("name").Value, Is.EqualTo("Oxygen Tank"));
            Assert.That(sequence.Cast<MappingDataNode>(0).Get<ValueDataNode>("description").Value, Is.EqualTo("oxygen tank"));
            Assert.That(sequence.Cast<MappingDataNode>(1).Get<ValueDataNode>("name").Value, Is.EqualTo("oxygen tank"));
            Assert.That(sequence.Cast<MappingDataNode>(2).Get<ValueDataNode>("name").Value, Is.EqualTo("Oxygen Tank"));
        });
    }
    private static DataNodeDocument Parse(string yaml)
    {
        using var reader = new StringReader(yaml);
        return DataNodeParser.ParseYamlStream(reader).Single();
    }
}
