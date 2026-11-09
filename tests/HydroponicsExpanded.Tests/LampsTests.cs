using HydroponicsExpanded.Core;
using Xunit;

namespace HydroponicsExpanded.Tests;

public class LampsTests
{
    [Fact]
    public void TheStockLampsGlowingBeamIsALightSourceToo()
    {
        // The Small's and Medium's stock lamp: "Spot Light" holds the light, "Spot Light (1)" only the beam you see.
        Assert.True(Lamps.IsSource(new[] { "UnityEngine.Transform", "UnityEngine.Light", "UnityEngine.Rendering.Universal.UniversalAdditionalLightData" }));
        Assert.True(Lamps.IsSource(new[] { "UnityEngine.Transform", "VLB.VolumetricLightBeamHD" }));
    }

    [Fact]
    public void TheTanksSparklesAndTheLampsHolderAreNotLightSources()
    {
        Assert.False(Lamps.IsSource(new[] { "UnityEngine.Transform", "UnityEngine.ParticleSystem", "UnityEngine.ParticleSystemRenderer" }));
        Assert.False(Lamps.IsSource(new[] { "UnityEngine.Transform" }));
        Assert.False(Lamps.IsSource(new[] { "UnityEngine.Transform", "MyVLB.Thing", "UnityEngine.LightProbeGroup" }));
    }

    [Fact]
    public void ALampHangingAboveTheBarComesDownUnderIt()
    {
        // The stock grow box lamp hangs at 2.95 m; the Small's bar is under 1.9 m.
        Assert.Equal(1.81f, Lamps.Height(2.95f, 1.81f));
    }

    [Fact]
    public void TheLargeTanksSideLightsStayLow()
    {
        Assert.Equal(0.92f, Lamps.Height(0.92f, 2.92f));
    }
}
