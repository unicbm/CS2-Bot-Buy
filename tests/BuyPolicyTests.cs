using BotBuyPatch;
using Xunit;

public sealed class BuyPolicyTests
{
    // Counts are the percentages in upstream 1.0.14, sampled at bin midpoints.
    [Theory]
    [InlineData(true, 800, "1:8,2:15,3:7,36:10,50:50,63:8,64:2")]
    [InlineData(false, 800, "1:8,2:15,30:13,36:12,50:50,64:2")]
    [InlineData(true, 1000, "1:30,2:20,3:30,63:15,64:5")]
    [InlineData(false, 1000, "1:25,2:20,30:40,36:10,64:5")]
    [InlineData(true, 10000, "9:20,16:35,38:10,60:35")]
    [InlineData(false, 10000, "7:70,9:20,11:10")]
    public void KeepsPistolAndOvertimeWeights(bool ct, int money, string expected)
        => Assert.Equal(expected, Distribution(r => BuyPolicy.Pistol(ct, money, r)));

    [Theory]
    [InlineData(8, true, "8:6,16:47,60:47")]
    [InlineData(19, true, "19:40,23:10,24:10,26:30,33:10")]
    [InlineData(25, true, "25:40,27:10,28:50")]
    [InlineData(25, false, "25:35,28:50,29:15")]
    [InlineData(40, true, "1:5,34:40,40:55")]
    [InlineData(40, false, "1:5,17:40,40:55")]
    public void KeepsReplacementWeights(int item, bool ct, string expected)
        => Assert.Equal(expected, Distribution(r => BuyPolicy.Replace((ushort)item, ct, r, false)));

    [Theory]
    [InlineData(true, "0:80,3:10,34:10", "0:86,14:4,38:10")]
    [InlineData(false, "0:80,17:10,30:10", "0:86,11:10,14:4")]
    public void KeepsTeamForceAndAdvantageWeights(bool ct, string force, string advantage)
    {
        Assert.Equal(force, Distribution(r => BuyPolicy.Force(ct, r)));
        Assert.Equal(advantage, Distribution(r => BuyPolicy.Advantage(ct, r)));
    }

    [Fact]
    public void LowGravityKeepsScoutAndUnrelatedRequestsPassThrough()
    {
        Assert.All(Enumerable.Range(0, 100), i => Assert.Equal(40, BuyPolicy.Replace(40, true, (i + .5f) / 100, true)));
        foreach (ushort item in new ushort[] { 7, 9, 11, 16, 38, 60 })
            Assert.Equal(item, BuyPolicy.Replace(item, true, .01f, false));
    }

    [Theory]
    [InlineData(8000, 2800, 2900, true)]
    [InlineData(16000, 3000, 2700, true)]
    [InlineData(5600, 2800, 2900, false)]
    [InlineData(5700, 2800, 2900, true)]
    [InlineData(2800, 8000, 2700, false)]
    [InlineData(8000, 2800, 0, false)]
    public void DonationUsesRemainingCash(int donor, int recipient, int price, bool expected)
        => Assert.Equal(expected, BuyPolicy.CanSponsor(donor, recipient, price));

    [Fact]
    public void ShopIncludesEveryWeightedChoiceWithoutEnemyOnlyWeapons()
    {
        foreach (bool ct in new[] { false, true })
        {
            var shop = BuyPolicy.Shop(ct).ToHashSet();
            foreach (int money in new[] { 800, 1000, 10000 })
            foreach (int i in Enumerable.Range(0, 100))
            {
                ushort item = BuyPolicy.Pistol(ct, money, (i + .5f) / 100);
                Assert.True(item == 50 || shop.Contains(item));
            }
            Assert.DoesNotContain(ct ? (ushort)7 : (ushort)60, shop);
        }
    }

    private static string Distribution(Func<float, ushort> choose)
        => string.Join(',', Enumerable.Range(0, 100).Select(i => choose((i + .5f) / 100))
            .GroupBy(i => i).OrderBy(g => g.Key).Select(g => $"{g.Key}:{g.Count()}"));
}
