using System.Collections.Generic;
using Mdk.Hub.Features.SpaceEngineers;

namespace Mdk.Hub.Tests.Features.SpaceEngineers;

[TestFixture]
public class BlockIdTests
{
    [TestCase("Refinery/LargeRefinery", "Refinery", "LargeRefinery")]
    [TestCase("Gyro/", "Gyro", "")]
    [TestCase("Gyro", "Gyro", "")]
    [TestCase("MyObjectBuilder_Thrust/SmallThrust", "Thrust", "SmallThrust")]
    [TestCase("MyObjectBuilder_Refinery/", "Refinery", "")]
    public void Parse_VariousInputs_NormalizesCorrectly(string input, string expectedTypeId, string expectedSubtypeId)
    {
        var id = BlockId.Parse(input);

        Assert.That(id.TypeId, Is.EqualTo(expectedTypeId));
        Assert.That(id.SubtypeId, Is.EqualTo(expectedSubtypeId));
    }

    [Test]
    public void ToString_ReturnsCanonicalForm()
    {
        var id = new BlockId("Refinery", "LargeRefinery");

        Assert.That(id.ToString(), Is.EqualTo("Refinery/LargeRefinery"));
    }

    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var a = new BlockId("Gyro", "Large");
        var b = new BlockId("Gyro", "Large");

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Equality_DifferentSubtype_AreNotEqual()
    {
        var a = new BlockId("Gyro", "Large");
        var b = new BlockId("Gyro", "Small");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Parse_StripsPrefixBeforeComparingEquality()
    {
        var withPrefix = BlockId.Parse("MyObjectBuilder_Gyro/Large");
        var withoutPrefix = BlockId.Parse("Gyro/Large");

        Assert.That(withPrefix, Is.EqualTo(withoutPrefix));
    }

    [Test]
    public void Equality_DifferentTypeId_AreNotEqual()
    {
        var a = new BlockId("Gyro", "Large");
        var b = new BlockId("Thrust", "Large");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_IsCaseSensitive()
    {
        var a = new BlockId("Gyro", "Large");
        var b = new BlockId("gyro", "Large");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void Equality_EmptySubtypeId_MatchesOtherEmptySubtypeId()
    {
        var a = new BlockId("Gyro", string.Empty);
        var b = BlockId.Parse("Gyro");

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void HashCode_SameValues_ProduceSameHashCode()
    {
        var a = new BlockId("Refinery", "Large");
        var b = new BlockId("Refinery", "Large");

        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void HashCode_UsableAsDictionaryKey()
    {
        var dict = new Dictionary<BlockId, string>
        {
            [new BlockId("Refinery", "Large")] = "found"
        };

        Assert.That(dict[BlockId.Parse("Refinery/Large")], Is.EqualTo("found"));
    }
}
