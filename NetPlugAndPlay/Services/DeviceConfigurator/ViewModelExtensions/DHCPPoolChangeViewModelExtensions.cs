using NetPlugAndPlay.Services.DeviceConfigurator.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DeviceConfigurator.ViewModelExtensions
{
    public static class DHCPPoolChangeViewModelExtensions
    {
        public static bool Changed(this DHCPPoolChangeViewModel changes)
        {
            if (changes.DHCPRelay != changes.ExistingDHCPRelay)
                return true;

            if (!changes.Prefix.Equals(changes.ExistingPrefix))
                return true;

            if (!changes.GatewayAddress.Equals(changes.ExistingGatewayAddress))
                return true;

            if (!changes.DomainName.Equals(changes.ExistingDomainName))
                return true;

            if (!changes.TFTPBootFile.Equals(changes.ExistingTFTPBootFile))
                return true;

            if (
                (changes.ExistingReservations == null) &&
                (changes.Reservations == null)
            )
            {
                return false;
            }

            if (
                (changes.ExistingReservations == null) &&
                (changes.Reservations == null)
            )
            {
                return true;
            }

            var countMatches =
                (from oldReservation in changes.ExistingReservations
                 join newReservation in changes.Reservations
                 on oldReservation equals newReservation
                 select
                 (
                    oldReservation
                 )
                )
                .Count();

            return countMatches != changes.ExistingReservations.Count;
        }
    }
}
