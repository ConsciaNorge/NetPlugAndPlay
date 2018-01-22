using NetPlugAndPlay.Services.DeviceConfigurator.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DeviceConfigurator.ViewModelExtensions
{
    public static class DHCPPoolChangeViewModelExtensions
    {
        private static bool StringsEqual(string left, string right)
        {
            return (
                    (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right)) ||
                    (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right) && left.Equals(right))
                );
        }

        public static bool Changed(this DHCPPoolChangeViewModel changes)
        {
            if (changes.DHCPRelay != changes.ExistingDHCPRelay)
                return true;

            if (!changes.Prefix.Equals(changes.ExistingPrefix))
                return true;

            if (!changes.GatewayAddress.Equals(changes.ExistingGatewayAddress))
                return true;

            if (!StringsEqual(changes.DomainName, changes.ExistingDomainName))
                return true;

            if (!StringsEqual(changes.TFTPBootFile, changes.ExistingTFTPBootFile))
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
