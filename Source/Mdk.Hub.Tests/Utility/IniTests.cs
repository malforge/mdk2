using Mdk.Hub.Utility;
using NUnit.Framework;

namespace Mdk.Hub.Tests.Utility;

[TestFixture]
public class IniTests
{
    [Test]
    public void RoundTrip_SimpleIni_ProducesIdenticalOutput()
    {
        // Arrange
        var original = @"; This is a comment

[Section1]
; Key comment
key1=value1
key2=value2

[Section2]
key3=value3
";

        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();

        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_WithBlankLines_PreservesBlankLines()
    {
        // Arrange
        var original = @"; Section comment

[mdk]
; Comment line 1
; Comment line 2

key1=value1

; Another comment
key2=value2
";

        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();

        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_WithMixedCommentStyles_PreservesExactFormatting()
    {
        // Arrange
        var original = @";No space after semicolon
; Space after semicolon
;  Two spaces

[section]
;Key comment no space
key=value
";

        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();

        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_ComplexTemplate_MatchesOriginal()
    {
        // Arrange - matches the actual PBScript template
        var original = @"; This file is project specific and should be checked in to source control.

[mdk]
; This is a programmable block script project.
; You should not change this.
type=programmableblock

; Toggle trace (on|off) (verbose output)
trace=off

; What type of minification to use (none|trim|stripcomments|lite|full)
; none: No minification
; trim: Removes unused types (NOT members).
; stripcomments: trim + removes comments.
; lite: stripcomments + removes leading/trailing whitespace.
; full: lite + renames identifiers to shorter names.
minify=none
";

        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();

        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
    }

    [Test]
    public void Parse_AttachesCommentsToCorrectElements()
    {
        // Arrange
        var input = @"; Section comment

[mdk]
; Key comment
key1=value1
key2=value2
";

        // Act
        var parsed = Ini.TryParse(input, out var ini);
        var section = ini["mdk"];
        var key1 = section["key1"];
        var key2 = section["key2"];

        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(section.LeadingComment, Does.Contain("Section comment"));
        Assert.That(key1.Comment, Does.Contain("Key comment"));
        Assert.That(key2.Comment, Is.Null.Or.Empty);
    }

    [Test]
    public void WithKey_PreservesExistingComments()
    {
        // Arrange
        var input = @"[section]
; Original comment
key=oldvalue
";
        Ini.TryParse(input, out var ini);

        // Act - update key value
        ini = ini.WithKey("section", "key", "newvalue");
        var output = ini.ToString();

        // Assert - comment should be preserved
        Assert.That(output, Does.Contain("; Original comment"));
        Assert.That(output, Does.Contain("key=newvalue"));
    }

    [Test]
    public void RoundTrip_TrailingCommentAfterLastKey_PreservesComment()
    {
        // Arrange
        var original = @"[section1]
key1=value1

; Trailing comment at end of file
";
        
        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();
        
        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_TrailingBlankLines_PreservesBlankLines()
    {
        // Arrange
        var original = @"[section1]
key1=value1


";
        
        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();
        
        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_CommentAfterLastKeyInSection_AttachesToNextSection()
    {
        // Arrange  
        var original = @"[section1]
key1=value1
; This should be section2's leading comment

[section2]
key2=value2
";
        
        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();
        
        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
    }

    [Test]
    public void RoundTrip_CustomKeysPreserved()
    {
        // Arrange - INI with mix of keys
        var original = @"[mdk]
type=programmableblock
mycustomkey=customvalue
trace=off
anothercustom=test
";
        
        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();
        
        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
        Assert.That(ini["mdk"]["mycustomkey"].Value, Is.EqualTo("customvalue"));
        Assert.That(ini["mdk"]["anothercustom"].Value, Is.EqualTo("test"));
    }

    [Test]
    public void RoundTrip_CustomSectionsPreserved()
    {
        // Arrange - INI with standard and custom sections
        var original = @"[mdk]
type=programmableblock

[mycustomsection]
setting1=value1
setting2=value2

[anothersection]
key=value
";
        
        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();
        
        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
        Assert.That(ini["mycustomsection"]["setting1"].Value, Is.EqualTo("value1"));
        Assert.That(ini["anothersection"]["key"].Value, Is.EqualTo("value"));
    }

    [Test]
    public void RoundTrip_CommentsOnCustomKeys()
    {
        // Arrange
        var original = @"[mdk]
type=programmableblock

; My custom configuration
mycustomkey=debugvalue

; Another comment
trace=off
";
        
        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();
        
        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
        var customKey = ini["mdk"]["mycustomkey"];
        Assert.That(customKey.Comment, Does.Contain("My custom configuration"));
    }

    [Test]
    public void RoundTrip_CustomSectionWithComments()
    {
        // Arrange
        var original = @"[mdk]
type=programmableblock

; User's custom configuration
[deployment]
; Target server
server=192.168.1.100
; Deployment mode
mode=automatic
";
        
        // Act
        var parsed = Ini.TryParse(original, out var ini);
        var output = ini.ToString();
        
        // Assert
        Assert.That(parsed, Is.True);
        Assert.That(output, Is.EqualTo(original));
    }
}
