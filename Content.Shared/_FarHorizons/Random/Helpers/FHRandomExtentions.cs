using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._FarHorizons.Random.Helpers;

public static class FhRandomExtensions
{
  public static void ShufflePredicted<T>(IGameTiming timing, NetEntity ent, IList<T> list)
  {
    var random = SharedRandomExtensions.PredictedRandom(timing, ent);
    random.Shuffle(list);
  }
}