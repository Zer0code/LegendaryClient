#region

using System;
using System.Net;

#endregion

namespace LegendaryClient.Logic.Region
{
    public sealed class PBE : BaseRegion
    {
        public override string RegionName
        {
            get { return "PBE"; }
        }

        public override string Location
        {
            get { return null; }
        }

        public override string InternalName
        {
            get { return "pbe1"; }
        }

        public override string ChatName
        {
            get { return "pbe1"; }
        }

        public override Uri NewsAddress
        {
            get { return new Uri("http://ll.leagueoflegends.com/landingpage/data/na/en_US.js"); }
        }

        public override bool Garena
        {
            get { return false; }
        }

        public override PVPNetConnect.Region PVPRegion
        {
            get { return PVPNetConnect.Region.PBE; }
        }

        public override string Locale
        {
            get { return "en_US"; }
        }

        public override IPAddress[] PingAddresses
        {
            get
            {
                return new IPAddress[]
                {
                    //No known IP address
                };
            }
        }

        public override Uri SpectatorLink
        {
            get { return new Uri("http://spectator.pbe1.lol.riotgames.com:8088/observer-mode/rest/"); }
        }

        public override string SpectatorIpAddress
        {
            get { return "69.88.138.29:8088"; }
            set { }
        }
    }
}