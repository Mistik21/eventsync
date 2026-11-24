using eventsynс.Domain.Entities;
using Xunit;

namespace EventSync.UnitTests.Domain;

public class CostTests
{
    [Fact]
    public void CreateCost_RejectsNegativeAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Cost(Guid.NewGuid(), "Food", -1));
    }

    [Fact]
    public void ChangeAmount_AllowsZeroAndPreservesAmountOnInvalidChange()
    {
        var cost = new Cost(Guid.NewGuid(), "Food", 50);

        Assert.Throws<ArgumentOutOfRangeException>(() => cost.ChangeAmount(-1));
        Assert.Equal(50, cost.Amount);

        cost.ChangeAmount(0);

        Assert.Equal(0, cost.Amount);
    }
}
