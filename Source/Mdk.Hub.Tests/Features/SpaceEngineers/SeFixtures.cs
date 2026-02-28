namespace Mdk.Hub.Tests.Features.SpaceEngineers;

/// <summary>
///     Minimal fixture XML files for testing SE data parsing.
///     All type names, display names and icon paths are entirely fictional —
///     no Space Engineers game content is reproduced here.
/// </summary>
internal static class SeFixtures
{
    /// <summary>
    ///     A minimal MyTexts.resx-style localization file.
    ///     Format follows the standard .NET RESX schema used by SE.
    /// </summary>
    public const string LocalizationResx = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <data name="DisplayName_TestRefinery" xml:space="preserve">
            <value>Test Refinery</value>
          </data>
          <data name="DisplayName_TestGyro" xml:space="preserve">
            <value>Test Gyroscope</value>
          </data>
          <data name="DisplayName_TestArmor" xml:space="preserve">
            <value>Test Armor Block</value>
          </data>
          <data name="DisplayName_Category_Processing" xml:space="preserve">
            <value>Processing</value>
          </data>
          <data name="DisplayName_Category_Motion" xml:space="preserve">
            <value>Motion</value>
          </data>
          <data name="DisplayName_Category_Motion_Sub" xml:space="preserve">
            <value>Motion Sub</value>
          </data>
        </root>
        """;

    /// <summary>
    ///     A minimal BlockCategories.sbc fixture.
    ///     Includes: two normal categories, one sub-category (3 leading spaces), one excluded (IsBlockCategory=false).
    /// </summary>
    public const string BlockCategoriesSbc = """
        <?xml version="1.0"?>
        <Definitions>
          <BlockCategories>
            <Category>
              <Name>Section1_Processing</Name>
              <DisplayName>DisplayName_Category_Processing</DisplayName>
              <IsBlockCategory>true</IsBlockCategory>
              <ItemIds>
                <string>TestRefinery/TestRefinery_Large</string>
              </ItemIds>
            </Category>
            <Category>
              <Name>Section2_Motion</Name>
              <DisplayName>DisplayName_Category_Motion</DisplayName>
              <ItemIds>
                <string>TestGyro/TestGyro_Large</string>
              </ItemIds>
            </Category>
            <Category>
              <Name>Section2_Motion_Sub</Name>
              <DisplayName>   DisplayName_Category_Motion_Sub</DisplayName>
              <ItemIds>
                <string>TestGyro/TestGyro_Small</string>
              </ItemIds>
            </Category>
            <Category>
              <Name>Section3_Excluded</Name>
              <DisplayName>DisplayName_Category_Processing</DisplayName>
              <IsBlockCategory>false</IsBlockCategory>
              <ItemIds>
                <string>TestArmor/TestArmor_Small</string>
              </ItemIds>
            </Category>
          </BlockCategories>
        </Definitions>
        """;

    /// <summary>
    ///     A minimal CubeBlocks SBC fixture.
    ///     Includes: one block with icon, one without icon, one with a MyObjectBuilder_ prefix on TypeId.
    /// </summary>
    public const string CubeBlocksSbc = """
        <?xml version="1.0"?>
        <Definitions>
          <CubeBlocks>
            <Definition>
              <Id>
                <TypeId>TestRefinery</TypeId>
                <SubtypeId>TestRefinery_Large</SubtypeId>
              </Id>
              <DisplayName>DisplayName_TestRefinery</DisplayName>
              <Icon>Textures\GUI\Icons\Cubes\test_refinery.dds</Icon>
              <CubeSize>Large</CubeSize>
            </Definition>
            <Definition>
              <Id>
                <TypeId>TestGyro</TypeId>
                <SubtypeId>TestGyro_Large</SubtypeId>
              </Id>
              <DisplayName>DisplayName_TestGyro</DisplayName>
              <Icon>Textures\GUI\Icons\Cubes\test_gyro.dds</Icon>
              <CubeSize>Large</CubeSize>
            </Definition>
            <Definition>
              <Id>
                <TypeId>TestGyro</TypeId>
                <SubtypeId>TestGyro_Small</SubtypeId>
              </Id>
              <DisplayName>DisplayName_TestGyro</DisplayName>
              <CubeSize>Small</CubeSize>
            </Definition>
            <Definition>
              <Id>
                <TypeId>MyObjectBuilder_TestArmor</TypeId>
                <SubtypeId>TestArmor_Small</SubtypeId>
              </Id>
              <DisplayName>DisplayName_TestArmor</DisplayName>
              <CubeSize>Small</CubeSize>
            </Definition>
          </CubeBlocks>
        </Definitions>
        """;
}
