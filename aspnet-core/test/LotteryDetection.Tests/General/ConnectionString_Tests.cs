using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace LotteryDetection.Tests.General
{
    // ReSharper disable once InconsistentNaming
    public class ConnectionString_Tests
    {
        [Fact]
        public void SqlConnectionStringBuilder_Test()
        {
            var csb = new SqlConnectionStringBuilder("Server=localhost; Database=LotteryDetection; Trusted_Connection=True;");
            csb["Database"].ShouldBe("LotteryDetection");
        }
    }
}
