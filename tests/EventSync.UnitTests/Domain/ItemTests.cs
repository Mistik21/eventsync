using eventsynс.Domain.Entities;
using Xunit;

namespace EventSync.UnitTests.Domain;

public class ItemTests
{
    [Fact]
    public void Create_RejectsNegativeCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateItem(-1));
    }

    [Fact]
    public void ChangeCount_AllowsZeroAndPreservesItOnInvalidUpdate()
    {
        var item = CreateItem(2);

        item.ChangeCount(0);
        Assert.Throws<ArgumentOutOfRangeException>(() => item.ChangeCount(-1));

        Assert.Equal(0, item.Count);
    }

    [Fact]
    public void AssignTo_KeepsExistingOwnerUntilUnassigned()
    {
        var item = CreateItem(1);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        item.AssignTo(first);

        Assert.Throws<InvalidOperationException>(() => item.AssignTo(second));
        Assert.True(item.IsResponsible(first));
        item.UnassignTo();
        Assert.Null(item.ResponsibleID);

        item.AssignTo(second);
        Assert.True(item.IsResponsible(second));
    }

    private static Item CreateItem(int count) => new(Guid.NewGuid(), "Water", "", count, urlShop: null);
}
