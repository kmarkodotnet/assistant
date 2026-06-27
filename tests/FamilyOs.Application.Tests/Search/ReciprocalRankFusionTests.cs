using FamilyOs.Application.Search.Rrf;

namespace FamilyOs.Application.Tests.Search;

public sealed class ReciprocalRankFusionTests
{
    [Fact]
    public void Fuse_BothLists_ProducesCorrectRanking()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        // id1 appears in both lists at top positions → should win
        var fts = new List<Guid> { id1, id2 };
        var vector = new List<Guid> { id1, id3 };

        var result = ReciprocalRankFusion.Fuse(fts, vector);

        Assert.NotEmpty(result);
        Assert.Equal(id1, result[0].id); // id1 appears in both, gets highest combined score
    }

    [Fact]
    public void Fuse_EmptyLists_ReturnsEmpty()
    {
        var result = ReciprocalRankFusion.Fuse([], []);
        Assert.Empty(result);
    }

    [Fact]
    public void Fuse_SingleListOnly_ReturnsItems()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var result = ReciprocalRankFusion.Fuse([id1, id2], []);
        Assert.Equal(2, result.Count);
    }
}
