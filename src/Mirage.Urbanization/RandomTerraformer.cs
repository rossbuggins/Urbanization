using System;
using System.Linq;
using Mirage.Urbanization.ZoneConsumption;
using Mirage.Urbanization.ZoneConsumption.Base;

namespace Mirage.Urbanization
{
    public class RandomTerraformer
    {
        private readonly Func<WaterZoneConsumption> _waterZoneFactory;
        private readonly Func<WoodlandZoneConsumption> _woodlandZoneFactory;

        public RandomTerraformer(Func<WaterZoneConsumption> waterZoneFactory, Func<WoodlandZoneConsumption> woodlandZoneFactory)
        {
            _waterZoneFactory = waterZoneFactory;
            _woodlandZoneFactory = woodlandZoneFactory;
        }

        internal void ApplyWith(ZoneInfoGrid repo, TerraformingOptions options)
        {
            ScatterZoneConsumptionClouds(
                repo: repo, 
                options: options, 
                random: _random, 
                valueSelector: o => o.Woodlands, 
                createZoneConsumption: _woodlandZoneFactory
            );
            ScatterZoneConsumptionClouds(
                repo: repo,
                options: options,
                random: _random,
                valueSelector: o => o.Lakes,
                createZoneConsumption: _waterZoneFactory
            );

            if (options.HorizontalRiver)
            {
                GenerateRiver(repo, true);
            }
            if (options.VerticalRiver)
            {
                GenerateRiver(repo, false);
            }
        }

        internal static void ScatterZoneConsumptionClouds(
            ZoneInfoGrid repo, 
            TerraformingOptions options,
            Random random,
            Func<TerraformingOptions, int> valueSelector,
            Func<IAreaZoneConsumption> createZoneConsumption)
        {
            foreach (var iteration in Enumerable.Range(0, valueSelector(options)))
            {
                var woodlandTargetPoint = repo.ZoneInfos[new ZonePoint
                {
                    X = random.Next(10, (repo.ZoneWidthAndHeight - 10)),
                    Y = random.Next(10, (repo.ZoneWidthAndHeight - 10))
                }];

                foreach (var subiteration in Enumerable.Range(0, 4))
                {
                    var center = woodlandTargetPoint
                        .GetRelativeZoneInfo(new RelativeZoneInfoQuery(
                            relativeX: random.Next(-3, 3),
                            relativeY: random.Next(-3, 3)

                            )
                        );

                    if (center.HasNoMatch)
                        continue;

                    var targets = center.MatchingObject.GetSurroundingZoneInfosDiamond(3);

                    foreach (var target in targets
                        .Where(x => x.HasMatch)
                        .Where(x => x
                            .MatchingObject
                            .ConsumptionState
                            .GetZoneConsumption()
                            .GetType() != createZoneConsumption().GetType()))
                    { 
                        target.MatchingObject
                            .ConsumptionState
                            .TryConsumeWith(createZoneConsumption())
                            .Apply();
                    }
                }
            }
        }

        private readonly Random _random = new Random();

        internal void GenerateRiver(ZoneInfoGrid repo, bool isHorizontal)
        {
            Func<ZonePoint, int> coordinateOne = isHorizontal
                ? new Func<ZonePoint, int>(x => x.X) : 
                new Func<ZonePoint, int>(x => x.Y);

            Func<ZonePoint, int> coordinateTwo = isHorizontal
                ? new Func<ZonePoint, int>(x => x.Y) :
                new Func<ZonePoint, int>(x => x.X);

            var xRange = repo.ZoneInfos
                .Keys
                .GroupBy(coordinateOne)
                .OrderBy(x => x.Key);

            int currentRiverCenter = _random.Next(repo.ZoneInfos.Keys.Min(coordinateTwo), repo.ZoneInfos.Keys.Max(coordinateTwo));
            int currentRiverWidth = 6;

            foreach (var xRangeEntry in xRange)
            {
                if (xRangeEntry.Key % 2 == 0)
                    currentRiverCenter = _random.Next(0, 50) % 2 == 0 ? currentRiverCenter + 1 : currentRiverCenter - 1;
                else
                {
                    currentRiverWidth = _random.Next(0, 50) % 2 == 0
                        ? currentRiverWidth + 1
                        : currentRiverWidth - 1;

                    if (currentRiverWidth < 4)
                        currentRiverWidth++;
                    else if (currentRiverWidth > 10)
                        currentRiverWidth--;
                }

                var entry = xRangeEntry
                    .SingleOrDefault(x => coordinateTwo(x) == currentRiverCenter);

                if (entry == default(ZonePoint))
                    continue;

                foreach (var zoneInfo in repo.ZoneInfos[entry].GetSurroundingZoneInfosDiamond(currentRiverWidth).Where(x => x.HasMatch))
                {
                    var action = zoneInfo.MatchingObject.ConsumptionState
                        .TryConsumeWith(_waterZoneFactory());

                    if (action.CanOverrideWithResult.WillSucceed)
                        action.Apply();
                }
            }
        }
    }
}