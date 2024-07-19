using FluentAssertions;
using Mal.DocumentGenerator.Whitelists;
using NUnit.Framework;

namespace Mal.DocumentGenerator.Tests;

[TestFixture]
public class WhitelistTests
{
    [Test]
    public void Rule_WithTypeButNotMembers_MatchesTypeButNotMembers()
    {
        var whitelist = new Whitelist();
        whitelist.AddWhitelist("System.Type, mscorlib");

        whitelist.IsWhitelisted("mscorlib", "System.Type").Should().BeTrue();
        whitelist.IsWhitelisted("mscorlib", "System.Type.Member").Should().BeFalse();
    }

    [Test]
    public void Rule_WithTypeOrMembers_DoesNotMatchType()
    {
        var whitelist = new Whitelist();
        whitelist.AddWhitelist("System.Type.*, mscorlib");
        
        whitelist.IsWhitelisted("mscorlib", "System.Type").Should().BeFalse();
        whitelist.IsWhitelisted("mscorlib", "System.Type.Member").Should().BeTrue();
    }
    
    [TestCase("System.Type+*, mscorlib")]
    [TestCase("System.Type.*, mscorlib")]
    public void Rule_WithTypeAndMembers_MatchesTypeAndMembers(string pattern)
    {
        var whitelist = new Whitelist();
        whitelist.AddWhitelist(pattern);

        whitelist.IsWhitelisted("mscorlib", "System.Type").Should().BeFalse();
        whitelist.IsWhitelisted("mscorlib", "System.Type.Member").Should().BeTrue();
    }
    
    [Test]
    public void Rule_WithTypeAndSpecificMember_MatchesTypeAndSpecificMember()
    {
        var whitelist = new Whitelist();
        whitelist.AddWhitelist("System.Type.Member, mscorlib");

        whitelist.IsWhitelisted("mscorlib", "System.Type.Member").Should().BeTrue();
        whitelist.IsWhitelisted("mscorlib", "System.Type.OtherMember").Should().BeFalse();
    }
}