﻿using System;
using System.Linq;
using Mirage.Urbanization.ZoneConsumption;
using Mirage.Urbanization.ZoneConsumption.Base;
using Mirage.Urbanization.ZoneStatisticsQuerying;

namespace Mirage.Urbanization
{
    public class ZoneConsumptionState : IZoneConsumptionState
    {
        private IAreaZoneConsumption _zoneConsumption = new EmptyZoneConsumption();
        public DateTime LastUpdateDateTime { get; private set; } = DateTime.Now;

        public IAreaZoneConsumption GetZoneConsumption()
        {
            return _zoneConsumption;
        }

        public IConsumeAreaOperation TryConsumeWith(IAreaZoneConsumption consumption)
        {
            if (consumption == null) throw new ArgumentNullException(nameof(consumption));
            return new ConsumeAreaOperation(_zoneConsumption.GetCanOverrideWith(consumption), (toBeDeployedConsumption) =>
            {
                _zoneConsumption = toBeDeployedConsumption;
                LastUpdateDateTime = DateTime.Now;
            });
        }

        public bool GetIsPowerGridMember()
        {
            return GetIsNetworkMember<PowerLineConsumption>() ||
                   GetZoneConsumption() is ZoneClusterMemberConsumption;
        }

        public bool GetIsWater()
        {
            return GetIsNetworkMember<WaterZoneConsumption>();
        }

        public bool GetIsNetworkMember<TBaseNetworkZoneConsumption>()
            where TBaseNetworkZoneConsumption : BaseInfrastructureNetworkZoneConsumption
        {
            var consumptionState = GetZoneConsumption();
            var consumptionAsPowerLine = consumptionState as TBaseNetworkZoneConsumption;
            var consumptionAsIntersection = consumptionState as IntersectingZoneConsumption;

            if (consumptionAsPowerLine != null)
                return true;

            return consumptionAsIntersection != null && consumptionAsIntersection
                .GetIntersectingTypes()
                .Contains(typeof(TBaseNetworkZoneConsumption));  
        }

        public bool GetIsRoadNetworkMember()
        {
            return GetIsNetworkMember<RoadZoneConsumption>();
        }

        public bool GetIsRailroadNetworkMember()
        {
            return GetIsNetworkMember<RailRoadZoneConsumption>();
        }

        public QueryResult<ZoneClusterMemberAndParent<T>> GetIfZoneClusterAndParent<T>() where T : BaseZoneClusterConsumption
        {
            return (_zoneConsumption as ZoneClusterMemberConsumption)
                .ToQueryResult()
                .WithResultIfHasMatch(x =>
                {
                    if (x.ParentBaseZoneClusterConsumption is T)
                    {
                        return new ZoneClusterMemberAndParent<T>(x, x.ParentBaseZoneClusterConsumption as T)
                            .ToQueryResult();
                    }
                    return QueryResult<ZoneClusterMemberAndParent<T>>.Empty;
                }, QueryResult<ZoneClusterMemberAndParent<T>>.Empty);
        }

        public bool GetIsZoneClusterMember()
        {
            return GetZoneConsumption() is ZoneClusterMemberConsumption;
        }

        public QueryResult<ZoneClusterMemberConsumption> QueryAsZoneClusterMember()
        {
            var consumption = GetZoneConsumption() as ZoneClusterMemberConsumption;
            return QueryResult<ZoneClusterMemberConsumption>.Create(consumption);
        }

        public void WithNetworkMember<TBaseNetworkZoneConsumption>(Action<TBaseNetworkZoneConsumption> action)
            where TBaseNetworkZoneConsumption : BaseInfrastructureNetworkZoneConsumption
        {
            var consumptionState = GetZoneConsumption();
            var consumptionAsPowerLine = consumptionState as TBaseNetworkZoneConsumption;
            var consumptionAsIntersection = consumptionState as IntersectingZoneConsumption;

            if (consumptionAsPowerLine != null)
                action(consumptionAsPowerLine);
            else if (consumptionAsIntersection != null)
            {
                var match =
                    consumptionAsIntersection.GetIntersectingZoneConsumptions()
                        .OfType<TBaseNetworkZoneConsumption>()
                        .FirstOrDefault();
                if (match != null)
                    action(match);
            }
        }
    }
}