// File : /team-samsara/apps/api/src/TeamSamsara.ArchitectureTests/ModuleIsolationTests.cs
// Version : 1.0.0
// Latest commit: feature/architecture-tests
// Author : Gerrah
// Purpose : Enforces that no business module references another business
// module - modules may depend only on Shared. Loads each module by assembly
// name rather than by referencing a real type inside it, since most modules
// have no real types yet. TeamSamsara.Api is deliberately excluded - it is
// the composition root and is meant to reference every module.

using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace TeamSamsara.ArchitectureTests;

public class ModuleIsolationTests
{
    #region Fields

    private static readonly string[] _moduleAssemblyNames =
    {
        "TeamSamsara.Modules.Identity",
        "TeamSamsara.Modules.Content",
        "TeamSamsara.Modules.Media",
        "TeamSamsara.Modules.Catalog",
        "TeamSamsara.Modules.Orders",
        "TeamSamsara.Modules.PingPong"
    };

    #endregion

    #region Public Methods

    public static TheoryData<string> ModuleNames()
    {
        var data = new TheoryData<string>();

        foreach (var moduleName in _moduleAssemblyNames)
        {
            data.Add(moduleName);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_ShouldNotDependOnAnyOtherModule(string moduleName)
    {
        var assembly = Assembly.Load(moduleName);
        var otherModules = _moduleAssemblyNames.Where(name => name != moduleName).ToArray();

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherModules)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"{moduleName} should not depend on any other module, but found: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    #endregion
}
