// File : /team-samsara/apps/api/src/TeamSamsara.Shared.Testing/TestWebApplicationFactory.cs
// Version : 1.0.0
// Latest commit: feature/shared-testing-harness
// Author : Gerrah
// Purpose : Boots a real instance of TeamSamsara.Api in-memory for
// integration tests — real DI, real middleware pipeline, real routing.
// Every module's test project references this rather than each one
// standing up its own WebApplicationFactory independently. Program refers
// to the entry point in TeamSamsara.Api/Program.cs, made accessible via
// that file's "public partial class Program {}" declaration.

using Microsoft.AspNetCore.Mvc.Testing;

namespace TeamSamsara.Shared.Testing;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
}
