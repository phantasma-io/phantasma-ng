using Phantasma.Business.CodeGen;
using Phantasma.Core.Domain.Exceptions;

namespace Phantasma.Business.Tests.CodeGen;

using Xunit;

public class CompilerExceptionsTests
{
    [Fact]
    public void TestCompilerException()
    {
        //TestCompilerException
        Assert.Throws<CompilerException>(TestCode);
    }

    private void TestCode()
    {
        throw new CompilerException(10, "Test Expection");
    }
}
